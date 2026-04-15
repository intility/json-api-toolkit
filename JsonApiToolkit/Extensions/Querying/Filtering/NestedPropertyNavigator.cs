using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using JsonApiToolkit.Helpers;
using JsonApiToolkit.Models.Errors;
using JsonApiToolkit.Models.Querying.Filtering;
using Microsoft.Extensions.Logging;

namespace JsonApiToolkit.Extensions.Querying;

internal static partial class NestedPropertyNavigator
{
    private const int MaxLogValueLength = 100;

    /// <summary>
    /// Maximum recursion depth for nested collection navigations.
    /// Prevents stack overflow from malicious deeply nested filter paths.
    /// </summary>
    private const int MaxRecursionDepth = 5;

    /// <summary>
    /// Sanitizes user input for safe logging by removing control characters
    /// and truncating long values to prevent log forging attacks.
    /// </summary>
    private static string SanitizeForLog(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "(empty)";

        // Remove control characters (newlines, tabs, etc.) that could forge log entries
        string sanitized = ControlCharRegex().Replace(value, " ");

        // Truncate long values
        if (sanitized.Length > MaxLogValueLength)
            return string.Concat(sanitized.AsSpan(0, MaxLogValueLength), "...(truncated)");

        return sanitized;
    }

    [GeneratedRegex(@"[\x00-\x1F\x7F]")]
    private static partial Regex ControlCharRegex();

    internal static Expression? BuildSafeNestedFilterExpression(
        ParameterExpression parameter,
        FilterParameter filter,
        ILogger? logger = null,
        int depth = 0
    )
    {
        if (depth > MaxRecursionDepth)
        {
            throw new JsonApiBadRequestException(
                $"Filter path recursion depth exceeds maximum of {MaxRecursionDepth}. "
                    + "Simplify the filter expression or reduce collection nesting.",
                JsonApiErrorCodes.QueryTooComplex,
                new ErrorSource { Parameter = $"filter[{filter.Field}]" },
                new Dictionary<string, object>
                {
                    ["field"] = filter.Field,
                    ["maxDepth"] = MaxRecursionDepth,
                    ["actualDepth"] = depth,
                }
            );
        }

        string[] parts = filter.Field.Split('.');
        Expression current = parameter;
        var nullChecks = new List<Expression>();

        for (int i = 0; i < parts.Length - 1; i++)
        {
            PropertyInfo? prop = QueryHelpers.GetPropertyByJsonName(current.Type, parts[i]);
            if (prop == null)
            {
                logger?.LogWarning(
                    "Property '{PropertyName}' not found on {Type} during navigation",
                    parts[i],
                    current.Type.Name
                );
                return null;
            }

            current = Expression.Property(current, prop);

            // Check if this property is a collection (but not a string)
            Type? elementType = TypeHelpers.GetCollectionElementType(prop.PropertyType);
            if (elementType != null)
            {
                // Build collection filter using Any() for remaining path
                string[] remainingParts = parts.Skip(i + 1).ToArray();
                Expression? collectionFilter = BuildCollectionFilterExpression(
                    current,
                    elementType,
                    remainingParts,
                    filter,
                    logger,
                    depth + 1
                );

                if (collectionFilter == null)
                    return null;

                // Combine with null checks for the path so far
                Expression result = collectionFilter;
                // Note: We don't add a null check for collection navigations because:
                // 1. Collection navigations in EF Core are never truly null in SQL
                // 2. Adding a null check forces MaterializeCollectionNavigation() which breaks many-to-many translation
                // 3. The Any() predicate handles empty collections correctly (returns false)

                for (int j = nullChecks.Count - 1; j >= 0; j--)
                    result = Expression.AndAlso(nullChecks[j], result);

                return result;
            }

            if (
                !prop.PropertyType.IsValueType
                || Nullable.GetUnderlyingType(prop.PropertyType) != null
            )
            {
                nullChecks.Add(Expression.NotEqual(current, Expression.Constant(null)));
            }
        }

        PropertyInfo? finalProp = QueryHelpers.GetPropertyByJsonName(current.Type, parts[^1]);
        if (finalProp == null)
        {
            logger?.LogWarning(
                "Property '{PropertyName}' not found on {Type}",
                parts[^1],
                current.Type.Name
            );
            return null;
        }

        Expression finalProperty = Expression.Property(current, finalProp);
        Expression? filterExpression = BuildPropertyFilterExpression(finalProperty, filter, logger);
        if (filterExpression == null)
            return null;

        Expression result2;
        if (filter.Operator == FilterOperator.Ne || filter.Operator == FilterOperator.Nin)
        {
            if (nullChecks.Count > 0)
            {
                Expression allNotNull = nullChecks[0];
                for (int i = 1; i < nullChecks.Count; i++)
                    allNotNull = Expression.AndAlso(allNotNull, nullChecks[i]);

                Expression anyNull = Expression.Not(allNotNull);
                Expression notNullAndFilter = Expression.AndAlso(allNotNull, filterExpression);
                result2 = Expression.OrElse(anyNull, notNullAndFilter);
            }
            else
            {
                result2 = filterExpression;
            }
        }
        else
        {
            result2 = filterExpression;
            // Iterate in reverse to ensure outer null checks are evaluated first
            // e.g., e.A != null && e.A.B != null && filterExpression
            for (int i = nullChecks.Count - 1; i >= 0; i--)
                result2 = Expression.AndAlso(nullChecks[i], result2);
        }

        return result2;
    }

    internal static Expression? BuildPropertyFilterExpression(
        Expression propertyAccess,
        FilterParameter filter,
        ILogger? logger = null
    )
    {
        Type targetType = propertyAccess.Type;

        // Check if the property itself is a collection (e.g., List<string> for CVEs/Tags)
        Type? collectionElementType = TypeHelpers.GetCollectionElementType(targetType);
        if (collectionElementType != null)
        {
            return BuildCollectionPropertyFilterExpression(
                propertyAccess,
                collectionElementType,
                filter,
                logger
            );
        }

        if (filter.Operator == FilterOperator.IsNull)
            return Expression.Equal(propertyAccess, Expression.Constant(null));

        if (filter.Operator == FilterOperator.IsNotNull)
            return Expression.NotEqual(propertyAccess, Expression.Constant(null));

        if (filter.Operator == FilterOperator.In)
        {
            Type? underlying = Nullable.GetUnderlyingType(targetType);
            if (underlying != null)
            {
                BinaryExpression notNullExpr = Expression.NotEqual(
                    propertyAccess,
                    Expression.Constant(null, propertyAccess.Type)
                );
                Expression containsExpr = FilterOperatorExpressions.BuildInExpression(
                    Expression.Property(propertyAccess, "Value"),
                    filter.Value,
                    underlying
                );
                return Expression.AndAlso(notNullExpr, containsExpr);
            }
            return FilterOperatorExpressions.BuildInExpression(
                propertyAccess,
                filter.Value,
                targetType
            );
        }

        if (filter.Operator == FilterOperator.Nin)
        {
            Type? underlying = Nullable.GetUnderlyingType(targetType);
            if (underlying != null)
            {
                BinaryExpression isNullExpr = Expression.Equal(
                    propertyAccess,
                    Expression.Constant(null, propertyAccess.Type)
                );
                Expression containsExpr = FilterOperatorExpressions.BuildInExpression(
                    Expression.Property(propertyAccess, "Value"),
                    filter.Value,
                    underlying
                );
                return Expression.OrElse(isNullExpr, Expression.Not(containsExpr));
            }
            return Expression.Not(
                FilterOperatorExpressions.BuildInExpression(
                    propertyAccess,
                    filter.Value,
                    targetType
                )
            );
        }

        object? filterValue = QueryHelpers.ConvertToPropertyType(filter.Value, targetType);
        if (
            filterValue == null
            && filter.Operator != FilterOperator.Eq
            && filter.Operator != FilterOperator.Ne
        )
        {
            logger?.LogWarning(
                "Failed to convert '{Value}' to {PropertyType}",
                SanitizeForLog(filter.Value),
                targetType.Name
            );
            return null;
        }

        ConstantExpression constant = Expression.Constant(filterValue, targetType);

        return filter.Operator switch
        {
            FilterOperator.Eq => Expression.Equal(propertyAccess, constant),
            FilterOperator.Ne => Expression.NotEqual(propertyAccess, constant),
            FilterOperator.Gt => Expression.GreaterThan(propertyAccess, constant),
            FilterOperator.Ge => Expression.GreaterThanOrEqual(propertyAccess, constant),
            FilterOperator.Lt => Expression.LessThan(propertyAccess, constant),
            FilterOperator.Le => Expression.LessThanOrEqual(propertyAccess, constant),
            FilterOperator.Like => FilterOperatorExpressions.BuildLikeExpression(
                propertyAccess,
                filter.Value
            ),
            _ => Expression.Equal(propertyAccess, constant),
        };
    }
}

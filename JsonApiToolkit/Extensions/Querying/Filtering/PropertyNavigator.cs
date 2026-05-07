using System.Linq.Expressions;
using System.Reflection;
using JsonApiToolkit.Models.Errors;
using JsonApiToolkit.Models.Querying.Filtering;
using Microsoft.Extensions.Logging;

namespace JsonApiToolkit.Extensions.Querying;

/// <summary>
/// Walks dot-notation filter paths (e.g. "author.address.city") to build LINQ
/// expressions, with null-safety chains and recursion-depth guarding.
/// </summary>
internal static class PropertyNavigator
{
    /// <summary>
    /// Maximum recursion depth for nested collection navigations.
    /// Prevents stack overflow from malicious deeply nested filter paths.
    /// </summary>
    internal const int MaxRecursionDepth = 5;

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
                Expression? collectionFilter =
                    CollectionFilterBuilder.BuildCollectionFilterExpression(
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
        Expression? filterExpression = PropertyFilterBuilder.BuildPropertyFilterExpression(
            finalProperty,
            filter,
            logger
        );
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
}

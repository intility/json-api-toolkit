using System.Linq.Expressions;
using System.Reflection;
using JsonApiToolkit.Helpers;
using JsonApiToolkit.Models.Errors;
using JsonApiToolkit.Models.Querying.Filtering;
using Microsoft.Extensions.Logging;

namespace JsonApiToolkit.Extensions.Querying;

internal static partial class NestedPropertyNavigator
{
    /// <summary>
    /// Builds a filter expression for collection navigation using Any().
    /// e.g., collection.Any(item => item.Property == value)
    /// </summary>
    private static Expression? BuildCollectionFilterExpression(
        Expression collectionAccess,
        Type elementType,
        string[] remainingParts,
        FilterParameter filter,
        ILogger? logger,
        int depth
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
        // Create parameter for the lambda: item =>
        ParameterExpression itemParam = Expression.Parameter(elementType, "item");

        // Build the inner filter expression for the remaining path
        FilterParameter innerFilter = new FilterParameter
        {
            Field = string.Join(".", remainingParts),
            Value = filter.Value,
            Operator = filter.Operator,
            IsIncludeFilter = filter.IsIncludeFilter,
        };

        Expression? innerExpression;
        if (remainingParts.Length == 1)
        {
            // Simple property access on the element
            PropertyInfo? prop = QueryHelpers.GetPropertyByJsonName(elementType, remainingParts[0]);
            if (prop == null)
            {
                logger?.LogWarning(
                    "Property '{PropertyName}' not found on {Type}",
                    remainingParts[0],
                    elementType.Name
                );
                return null;
            }

            Expression propertyAccess = Expression.Property(itemParam, prop);
            innerExpression = BuildPropertyFilterExpression(propertyAccess, filter, logger);
        }
        else
        {
            // Nested property access - recursively build
            innerExpression = BuildSafeNestedFilterExpression(
                itemParam,
                innerFilter,
                logger,
                depth
            );
        }

        if (innerExpression == null)
            return null;

        // Create lambda: item => innerExpression
        LambdaExpression predicate = Expression.Lambda(innerExpression, itemParam);

        // Get the Enumerable.Any<T>(IEnumerable<T>, Func<T, bool>) method
        MethodInfo anyMethod = ReflectionMethodCache.GetEnumerableAnyWithPredicate(elementType);

        // Build: collection.Any(item => predicate)
        return Expression.Call(anyMethod, collectionAccess, predicate);
    }

    /// <summary>
    /// Builds a filter expression when the property itself is a collection.
    /// e.g., entity.Tags.Contains("value") for filter[tags][in]=value
    /// </summary>
    private static Expression? BuildCollectionPropertyFilterExpression(
        Expression collectionAccess,
        Type elementType,
        FilterParameter filter,
        ILogger? logger
    )
    {
        // For In/Eq operators: check if collection contains the value
        // e.g., tags.Contains("important")
        if (
            filter.Operator == FilterOperator.In
            || filter.Operator == FilterOperator.Eq
            || filter.Operator == FilterOperator.Like
        )
        {
            // For Like operator on collection, use Any() with Contains
            if (filter.Operator == FilterOperator.Like)
            {
                // collection.Any(item => item.Contains(value))
                ParameterExpression itemParam = Expression.Parameter(elementType, "item");

                // Only strip % if value has both leading AND trailing %
                string cleanValue =
                    filter.Value.StartsWith('%')
                    && filter.Value.EndsWith('%')
                    && filter.Value.Length > 2
                        ? filter.Value[1..^1]
                        : filter.Value;

                MethodInfo? containsMethod = typeof(string).GetMethod("Contains", [typeof(string)]);
                Expression containsCall = Expression.Call(
                    itemParam,
                    containsMethod!,
                    Expression.Constant(cleanValue)
                );

                LambdaExpression predicate = Expression.Lambda(containsCall, itemParam);

                MethodInfo anyMethod = ReflectionMethodCache.GetEnumerableAnyWithPredicate(
                    elementType
                );

                return Expression.Call(anyMethod, collectionAccess, predicate);
            }

            // For In/Eq: collection.Contains(value)
            object? filterValue = QueryHelpers.ConvertToPropertyType(filter.Value, elementType);
            if (filterValue == null)
            {
                logger?.LogWarning(
                    "Failed to convert '{Value}' to {ElementType} for collection filter",
                    SanitizeForLog(filter.Value),
                    elementType.Name
                );
                return null;
            }

            // Get Contains method on IEnumerable<T> (via Enumerable.Contains)
            MethodInfo containsMethodInfo = ReflectionMethodCache.GetEnumerableContains(
                elementType
            );

            return Expression.Call(
                containsMethodInfo,
                collectionAccess,
                Expression.Constant(filterValue, elementType)
            );
        }

        // For Nin/Ne operators: check if collection does NOT contain the value
        if (filter.Operator == FilterOperator.Nin || filter.Operator == FilterOperator.Ne)
        {
            object? filterValue = QueryHelpers.ConvertToPropertyType(filter.Value, elementType);
            if (filterValue == null)
            {
                logger?.LogWarning(
                    "Failed to convert '{Value}' to {ElementType} for collection filter",
                    SanitizeForLog(filter.Value),
                    elementType.Name
                );
                return null;
            }

            MethodInfo containsMethodInfo = ReflectionMethodCache.GetEnumerableContains(
                elementType
            );

            return Expression.Not(
                Expression.Call(
                    containsMethodInfo,
                    collectionAccess,
                    Expression.Constant(filterValue, elementType)
                )
            );
        }

        // For IsNull/IsNotNull: check if collection is null
        if (filter.Operator == FilterOperator.IsNull)
            return Expression.Equal(collectionAccess, Expression.Constant(null));

        if (filter.Operator == FilterOperator.IsNotNull)
            return Expression.NotEqual(collectionAccess, Expression.Constant(null));

        logger?.LogWarning(
            "Operator '{Operator}' is not supported for collection properties",
            filter.Operator
        );
        return null;
    }
}

using System.Linq.Expressions;
using System.Reflection;
using JsonApiToolkit.Models.Querying;
using Microsoft.Extensions.Logging;

namespace JsonApiToolkit.Extensions.Querying;

/// <summary>
/// Provides extension methods to apply JSON:API sorting parameters to IQueryable sources.
/// </summary>
/// <remarks>
/// Implements the sorting strategy defined in the JSON:API specification, supporting multiple
/// sort fields and ascending/descending direction.
/// </remarks>
public static class SortingHandler
{
    /// <summary>
    /// Applies JSON:API sort parameters to an IQueryable data source.
    /// </summary>
    /// <typeparam name="T">The entity type of the queryable</typeparam>
    /// <param name="query">The source IQueryable to sort</param>
    /// <param name="sortParameters">The list of sort parameters specifying fields and directions</param>
    /// <param name="logger">Optional logger for debugging and tracing</param>
    /// <returns>A new IQueryable with the specified sorting applied</returns>
    /// <remarks>
    /// <para>
    /// Supports multiple sort fields in priority order. For each field, dynamically creates an OrderBy
    /// or OrderByDescending expression, followed by ThenBy or ThenByDescending for subsequent fields.
    /// </para>
    /// <para>
    /// Returns the original query if no valid sort parameters are provided.
    /// </para>
    /// </remarks>
    public static IQueryable<T> ApplySorting<T>(
        this IQueryable<T> query,
        List<SortParameter> sortParameters,
        ILogger? logger = null
    )
    {
        logger?.LogDebug(
            "Applying sorting to query for type {EntityType} with {SortParameterCount} sort parameters",
            typeof(T).Name,
            sortParameters?.Count ?? 0
        );

        if (sortParameters == null || sortParameters.Count == 0)
        {
            logger?.LogDebug("No sort parameters provided, returning original query");
            return query;
        }

        IOrderedQueryable<T>? orderedQuery = null;
        Type entityType = typeof(T);

        for (int i = 0; i < sortParameters.Count; i++)
        {
            SortParameter sortParam = sortParameters[i];
            PropertyInfo? property = QueryHelpers.GetPropertyByJsonName(
                entityType,
                sortParam.Field
            );

            if (property == null)
                continue;

            ParameterExpression parameter = Expression.Parameter(entityType, "x");
            MemberExpression propertyAccess = Expression.Property(parameter, property);
            LambdaExpression lambda = Expression.Lambda(propertyAccess, parameter);

            string methodName;

            if (i == 0)
            {
                methodName = sortParam.IsDescending ? "OrderByDescending" : "OrderBy";
                orderedQuery = (IOrderedQueryable<T>?)
                    typeof(Queryable)
                        .GetMethods()
                        .Single(method =>
                            method.Name == methodName
                            && method.IsGenericMethodDefinition
                            && method.GetParameters().Length == 2
                        )
                        .MakeGenericMethod(entityType, property.PropertyType)
                        .Invoke(null, [query, lambda]);
            }
            else
            {
                methodName = sortParam.IsDescending ? "ThenByDescending" : "ThenBy";

                orderedQuery = (IOrderedQueryable<T>?)
                    typeof(Queryable)
                        .GetMethods()
                        .Single(method =>
                            method.Name == methodName
                            && method.IsGenericMethodDefinition
                            && method.GetParameters().Length == 2
                        )
                        .MakeGenericMethod(entityType, property.PropertyType)
                        .Invoke(null, [orderedQuery, lambda]);
            }
        }

        return orderedQuery ?? query;
    }
}

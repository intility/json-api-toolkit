using System.Linq.Expressions;
using JsonApiToolkit.Models.Querying.Filtering;
using Microsoft.Extensions.Logging;

namespace JsonApiToolkit.Extensions.Querying;

/// <summary>
/// /// Provides extension methods to apply JSON:API filter conditions to IQueryable sources.
/// </summary>
/// <remarks>
/// This class serves as the bridge between JSON:API filter parameters and queryable data sources,
/// translating filter specifications into LINQ expressions.
/// </remarks>
public static class FilterHandler
{
    /// <summary>
    /// Applies a set of JSON:API filter conditions to an IQueryable data source.
    /// </summary>
    /// <typeparam name="T">The entity type of the queryable</typeparam>
    /// <param name="query">The source IQueryable to filter</param>
    /// <param name="filterGroup">The filter group defining the conditions to apply</param>
    /// <param name="logger">Optional logger for debugging and tracing</param>
    /// <returns>A new IQueryable with the filter conditions applied</returns>
    /// <remarks>
    /// Supports complex filtering with nested conditions, different operators (eq, ne, gt, lt, etc.),
    /// and logical combinations (AND, OR, NOT). Returns the original query if no valid filters exist.
    /// </remarks>
    public static IQueryable<T> ApplyFilters<T>(
        this IQueryable<T> query,
        FilterGroup filterGroup,
        ILogger? logger = null
    )
    {
        logger?.LogDebug(
            "Applying filters to query for type {EntityType}: {FilterCount} direct filters, {GroupCount} nested groups",
            typeof(T).Name,
            filterGroup?.Filters.Count ?? 0,
            filterGroup?.Groups.Count ?? 0
        );

        if (
            filterGroup == null
            || (filterGroup.Filters.Count == 0 && filterGroup.Groups.Count == 0)
        )
        {
            logger?.LogDebug("No filters to apply, returning original query");
            return query;
        }

        ParameterExpression parameter = Expression.Parameter(typeof(T), "x");
        Expression? expression = FilterExpressionBuilder.BuildFilterExpression<T>(
            filterGroup,
            parameter,
            logger
        );

        if (expression != null)
        {
            logger?.LogDebug("Successfully built filter expression, applying to query");
            var lambda = Expression.Lambda<Func<T, bool>>(expression, parameter);
            query = query.Where(lambda);
        }
        else
        {
            logger?.LogWarning("Filter expression builder returned null, no filters applied");
        }

        return query;
    }
}

using System.Linq.Expressions;
using JsonApiToolkit.Models.Querying.Filtering;
using Microsoft.Extensions.Logging;

namespace JsonApiToolkit.Extensions.Querying;

/// <summary>
/// Applies JSON:API filter conditions to IQueryable sources.
/// </summary>
public static class FilterHandler
{
    /// <summary>
    /// Applies filters to queryable (supports nested conditions, operators, AND/OR/NOT).
    /// </summary>
    public static IQueryable<T> ApplyFilters<T>(
        this IQueryable<T> query,
        FilterGroup filterGroup,
        ILogger? logger = null
    )
    {
        if (
            filterGroup == null
            || (filterGroup.Filters.Count == 0 && filterGroup.Groups.Count == 0)
        )
            return query;

        ParameterExpression parameter = Expression.Parameter(typeof(T), "x");
        Expression? expression = FilterExpressionBuilder.BuildFilterExpression<T>(
            filterGroup,
            parameter,
            logger
        );

        if (expression != null)
        {
            var lambda = Expression.Lambda<Func<T, bool>>(expression, parameter);
            return query.Where(lambda);
        }

        logger?.LogWarning("Filter expression returned null for {Type}", typeof(T).Name);
        return query;
    }
}

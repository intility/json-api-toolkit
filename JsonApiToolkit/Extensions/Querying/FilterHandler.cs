using System.Linq.Expressions;
using JsonApiToolkit.Models.Querying.Filtering;

namespace JsonApiToolkit.Extensions.Querying;

/// <summary>
/// Helper class for applying filters.
/// </summary>
public static class FilterHandler
{
    /// <summary>
    /// Applies filters to an IQueryable.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="query">The IQueryable to apply the filters to.</param>
    /// <param name="filterGroup">The filter group to apply.</param>
    /// <returns>The IQueryable with the filters applied.</returns>
    public static IQueryable<T> ApplyFilters<T>(this IQueryable<T> query, FilterGroup filterGroup)
    {
        if (
            filterGroup == null
            || (filterGroup.Filters.Count == 0 && filterGroup.Groups.Count == 0)
        )
        {
            return query;
        }

        ParameterExpression parameter = Expression.Parameter(typeof(T), "x");
        Expression? expression = FilterExpressionBuilder.BuildFilterExpression<T>(
            filterGroup,
            parameter
        );

        if (expression != null)
        {
            var lambda = Expression.Lambda<Func<T, bool>>(expression, parameter);
            query = query.Where(lambda);
        }

        return query;
    }
}

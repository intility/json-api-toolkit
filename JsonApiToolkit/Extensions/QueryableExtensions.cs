using JsonApiToolkit.Extensions.Querying;
using JsonApiToolkit.Models;
using JsonApiToolkit.Models.Querying;

namespace JsonApiToolkit.Extensions;

/// <summary>
/// Extensions for IQueryable to apply JSON:API query parameters.
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Applies JSON:API query parameters to an IQueryable.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="query">The IQueryable to apply the query parameters to.</param>
    /// <param name="parameters">The JSON:API query parameters.</param>
    /// <returns>The IQueryable with the query parameters applied.</returns>
    public static IQueryable<T> ApplyJsonApiParameters<T>(
        this IQueryable<T> query,
        QueryParameters parameters
    )
    {
        if (parameters == null)
            return query;

        if (parameters.Filter != null)
            query = query.ApplyFilters(parameters.Filter);

        if (parameters.Sort?.Count > 0)
            query = query.ApplySorting(parameters.Sort);
        else
            query = query.ApplySorting([new SortParameter { Field = "Id", IsDescending = false }]);

        if (parameters.Pagination != null)
            query = query.ApplyPagination(parameters.Pagination);

        return query;
    }
}

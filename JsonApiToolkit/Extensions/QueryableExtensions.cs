using JsonApiToolkit.Extensions.Querying;
using JsonApiToolkit.Models.Querying;
using JsonApiToolkit.Models.Querying.Filtering;
using Microsoft.EntityFrameworkCore;

namespace JsonApiToolkit.Extensions;

/// <summary>
/// Extension methods for applying JSON:API query parameters to IQueryable.
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Applies all JSON:API parameters: filters → sort (defaults to Id) → pagination.
    /// </summary>
    public static IQueryable<T> ApplyJsonApiParameters<T>(
        this IQueryable<T> query,
        QueryParameters parameters
    )
    {
        if (parameters == null)
            return query;

        if (parameters.Filter != null)
            query = query.ApplyFilters(parameters.Filter);

        query =
            parameters.Sort?.Count > 0
                ? query.ApplySorting(parameters.Sort!)
                : query.ApplySorting([new SortParameter { Field = "Id", IsDescending = false }]);

        if (parameters.Pagination != null)
            query = query.ApplyPagination(parameters.Pagination);

        return query;
    }

    /// <summary>
    /// Applies EF Core Include() for each path (supports dot notation).
    /// </summary>
    public static IQueryable<T> ApplyIncludes<T>(
        this IQueryable<T> query,
        List<string>? includePaths
    )
        where T : class
    {
        if (includePaths == null || includePaths.Count == 0)
            return query;

        foreach (string path in includePaths)
        {
            query = query.Include(path.Trim());
        }
        return query;
    }

    /// <summary>
    /// Applies EF Core Include() using AsSingleQuery() to prevent split query issues with pagination.
    /// Forces single query with JOINs instead of separate queries.
    /// </summary>
    public static IQueryable<T> ApplyIncludesSingleQuery<T>(
        this IQueryable<T> query,
        List<string>? includePaths
    )
        where T : class
    {
        if (includePaths == null || includePaths.Count == 0)
            return query;

        query = query.AsSingleQuery();

        foreach (string path in includePaths)
        {
            query = query.Include(path.Trim());
        }
        return query;
    }
}

using JsonApiToolkit.Extensions.Querying;
using JsonApiToolkit.Models.Querying;
using JsonApiToolkit.Models.Querying.Filtering;
using Microsoft.EntityFrameworkCore;

namespace JsonApiToolkit.Extensions;

/// <summary>
/// Provides extension methods for applying JSON:API query parameters to IQueryable data sources.
/// </summary>
/// <remarks>
/// Consolidates the application of filtering, sorting, and pagination in a single convenient extension method.
/// </remarks>
public static class QueryableExtensions
{
    /// <summary>
    /// Applies all JSON:API query parameters to an IQueryable data source in the correct order.
    /// </summary>
    /// <typeparam name="T">The entity type of the queryable</typeparam>
    /// <param name="query">The source IQueryable to apply parameters to</param>
    /// <param name="parameters">The complete set of JSON:API query parameters</param>
    /// <returns>A new IQueryable with all query parameters applied</returns>
    /// <remarks>
    /// Applies parameters in the following order:
    /// <list type="number">
    /// <item>
    /// <description>Filtering - narrows the result set based on field conditions</description>
    /// </item>
    /// <item>
    /// <description>Sorting - orders the results (defaults to Id ascending if not specified)</description>
    /// </item>
    /// <item>
    /// <description>Pagination - limits the number of results and supports paging</description>
    /// </item>
    /// </list>
    /// This is the recommended method for applying all JSON:API query parameters in a single operation.
    /// </remarks>
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

    /// <summary>
    /// Dynamically applies EF Core Include() calls for each include path (dot notation supported).
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The source queryable.</param>
    /// <param name="includePaths">A list of include paths (e.g. "todo", "todo.category").</param>
    /// <returns>The queryable with all includes applied.</returns>
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
    /// Dynamically applies EF Core Include() calls using AsSingleQuery() to prevent split query issues.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The source queryable.</param>
    /// <param name="includePaths">A list of include paths (e.g. "todo", "todo.category").</param>
    /// <returns>The queryable with all includes applied using single query mode.</returns>
    /// <remarks>
    /// Use this method when pagination is present to avoid EF Core split query optimization issues
    /// that can cause includes to load data for wrong entities or no entities at all.
    /// Forces EF Core to use a single query with JOINs instead of separate queries.
    /// </remarks>
    public static IQueryable<T> ApplyIncludesSingleQuery<T>(
        this IQueryable<T> query,
        List<string>? includePaths
    )
        where T : class
    {
        if (includePaths == null || includePaths.Count == 0)
            return query;

        // Force single query to prevent EF Core split query issues with pagination
        query = query.AsSingleQuery();

        foreach (string path in includePaths)
        {
            query = query.Include(path.Trim());
        }
        return query;
    }
}

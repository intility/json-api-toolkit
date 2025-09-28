using JsonApiToolkit.Models.Metadata;
using JsonApiToolkit.Models.Querying;
using Microsoft.EntityFrameworkCore;

namespace JsonApiToolkit.Extensions.Querying;

/// <summary>
/// Provides extension methods for implementing JSON:API pagination on IQueryable data sources.
/// </summary>
/// <remarks>
/// Implements the pagination strategy specified in the JSON:API specification using page-based pagination
/// with configurable page size and page number.
/// </remarks>
public static class PaginationHandler
{
    /// <summary>
    /// Applies pagination parameters to an IQueryable data source.
    /// </summary>
    /// <typeparam name="T">The entity type of the queryable</typeparam>
    /// <param name="query">The source IQueryable to paginate</param>
    /// <param name="pagination">The pagination parameters defining page number and size</param>
    /// <returns>A new IQueryable with pagination applied (Skip/Take)</returns>
    /// <remarks>
    /// Translates the page-based pagination model (page number and size) into the offset-based
    /// pagination used by LINQ (Skip and Take). Invalid page numbers are clamped to valid ranges.
    /// </remarks>
    public static IQueryable<T> ApplyPagination<T>(
        this IQueryable<T> query,
        PaginationParameters pagination
    )
    {
        // Calculate total count and pages to determine valid page range
        int totalCount = query.Count();
        int totalPages = (int)Math.Ceiling(totalCount / (double)pagination.Size);

        // Clamp page number to valid range (1 to totalPages, default to 1 if empty)
        int effectivePage = Math.Max(1, Math.Min(pagination.Number, Math.Max(totalPages, 1)));

        int skip = (effectivePage - 1) * pagination.Size;
        return query.Skip(skip).Take(pagination.Size);
    }

    /// <summary>
    /// Creates pagination metadata for use in JSON:API responses.
    /// </summary>
    /// <typeparam name="T">The entity type of the queryable</typeparam>
    /// <param name="query">The source IQueryable before pagination was applied</param>
    /// <param name="pagination">The pagination parameters that were applied</param>
    /// <returns>A PaginationMeta object containing total counts and pagination information</returns>
    /// <remarks>
    /// This method executes a COUNT query on the database to determine the total number of resources
    /// and calculates total pages based on the page size. Invalid page numbers are clamped to valid ranges.
    /// </remarks>
    public static async Task<PaginationMeta> CreatePaginationMetaAsync<T>(
        this IQueryable<T> query,
        PaginationParameters pagination
    )
    {
        int totalCount;
        try
        {
            totalCount = await query.CountAsync();
        }
        catch (InvalidOperationException)
        {
            // Fallback for in-memory queryables that don't support async operations
            totalCount = query.Count();
        }

        int totalPages = (int)Math.Ceiling(totalCount / (double)pagination.Size);

        // Clamp page number to valid range (1 to totalPages, default to 1 if empty)
        int effectivePage = Math.Max(1, Math.Min(pagination.Number, Math.Max(totalPages, 1)));

        return new PaginationMeta
        {
            TotalResources = totalCount,
            TotalPages = totalPages,
            CurrentPage = effectivePage,
            PageSize = pagination.Size,
        };
    }
}

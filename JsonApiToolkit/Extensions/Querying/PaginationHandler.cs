using JsonApiToolkit.Models.Metadata;
using JsonApiToolkit.Models.Querying;
using Microsoft.EntityFrameworkCore;

namespace JsonApiToolkit.Extensions.Querying;

/// <summary>
/// Helper class for handling pagination.
/// </summary>
public static class PaginationHandler
{
    /// <summary>
    /// Applies pagination to an IQueryable.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="query">The IQueryable to apply the pagination to.</param>
    /// <param name="pagination">The pagination parameters to apply.</param>
    /// <returns>The IQueryable with the pagination applied.</returns>
    public static IQueryable<T> ApplyPagination<T>(
        this IQueryable<T> query,
        PaginationParameters pagination
    )
    {
        int skip = (pagination.Number - 1) * pagination.Size;
        return query.Skip(skip).Take(pagination.Size);
    }

    /// <summary>
    /// Creates pagination metadata for an IQueryable.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="query">The IQueryable to create the pagination metadata for.</param>
    /// <param name="pagination">The pagination parameters.</param>
    /// <returns>The pagination metadata.</returns>
    public static async Task<PaginationMeta> CreatePaginationMetaAsync<T>(
        this IQueryable<T> query,
        PaginationParameters pagination
    )
    {
        int totalCount = await query.CountAsync();
        int totalPages = (int)Math.Ceiling(totalCount / (double)pagination.Size);

        return new PaginationMeta
        {
            TotalResources = totalCount,
            TotalPages = totalPages,
            CurrentPage = pagination.Number,
            PageSize = pagination.Size,
        };
    }
}

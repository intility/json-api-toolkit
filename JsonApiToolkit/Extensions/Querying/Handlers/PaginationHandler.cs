using JsonApiToolkit.Models.Metadata;
using JsonApiToolkit.Models.Querying;
using Microsoft.EntityFrameworkCore;

namespace JsonApiToolkit.Extensions.Querying;

/// <summary>
/// Applies JSON:API pagination to IQueryable sources.
/// Uses page-based pagination (page number + size).
/// </summary>
public static class PaginationHandler
{
    /// <summary>
    /// Applies pagination using Skip/Take. Clamps invalid page numbers.
    /// </summary>
    public static IQueryable<T> ApplyPagination<T>(
        this IQueryable<T> query,
        PaginationParameters pagination
    )
    {
        int size = Math.Max(1, pagination.Size);
        int totalCount = query.Count();
        int totalPages = (int)Math.Ceiling(totalCount / (double)size);
        int effectivePage = Math.Max(1, Math.Min(pagination.Number, Math.Max(totalPages, 1)));

        int skip = (effectivePage - 1) * size;
        return query.Skip(skip).Take(size);
    }

    /// <summary>
    /// Creates pagination metadata (executes COUNT query).
    /// </summary>
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
            totalCount = query.Count(); // Fallback for in-memory queryables
        }

        int size = Math.Max(1, pagination.Size);
        int totalPages = (int)Math.Ceiling(totalCount / (double)size);
        int effectivePage = Math.Max(1, Math.Min(pagination.Number, Math.Max(totalPages, 1)));

        return new PaginationMeta
        {
            TotalResources = totalCount,
            TotalPages = totalPages,
            CurrentPage = effectivePage,
            PageSize = size,
        };
    }
}

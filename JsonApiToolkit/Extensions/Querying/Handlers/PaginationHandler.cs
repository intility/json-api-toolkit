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
    /// Applies pagination using Skip/Take with pre-computed total count.
    /// Prefer this overload when you already have the count to avoid redundant queries.
    /// </summary>
    public static IQueryable<T> ApplyPagination<T>(
        this IQueryable<T> query,
        PaginationParameters pagination,
        int totalCount
    )
    {
        int size = Math.Max(1, pagination.Size);
        int totalPages = (int)Math.Ceiling(totalCount / (double)size);
        int effectivePage = Math.Max(1, Math.Min(pagination.Number, Math.Max(totalPages, 1)));

        int skip = (effectivePage - 1) * size;
        return query.Skip(skip).Take(size);
    }

    /// <summary>
    /// Applies pagination using Skip/Take. Executes a synchronous COUNT query.
    /// For async contexts, prefer using the overload with pre-computed totalCount.
    /// </summary>
    public static IQueryable<T> ApplyPagination<T>(
        this IQueryable<T> query,
        PaginationParameters pagination
    )
    {
        int totalCount = query.Count();
        return query.ApplyPagination(pagination, totalCount);
    }

    /// <summary>
    /// Applies pagination using Skip/Take with async COUNT query.
    /// </summary>
    public static async Task<IQueryable<T>> ApplyPaginationAsync<T>(
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

        return query.ApplyPagination(pagination, totalCount);
    }

    /// <summary>
    /// Creates pagination metadata from pre-computed total count.
    /// Prefer this overload when you already have the count.
    /// </summary>
    public static PaginationMeta CreatePaginationMeta(
        PaginationParameters pagination,
        int totalCount
    )
    {
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

    /// <summary>
    /// Creates pagination metadata (executes COUNT query).
    /// Consider using CreatePaginationMeta with pre-computed count if available.
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

        return CreatePaginationMeta(pagination, totalCount);
    }
}

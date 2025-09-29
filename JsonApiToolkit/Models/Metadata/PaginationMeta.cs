namespace JsonApiToolkit.Models.Metadata;

/// <summary>
/// Pagination metadata for collection responses.
/// </summary>
public class PaginationMeta
{
    /// <summary>
    /// Total number of resources across all pages.
    /// </summary>
    public int TotalResources { get; set; }

    /// <summary>
    /// Total number of pages.
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Current page number.
    /// </summary>
    public int CurrentPage { get; set; }

    /// <summary>
    /// Number of resources per page.
    /// </summary>
    public int PageSize { get; set; }
}

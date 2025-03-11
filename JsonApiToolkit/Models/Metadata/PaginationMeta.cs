namespace JsonApiToolkit.Models.Metadata;

/// <summary>
/// Represents pagination metadata.
/// </summary>
public class PaginationMeta
{
    /// <summary>
    /// The total number of resources.
    /// </summary>
    public int TotalResources { get; set; }

    /// <summary>
    /// The total number of pages.
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// The current page number.
    /// </summary>
    public int CurrentPage { get; set; }

    /// <summary>
    /// The current page size.
    /// </summary>
    public int PageSize { get; set; }
}

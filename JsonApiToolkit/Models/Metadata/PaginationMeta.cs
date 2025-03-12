namespace JsonApiToolkit.Models.Metadata;

/// <summary>
/// Contains metadata about pagination for JSON:API collection responses.
/// </summary>
/// <remarks>
/// This class provides additional pagination information that complements the pagination links.
/// While pagination links help with navigation, pagination metadata provides the context
/// about the overall size and structure of the paginated collection.
/// </remarks>
public class PaginationMeta
{
    /// <summary>
    /// The total number of resources in the collection before pagination.
    /// </summary>
    /// <remarks>
    /// This represents the complete count of resources that match the current filter criteria,
    /// regardless of pagination. Used by clients to show total counts or calculate percentages.
    /// </remarks>
    public int TotalResources { get; set; }

    /// <summary>
    /// The total number of pages available given the current page size.
    /// </summary>
    /// <remarks>
    /// Calculated as ceiling(TotalResources / PageSize). This helps clients understand
    /// the total page range and can be used to implement pagination controls.
    /// </remarks>
    public int TotalPages { get; set; }

    /// <summary>
    /// The current page number being displayed (1-based).
    /// </summary>
    /// <remarks>
    /// Corresponds to the page[number] query parameter. Starts at 1 for the first page,
    /// allowing clients to track current pagination position.
    /// </remarks>
    public int CurrentPage { get; set; }

    /// <summary>
    /// The number of resources displayed per page.
    /// </summary>
    /// <remarks>
    /// Corresponds to the page[size] query parameter. Helps clients understand how
    /// many resources to expect on each page and calculate positions within the collection.
    /// </remarks>
    public int PageSize { get; set; }
}

namespace JsonApiToolkit.Models.Querying;

/// <summary>
/// Contains pagination parameters for limiting and paging through collection resources.
/// </summary>
/// <remarks>
/// <para>
/// Implements the JSON:API pagination strategy with page-based pagination using the
/// page[number] and page[size] query parameters.
/// </para>
/// <para>
/// Pagination is used to limit the number of resources returned in a response and to
/// navigate through large collections across multiple requests.
/// </para>
/// </remarks>
public class PaginationParameters
{
    /// <summary>
    /// The current page number (1-based).
    /// </summary>
    /// <remarks>
    /// Corresponds to the page[number] query parameter in JSON:API requests.
    /// </remarks>
    public int Number { get; set; } = 1;

    /// <summary>
    /// The number of resources to include per page.
    /// </summary>
    /// <remarks>
    /// Corresponds to the page[size] query parameter in JSON:API requests.
    /// </remarks>
    public int Size { get; set; } = 10;
}

namespace JsonApiToolkit.Models.Querying;

/// <summary>
/// Pagination parameters (page[number] and page[size]).
/// </summary>
public class PaginationParameters
{
    /// <summary>
    /// Page number (1-based).
    /// </summary>
    public int Number { get; set; } = 1;

    /// <summary>
    /// Number of resources per page.
    /// </summary>
    public int Size { get; set; } = 10;
}

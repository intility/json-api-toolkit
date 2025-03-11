using JsonApiToolkit.Models.FilterParameters;

namespace JsonApiToolkit.Models;

/// <summary>
/// Represents JSON:API query parameters for pagination, filtering, sorting, and including related resources.
/// </summary>
public class QueryParameters
{
    /// <summary>
    /// Pagination parameters.
    /// </summary>
    public PaginationParameters? Pagination { get; set; }

    /// <summary>
    /// Filter parameters.
    /// </summary>
    public FilterGroup? Filter { get; set; }

    /// <summary>
    /// Sort parameters.
    /// </summary>
    public List<SortParameter>? Sort { get; set; }

    /// <summary>
    /// Include parameters for related resources.
    /// </summary>
    public List<string>? Include { get; set; }
}

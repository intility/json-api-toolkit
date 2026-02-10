using JsonApiToolkit.Models.Querying.Filtering;

namespace JsonApiToolkit.Models.Querying;

/// <summary>
/// All JSON:API query parameters: pagination, filtering, sorting, and includes.
/// </summary>
public class QueryParameters
{
    /// <summary>
    /// Pagination parameters (page number and size).
    /// </summary>
    public PaginationParameters? Pagination { get; set; }

    /// <summary>
    /// Filter criteria with conditions and logical operators.
    /// </summary>
    public FilterGroup? Filter { get; set; }

    /// <summary>
    /// Sort parameters (field and direction).
    /// </summary>
    public List<SortParameter>? Sort { get; set; }

    /// <summary>
    /// Relationships to include in the response.
    /// </summary>
    public List<string>? Include { get; set; }

    /// <summary>
    /// Sparse fieldsets per resource type (e.g., fields[articles]=title,body).
    /// </summary>
    public Dictionary<string, List<string>>? Fields { get; set; }
}

using JsonApiToolkit.Models.Querying.Filtering;

namespace JsonApiToolkit.Models.Querying;

/// <summary>
/// Encapsulates all JSON:API query parameters (pagination, filtering, sorting, and inclusion).
/// </summary>
/// <remarks>
/// <para>
/// This class aggregates all the possible query parameters defined in the JSON:API specification
/// into a single structure. It's used to parse and apply query parameters consistently across
/// the API implementation.
/// </para>
/// <para>
/// All properties are optional, allowing for partial application of query parameters.
/// </para>
/// </remarks>
public class QueryParameters
{
    /// <summary>
    /// Parameters for limiting and paging through collection resources.
    /// </summary>
    /// <remarks>
    /// Based on the page[number] and page[size] query parameter structure.
    /// Used to implement pagination for large collections.
    /// </remarks>
    public PaginationParameters? Pagination { get; set; }

    /// <summary>
    /// Parameters for filtering resources based on attribute values.
    /// </summary>
    /// <remarks>
    /// Represents parsed filter[fieldName] query parameters. Can include simple filters,
    /// complex filters with operators, and logical groups of filters.
    /// </remarks>
    public FilterGroup? Filter { get; set; }

    /// <summary>
    /// Parameters for ordering collection results by specified fields.
    /// </summary>
    /// <remarks>
    /// Based on the sort query parameter. Supports multiple sort fields and
    /// ascending/descending direction for each field.
    /// </remarks>
    public List<SortParameter>? Sort { get; set; }

    /// <summary>
    /// List of relationship paths to include in the response.
    /// </summary>
    /// <remarks>
    /// Based on the include query parameter. Specifies which related resources
    /// should be included in the response to reduce the number of API requests needed.
    /// Supports dot notation for nested relationships (e.g., "author.comments").
    /// </remarks>
    public List<string>? Include { get; set; }
}

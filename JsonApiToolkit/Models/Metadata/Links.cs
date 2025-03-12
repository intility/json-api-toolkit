using System.Text.Json.Serialization;

namespace JsonApiToolkit.Models.Metadata;

/// <summary>
/// Represents a collection of hypermedia links in a JSON:API document.
/// </summary>
/// <remarks>
/// Links provide navigation capabilities between resources and related data in a JSON:API document.
/// They enable clients to traverse the API without having to construct URLs manually.
/// </remarks>
public class Links
{
    /// <summary>
    /// A link to the resource represented by this document.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>
    /// <description>For resource objects, points to the resource itself.</description>
    /// </item>
    /// <item>
    /// <description>For resource collections, points to the collection.</description>
    /// </item>
    /// <item>
    /// <description>For relationship objects, points to the relationship endpoint.</description>
    /// </item>
    /// </list>
    /// </remarks>
    [JsonPropertyName("self")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Self { get; set; }

    /// <summary>
    /// A link to the related resource(s) when in a relationship context.
    /// </summary>
    /// <remarks>
    /// Used in relationship objects to provide direct access to the related resource(s)
    /// without requiring the client to extract and construct the URL.
    /// </remarks>
    [JsonPropertyName("related")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Related { get; set; }

    /// <summary>
    /// A link to the first page of data in a paginated collection.
    /// </summary>
    /// <remarks>
    /// Only relevant for paginated collection responses.
    /// Typically includes page[number]=1 in the query string.
    /// </remarks>
    [JsonPropertyName("first")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? First { get; set; }

    /// <summary>
    /// A link to the last page of data in a paginated collection.
    /// </summary>
    /// <remarks>
    /// Only relevant for paginated collection responses.
    /// Requires knowledge of the total number of pages.
    /// </remarks>
    [JsonPropertyName("last")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Last { get; set; }

    /// <summary>
    /// A link to the previous page of data in a paginated collection.
    /// </summary>
    /// <remarks>
    /// Only relevant for paginated collection responses.
    /// Should be omitted when on the first page.
    /// </remarks>
    [JsonPropertyName("prev")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Prev { get; set; }

    /// <summary>
    /// A link to the next page of data in a paginated collection.
    /// </summary>
    /// <remarks>
    /// Only relevant for paginated collection responses.
    /// Should be omitted when on the last page.
    /// </remarks>
    [JsonPropertyName("next")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Next { get; set; }
}

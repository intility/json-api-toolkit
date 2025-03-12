using System.Text.Json.Serialization;
using JsonApiToolkit.Models.Metadata;
using JsonApiToolkit.Models.Resources;

namespace JsonApiToolkit.Models.Documents;

/// <summary>
/// Represents a JSON:API document containing a single resource as the primary data.
/// </summary>
/// <typeparam name="T">The type of the primary resource (typically ResourceObject)</typeparam>
/// <remarks>
/// <para>
/// Used for endpoints that return a single resource, such as individual GET, POST, or PATCH requests.
/// Follows the JSON:API specification structure with data as a single resource object.
/// </para>
/// <para>
/// Can include related resources in the "included" array, metadata in the "meta" object,
/// and navigation links in the "links" object.
/// </para>
/// </remarks>
public class JsonApiDocument<T>
    where T : class
{
    /// <summary>
    /// The primary data of the document as a single resource.
    /// </summary>
    /// <remarks>
    /// According to the JSON:API specification, this is either a single resource object
    /// or null for an empty response.
    /// </remarks>
    [JsonPropertyName("data")]
    public T? Data { get; set; }

    /// <summary>
    /// Related resources included in the document to reduce the need for additional requests.
    /// </summary>
    /// <remarks>
    /// Contains resource objects that are related to the primary data and requested via the
    /// "include" query parameter. This array is omitted when no related resources are included.
    /// </remarks>
    [JsonPropertyName("included")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<ResourceObject>? Included { get; set; }

    /// <summary>
    /// Non-standard metadata about the document or the resources it contains.
    /// </summary>
    /// <remarks>
    /// Can include arbitrary information such as pagination details, processing statistics,
    /// or any other non-standard information related to the request or response.
    /// </remarks>
    [JsonPropertyName("meta")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Meta { get; set; }

    /// <summary>
    /// Links related to the document and its primary data.
    /// </summary>
    /// <remarks>
    /// Contains links for navigation and relationship traversal, including self links,
    /// pagination links, and other related links as defined by the JSON:API specification.
    /// </remarks>
    [JsonPropertyName("links")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Links? Links { get; set; }
}

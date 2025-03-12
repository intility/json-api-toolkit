using System.Text.Json.Serialization;
using JsonApiToolkit.Models.Metadata;
using JsonApiToolkit.Models.Resources;

namespace JsonApiToolkit.Models.Documents;

/// <summary>
/// Represents a JSON:API document containing a collection of resources as the primary data.
/// </summary>
/// <typeparam name="T">The type of resources in the collection (typically ResourceObject)</typeparam>
/// <remarks>
/// <para>
/// Used for endpoints that return multiple resources, such as collection GET requests.
/// Follows the JSON:API specification structure with data as an array of resources.
/// </para>
/// <para>
/// Can include related resources in the "included" array, metadata in the "meta" object,
/// and navigation links in the "links" object.
/// </para>
/// </remarks>
public class JsonApiCollectionDocument<T>
    where T : class
{
    /// <summary>
    /// The primary data of the document as a collection of resources.
    /// </summary>
    /// <remarks>
    /// According to the JSON:API specification, this is always an array of resource objects
    /// for collection documents, even if empty.
    /// </remarks>
    [JsonPropertyName("data")]
    public IEnumerable<T> Data { get; set; } = [];

    /// <inheritdoc cref="JsonApiDocument{T}.Included"/>
    [JsonPropertyName("included")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<ResourceObject>? Included { get; set; }

    /// <inheritdoc cref="JsonApiDocument{T}.Meta"/>
    [JsonPropertyName("meta")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Meta { get; set; }

    /// <inheritdoc cref="JsonApiDocument{T}.Links"/>
    [JsonPropertyName("links")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Links? Links { get; set; }
}

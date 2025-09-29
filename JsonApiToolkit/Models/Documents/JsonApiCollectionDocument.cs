using System.Text.Json.Serialization;
using JsonApiToolkit.Models.Metadata;
using JsonApiToolkit.Models.Resources;

namespace JsonApiToolkit.Models.Documents;

/// <summary>
/// JSON:API document containing a collection of resources with optional includes, meta, and links.
/// </summary>
public class JsonApiCollectionDocument<T>
    where T : class
{
    /// <summary>
    /// The collection of primary resources.
    /// </summary>
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

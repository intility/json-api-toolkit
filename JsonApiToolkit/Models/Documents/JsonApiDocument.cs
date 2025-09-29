using System.Text.Json.Serialization;
using JsonApiToolkit.Models.Metadata;
using JsonApiToolkit.Models.Resources;

namespace JsonApiToolkit.Models.Documents;

/// <summary>
/// JSON:API document containing a single resource with optional includes, meta, and links.
/// </summary>
public class JsonApiDocument<T>
    where T : class
{
    /// <summary>
    /// The primary resource.
    /// </summary>
    [JsonPropertyName("data")]
    public T? Data { get; set; }

    /// <summary>
    /// Related resources requested via include parameter.
    /// </summary>
    [JsonPropertyName("included")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<ResourceObject>? Included { get; set; }

    /// <summary>
    /// Metadata (pagination, statistics, etc.).
    /// </summary>
    [JsonPropertyName("meta")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Meta { get; set; }

    /// <summary>
    /// Navigation and pagination links.
    /// </summary>
    [JsonPropertyName("links")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Links? Links { get; set; }
}

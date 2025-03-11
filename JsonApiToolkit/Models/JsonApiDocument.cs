using System.Text.Json.Serialization;

namespace JsonApiToolkit.Models;

/// <summary>
/// A JSON:API document.
/// </summary>
/// <typeparam name="T">The type of the primary data.</typeparam>
public class JsonApiDocument<T>
    where T : class
{
    /// <summary>
    /// The primary data of the document.
    /// </summary>
    [JsonPropertyName("data")]
    public T? Data { get; set; }

    /// <summary>
    /// Include related resources in the response. This is optional.
    /// </summary>
    [JsonPropertyName("included")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<ResourceObject>? Included { get; set; }

    /// <summary>
    /// Additional information about the document.
    /// </summary>
    [JsonPropertyName("meta")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Meta { get; set; }

    /// <summary>
    /// Links related to the primary data.
    /// </summary>
    [JsonPropertyName("links")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Links? Links { get; set; }
}

using System.Text.Json.Serialization;

namespace JsonApiToolkit.Models;

/// <summary>
///  A JSON:API document with a collection of resources.
/// </summary>
/// <typeparam name="T">The type of the primary data.</typeparam>
public class JsonApiCollectionDocument<T>
    where T : class
{
    /// <summary>
    /// The collection of resources.
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

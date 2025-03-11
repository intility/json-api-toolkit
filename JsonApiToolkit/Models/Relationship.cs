using System.Text.Json.Serialization;

namespace JsonApiToolkit.Models;

/// <summary>
/// A JSON:API relationship.
/// </summary>
public class Relationship
{
    /// <summary>
    /// The data of the relationship.
    /// </summary>
    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Data { get; set; }

    /// <inheritdoc cref="JsonApiDocument{T}.Links"/>
    [JsonPropertyName("links")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Links? Links { get; set; }
}

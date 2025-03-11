using System.Text.Json.Serialization;
using JsonApiToolkit.Models.Documents;
using JsonApiToolkit.Models.Metadata;

namespace JsonApiToolkit.Models.Resources;

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

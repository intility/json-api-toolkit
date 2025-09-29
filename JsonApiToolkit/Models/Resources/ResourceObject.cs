using System.Text.Json.Serialization;
using JsonApiToolkit.Models.Documents;
using JsonApiToolkit.Models.Metadata;

namespace JsonApiToolkit.Models.Resources;

/// <summary>
/// Resource object with id, type, attributes, relationships, and links.
/// </summary>
public class ResourceObject
{
    /// <summary>
    /// Unique resource identifier.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Resource type (e.g., "articles", "people").
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Resource attributes (properties).
    /// </summary>
    [JsonPropertyName("attributes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Attributes { get; set; }

    /// <summary>
    /// Relationships to other resources.
    /// </summary>
    [JsonPropertyName("relationships")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, Relationship>? Relationships { get; set; }

    /// <summary>
    /// Links related to this resource.
    /// </summary>
    [JsonPropertyName("links")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Links? Links { get; set; }
}

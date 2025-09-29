using System.Text.Json.Serialization;
using JsonApiToolkit.Models.Documents;
using JsonApiToolkit.Models.Metadata;

namespace JsonApiToolkit.Models.Resources;

/// <summary>
/// Relationship to other resources (to-one or to-many).
/// </summary>
public class Relationship
{
    /// <summary>
    /// Resource linkage: null, single ResourceIdentifier, or array of ResourceIdentifiers.
    /// </summary>
    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Data { get; set; }

    /// <summary>
    /// Links related to this relationship.
    /// </summary>
    [JsonPropertyName("links")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Links? Links { get; set; }
}

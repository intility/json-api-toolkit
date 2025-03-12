using System.Text.Json.Serialization;

namespace JsonApiToolkit.Models.Resources;

/// <summary>
/// Represents a resource pointer in JSON:API, containing only the resource type and ID.
/// </summary>
/// <remarks>
/// Resource identifiers are used in relationship linkage to reference related resources
/// without including their attributes. They provide the minimum information needed to
/// locate and identify a specific resource.
/// </remarks>
public class ResourceIdentifier
{
    /// <summary>
    /// The unique identifier of the resource within its type.
    /// </summary>
    /// <remarks>
    /// Combined with the type, forms a globally unique identifier for the resource.
    /// Must be a string, even if the underlying ID is numeric.
    /// </remarks>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The JSON:API resource type.
    /// </summary>
    /// <remarks>
    /// Identifies the type of resource being referenced. Typically corresponds to an entity
    /// type in the system and follows the JSON:API naming convention (usually camelCase).
    /// </remarks>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}

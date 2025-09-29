using System.Text.Json.Serialization;

namespace JsonApiToolkit.Models.Resources;

/// <summary>
/// Resource pointer with type and ID only (used in relationships).
/// </summary>
public class ResourceIdentifier
{
    /// <summary>
    /// Unique resource identifier.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Resource type.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}

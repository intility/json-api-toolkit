using System.Text.Json.Serialization;

namespace JsonApiToolkit.Models;

/// <summary>
/// A JSON:API resource identifier.
/// </summary>
public class ResourceIdentifier
{
    /// <summary>
    /// The resource identifier's ID.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The resource identifier's type.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}

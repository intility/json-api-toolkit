using System.Text.Json.Serialization;

namespace JsonApiToolkit.Models;

/// <summary>
/// The links object.
/// </summary>
public class Links
{
    /// <summary>
    /// The URL of the current request.
    /// </summary>
    [JsonPropertyName("self")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Self { get; set; }

    /// <summary>
    /// The URL of the related request.
    /// </summary>
    [JsonPropertyName("related")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Related { get; set; }

    /// <summary>
    /// The URL of the first page.
    /// </summary>
    [JsonPropertyName("first")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? First { get; set; }

    /// <summary>
    /// The URL of the last page.
    /// </summary>
    [JsonPropertyName("last")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Last { get; set; }

    /// <summary>
    /// The URL of the previous page.
    /// </summary>
    [JsonPropertyName("prev")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Prev { get; set; }

    /// <summary>
    /// The URL of the next page.
    /// </summary>
    [JsonPropertyName("next")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Next { get; set; }
}

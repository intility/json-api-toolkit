using System.Text.Json.Serialization;

namespace JsonApiToolkit.Models.Metadata;

/// <summary>
/// Hypermedia links for navigation and pagination.
/// </summary>
public class Links
{
    /// <summary>
    /// Link to the current resource.
    /// </summary>
    [JsonPropertyName("self")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Self { get; set; }

    /// <summary>
    /// Link to the related resource.
    /// </summary>
    [JsonPropertyName("related")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Related { get; set; }

    /// <summary>
    /// Link to the first page.
    /// </summary>
    [JsonPropertyName("first")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? First { get; set; }

    /// <summary>
    /// Link to the last page.
    /// </summary>
    [JsonPropertyName("last")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Last { get; set; }

    /// <summary>
    /// Link to the previous page.
    /// </summary>
    [JsonPropertyName("prev")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Prev { get; set; }

    /// <summary>
    /// Link to the next page.
    /// </summary>
    [JsonPropertyName("next")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Next { get; set; }
}

using System.Text.Json.Serialization;

namespace JsonApiToolkit.Models.Errors;

/// <summary>
/// Extends JsonApiError to include metadata support.
/// </summary>
public class JsonApiErrorWithMeta : JsonApiError
{
    /// <summary>
    /// A meta object containing non-standard meta-information about the error.
    /// </summary>
    [JsonPropertyName("meta")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Meta { get; set; }
}

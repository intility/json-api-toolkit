using System.Text.Json.Serialization;

namespace JsonApiToolkit.Models.Errors;

/// <summary>
/// JSON:API error response document.
/// </summary>
public class JsonApiErrorResponse
{
    /// <summary>
    /// Collection of error objects.
    /// </summary>
    [JsonPropertyName("errors")]
    public List<JsonApiError> Errors { get; set; } = [];
}

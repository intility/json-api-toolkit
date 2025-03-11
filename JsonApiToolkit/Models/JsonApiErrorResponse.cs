using System.Text.Json.Serialization;

namespace JsonApiToolkit.Models;

/// <summary>
/// A JSON:API error response.
/// </summary>
public class JsonApiErrorResponse
{
    /// <summary>
    /// The JSON:API error objects.
    /// </summary>
    [JsonPropertyName("errors")]
    public List<JsonApiError> Errors { get; set; } = [];
}

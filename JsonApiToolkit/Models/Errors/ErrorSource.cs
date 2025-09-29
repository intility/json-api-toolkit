using System.Text.Json.Serialization;

namespace JsonApiToolkit.Models.Errors;

/// <summary>
/// Identifies the source of an error (JSON pointer or query parameter).
/// </summary>
public class ErrorSource
{
    /// <summary>
    /// JSON Pointer to the error location in request body (e.g., "/data/attributes/title").
    /// </summary>
    [JsonPropertyName("pointer")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Pointer { get; set; }

    /// <summary>
    /// Query parameter that caused the error.
    /// </summary>
    [JsonPropertyName("parameter")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Parameter { get; set; }
}

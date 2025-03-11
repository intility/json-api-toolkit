using System.Text.Json.Serialization;

namespace JsonApiToolkit.Models.Errors;

/// <summary>
/// The source object of a JSON:API error.
/// </summary>
public class ErrorSource
{
    /// <summary>
    /// The source of the error.
    /// </summary>
    [JsonPropertyName("pointer")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Pointer { get; set; }

    /// <summary>
    /// The parameter of the error.
    /// </summary>
    [JsonPropertyName("parameter")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Parameter { get; set; }
}

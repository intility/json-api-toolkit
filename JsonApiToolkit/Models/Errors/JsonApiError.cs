using System.Text.Json.Serialization;

namespace JsonApiToolkit.Models.Errors;

/// <summary>
/// Represents a standardized error object in a JSON:API error response.
/// </summary>
/// <remarks>
/// Follows the JSON:API specification for error objects, providing a consistent structure
/// for conveying error information to clients. Each field is optional but should be used
/// appropriately to provide meaningful error context.
/// </remarks>
public class JsonApiError
{
    /// <summary>
    /// A unique identifier for this specific occurrence of the error.
    /// </summary>
    /// <remarks>
    /// Can be used for logging and tracking purposes. Useful when referencing errors
    /// in server logs.
    /// </remarks>
    [JsonPropertyName("id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Id { get; set; }

    /// <summary>
    /// The HTTP status code applicable to this error.
    /// </summary>
    /// <remarks>
    /// Should match the actual HTTP response status code. Formatted as a string
    /// to allow for application-specific non-numeric codes if needed.
    /// </remarks>
    [JsonPropertyName("status")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Status { get; set; }

    /// <summary>
    /// An application-specific error code.
    /// </summary>
    /// <remarks>
    /// Provides a more specific error categorization than the HTTP status code.
    /// Useful for client-side error handling and display.
    /// </remarks>
    [JsonPropertyName("code")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Code { get; set; }

    /// <summary>
    /// A short, human-readable summary of the error.
    /// </summary>
    /// <remarks>
    /// Should be the same for all occurrences of a given error type.
    /// Typically corresponds to an HTTP status text for the status.
    /// </remarks>
    [JsonPropertyName("title")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Title { get; set; }

    /// <summary>
    /// A human-readable explanation specific to this occurrence of the error.
    /// </summary>
    /// <remarks>
    /// Provides more detailed information than the title. Should clarify what went wrong
    /// and potentially how to fix it. May include instance-specific details.
    /// </remarks>
    [JsonPropertyName("detail")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Detail { get; set; }

    /// <summary>
    /// Information about the source of the error in the request.
    /// </summary>
    /// <remarks>
    /// Helps pinpoint which part of the request caused the error, either in the
    /// request body (pointer) or in query parameters (parameter).
    /// </remarks>
    [JsonPropertyName("source")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ErrorSource? Source { get; set; }
}

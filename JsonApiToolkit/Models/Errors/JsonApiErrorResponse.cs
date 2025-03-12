using System.Text.Json.Serialization;

namespace JsonApiToolkit.Models.Errors;

/// <summary>
/// Represents a JSON:API compliant error response document.
/// </summary>
/// <remarks>
/// <para>
/// According to the JSON:API specification, error responses contain an array of error objects
/// in the "errors" member of the top-level document. This class encapsulates that structure.
/// </para>
/// <para>
/// Error responses must not include any other top-level members alongside "errors".
/// </para>
/// </remarks>
public class JsonApiErrorResponse
{
    /// <summary>
    /// An array of error objects describing the errors that occurred.
    /// </summary>
    /// <remarks>
    /// The JSON:API specification requires that this array contain at least one error object.
    /// Each error object provides information about a specific error condition.
    /// </remarks>
    [JsonPropertyName("errors")]
    public List<JsonApiError> Errors { get; set; } = [];
}

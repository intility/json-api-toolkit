using System.Text.Json.Serialization;

namespace JsonApiToolkit.Models.Errors;

/// <summary>
/// Identifies the specific source of a JSON:API error within a request document or URL parameters.
/// </summary>
/// <remarks>
/// The error source object allows API providers to pinpoint exactly what part of the request
/// led to an error, whether it's a specific field in the request body or a specific query parameter.
/// </remarks>
public class ErrorSource
{
    /// <summary>
    /// A JSON Pointer to the associated entity in the request document.
    /// </summary>
    /// <remarks>
    /// Examples:
    /// <list type="bullet">
    /// <item>
    /// <description>"/data" for errors related to the entire resource object</description>
    /// </item>
    /// <item>
    /// <description>"/data/attributes/title" for errors related to a specific attribute</description>
    /// </item>
    /// <item>
    /// <description>"/data/relationships/author" for errors related to a relationship</description>
    /// </item>
    /// </list>
    /// Only applicable for errors related to the request body.
    /// </remarks>
    [JsonPropertyName("pointer")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Pointer { get; set; }

    /// <summary>
    /// The URL query parameter that caused the error.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Used for errors related to query parameters, such as invalid filter syntax,
    /// unsupported include paths, or pagination issues.
    /// </para>
    /// <para>
    /// Only applicable for errors related to query parameters.
    /// </para>
    /// </remarks>
    [JsonPropertyName("parameter")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Parameter { get; set; }
}

using JsonApiToolkit.Models.Querying;
using Microsoft.AspNetCore.Http;

namespace JsonApiToolkit.Services;

/// <summary>
/// Service interface for parsing JSON:API query parameters with logging support.
/// </summary>
public interface IJsonApiQueryParser
{
    /// <summary>
    /// Parses all JSON:API query parameters from an HTTP request into a structured QueryParameters object.
    /// </summary>
    /// <param name="request">The HTTP request containing the query parameters</param>
    /// <returns>A QueryParameters object containing all parsed query parameters</returns>
    QueryParameters Parse(HttpRequest request);
}

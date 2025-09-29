using JsonApiToolkit.Models.Querying;
using Microsoft.AspNetCore.Http;

namespace JsonApiToolkit.Services;

/// <summary>
/// Service interface for parsing JSON:API query parameters.
/// </summary>
public interface IJsonApiQueryParser
{
    /// <summary>
    /// Parses JSON:API query parameters from an HTTP request.
    /// </summary>
    QueryParameters Parse(HttpRequest request);
}

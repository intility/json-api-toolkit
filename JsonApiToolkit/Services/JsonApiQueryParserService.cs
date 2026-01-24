using JsonApiToolkit.Configuration;
using JsonApiToolkit.Models.Querying;
using JsonApiToolkit.Parsing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JsonApiToolkit.Services;

/// <summary>
/// Service implementation for parsing JSON:API query parameters.
/// </summary>
public class JsonApiQueryParserService : IJsonApiQueryParser
{
    private readonly ILogger<JsonApiQueryParserService> _logger;
    private readonly JsonApiOptions _options;

    /// <summary>
    /// Initializes a new instance of the query parser service.
    /// </summary>
    public JsonApiQueryParserService(
        ILogger<JsonApiQueryParserService> logger,
        IOptions<JsonApiOptions> options
    )
    {
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>
    /// Parses JSON:API query parameters from an HTTP request.
    /// </summary>
    public QueryParameters Parse(HttpRequest request)
    {
        var queryParams = JsonApiQueryParser.Parse(request, _options, _logger);

        if (
            request.Query.Keys.Any(k => k.StartsWith("filter", StringComparison.OrdinalIgnoreCase))
            && (queryParams.Filter?.Filters?.Count ?? 0) == 0
        )
        {
            _logger.LogWarning(
                "Filter parameters detected but no valid filters parsed. Check syntax: filter[field][operator]=value"
            );
        }

        if (
            request.Query.Keys.Any(k => k.StartsWith("sort", StringComparison.OrdinalIgnoreCase))
            && (queryParams.Sort?.Count ?? 0) == 0
        )
        {
            _logger.LogWarning(
                "Sort parameter detected but no valid sorts parsed. Check syntax: sort=field1,-field2"
            );
        }

        if (
            request.Query.Keys.Any(k => k.StartsWith("page", StringComparison.OrdinalIgnoreCase))
            && queryParams.Pagination == null
        )
        {
            _logger.LogWarning(
                "Page parameters detected but no pagination parsed. Use: page[number]=1&page[size]=10"
            );
        }

        // Validate query complexity against configured limits
        QueryComplexityAnalyzer.Validate(queryParams, _options);

        return queryParams;
    }
}

using JsonApiToolkit.Models.Querying;
using JsonApiToolkit.Parsing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace JsonApiToolkit.Services;

/// <summary>
/// Service implementation for parsing JSON:API query parameters with comprehensive debug logging.
/// </summary>
public class JsonApiQueryParserService : IJsonApiQueryParser
{
    private readonly ILogger<JsonApiQueryParserService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonApiQueryParserService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance for logging parsing operations</param>
    public JsonApiQueryParserService(ILogger<JsonApiQueryParserService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public QueryParameters Parse(HttpRequest request)
    {
        _logger.LogDebug(
            "Starting to parse JSON:API query parameters from request: {RequestPath}?{QueryString}",
            request.Path,
            request.QueryString
        );

        var queryParams = JsonApiQueryParser.Parse(request);

        _logger.LogDebug(
            "Successfully parsed query parameters: Filters={FilterCount}, Sorts={SortCount}, Includes={IncludeCount}, HasPagination={HasPagination}",
            queryParams.Filter?.Filters?.Count ?? 0,
            queryParams.Sort?.Count ?? 0,
            queryParams.Include?.Count ?? 0,
            queryParams.Pagination != null
        );

        // User-friendly warnings for common parameter issues
        if (
            request.Query.Keys.Any(k => k.StartsWith("filter", StringComparison.OrdinalIgnoreCase))
            && (queryParams.Filter?.Filters?.Count ?? 0) == 0
        )
        {
            _logger.LogWarning(
                "Filter parameters detected in query string but no valid filters parsed. Check filter syntax: filter[fieldName][operator]=value. Example: filter[name][like]=John"
            );
        }

        if (
            request.Query.Keys.Any(k => k.StartsWith("sort", StringComparison.OrdinalIgnoreCase))
            && (queryParams.Sort?.Count ?? 0) == 0
        )
        {
            _logger.LogWarning(
                "Sort parameter detected but no valid sorts parsed. Check sort syntax: sort=field1,-field2. Example: sort=name,-createdAt"
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

        if (queryParams.Pagination != null)
        {
            _logger.LogDebug(
                "Pagination parameters: Page={PageNumber}, Size={PageSize}",
                queryParams.Pagination.Number,
                queryParams.Pagination.Size
            );
        }

        if (queryParams.Filter != null)
        {
            _logger.LogDebug(
                "Filter details: DirectFilters={DirectFilterCount}, Groups={GroupCount}",
                queryParams.Filter.Filters?.Count ?? 0,
                queryParams.Filter.Groups?.Count ?? 0
            );
        }

        if (queryParams.Sort?.Count > 0)
        {
            var sortFields = string.Join(
                ", ",
                queryParams.Sort.Select(s => $"{s.Field}({(s.IsDescending ? "desc" : "asc")})")
            );
            _logger.LogDebug("Sort fields: {SortFields}", sortFields);
        }

        if (queryParams.Include?.Count > 0)
        {
            _logger.LogDebug(
                "Include relationships: {IncludeFields}",
                string.Join(", ", queryParams.Include)
            );
        }

        return queryParams;
    }
}

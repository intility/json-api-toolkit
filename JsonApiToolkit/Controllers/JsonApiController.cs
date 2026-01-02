using JsonApiToolkit.Extensions;
using JsonApiToolkit.Extensions.Querying;
using JsonApiToolkit.Filters;
using JsonApiToolkit.Helpers;
using JsonApiToolkit.Mapping;
using JsonApiToolkit.Models.Documents;
using JsonApiToolkit.Models.Errors;
using JsonApiToolkit.Models.Metadata;
using JsonApiToolkit.Models.Querying;
using JsonApiToolkit.Models.Resources;
using JsonApiToolkit.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JsonApiToolkit.Controllers;

/// <summary>
/// Base controller for JSON:API compliant responses.
/// Handles content negotiation and applies JsonApiExceptionFilter automatically.
/// </summary>
[Produces("application/vnd.api+json")]
[Consumes("application/vnd.api+json")]
[ServiceFilter(typeof(JsonApiExceptionFilter))]
public abstract class JsonApiController : ControllerBase
{
    private ILogger<JsonApiController>? _logger;
    private IJsonApiQueryParser? _queryParser;

    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected ILogger<JsonApiController> Logger =>
        _logger ??= HttpContext.RequestServices.GetRequiredService<ILogger<JsonApiController>>();

    /// <summary>
    /// Gets the query parser service.
    /// </summary>
    protected IJsonApiQueryParser QueryParser =>
        _queryParser ??= HttpContext.RequestServices.GetRequiredService<IJsonApiQueryParser>();

    /// <summary>
    /// Parses JSON:API query parameters (filter, sort, page, include).
    /// </summary>
    protected QueryParameters GetJsonApiQueryParameters()
    {
        return QueryParser.Parse(Request);
    }

    /// <summary>
    /// Applies only filtering from JSON:API query parameters to a queryable.
    /// Useful when you need to filter before aggregation/projection to DTOs.
    /// </summary>
    /// <typeparam name="T">The entity type to filter.</typeparam>
    /// <param name="queryable">The queryable to apply filters to.</param>
    /// <returns>The filtered queryable.</returns>
    /// <remarks>
    /// Use this when working with projections/DTOs where you need to apply filters
    /// to the source entity before grouping or projecting to a DTO.
    /// </remarks>
    protected IQueryable<T> ApplyFiltersOnly<T>(IQueryable<T> queryable)
        where T : class
    {
        QueryParameters parameters = GetJsonApiQueryParameters();
        if (parameters.Filter == null)
            return queryable;

        return queryable.ApplyFilters(parameters.Filter, Logger);
    }

    /// <summary>
    /// Returns 200 OK with a single resource as JSON:API document.
    /// </summary>
    protected IActionResult JsonApiOk<T>(T entity, string resourceType)
        where T : class
    {
        QueryParameters parameters = GetJsonApiQueryParameters();
        var mappedIncludes = EfIncludePathHelper.MapIncludePathsToClrProperties<T>(
            parameters.Include
        );

        string baseUrl = $"{Request.Scheme}://{Request.Host}{Request.Path}";
        JsonApiDocument<ResourceObject> document = JsonApiMapper.ToDocument(
            entity,
            resourceType,
            baseUrl,
            mappedIncludes,
            Logger
        );
        return Ok(document);
    }

    /// <summary>
    /// Returns 200 OK for a single resource queryable with JSON:API query support (filter, include).
    /// Use this when you need includes to be automatically loaded from the database.
    /// </summary>
    protected async Task<IActionResult> JsonApiOkAsync<T>(
        IQueryable<T> queryable,
        string resourceType
    )
        where T : class
    {
        QueryParameters parameters = GetJsonApiQueryParameters();

        var mappedIncludes = EfIncludePathHelper.MapIncludePathsToClrProperties<T>(
            parameters.Include
        );

        var (mainFilters, includeFilters) = IncludeFilterParser.SeparateIncludeFilters(
            parameters.Filter,
            parameters.Include
        );

        IQueryable<T> filteredQuery = queryable;

        // Apply main entity filters
        if (mainFilters != null)
            filteredQuery = filteredQuery.ApplyFilters(mainFilters, Logger);

        // Apply includes (with or without filters)
        if (includeFilters.Count > 0)
        {
            filteredQuery = filteredQuery.ApplyFilteredIncludes(
                mappedIncludes,
                includeFilters,
                Logger
            );
        }
        else if (mappedIncludes.Count > 0)
        {
            filteredQuery = filteredQuery.ApplyIncludes(mappedIncludes);
        }

        // Execute query for single entity
        T? entity = await filteredQuery.FirstOrDefaultAsync().ConfigureAwait(false);

        if (entity == null)
            return JsonApiNotFound();

        // Use existing JsonApiOk - entity now has includes loaded
        return JsonApiOk(entity, resourceType);
    }

    /// <summary>
    /// Returns 200 OK with a collection of resources as JSON:API document.
    /// </summary>
    protected IActionResult JsonApiOk<T>(
        IEnumerable<T> entities,
        string resourceType,
        PaginationMeta? paginationMeta = null
    )
        where T : class
    {
        QueryParameters parameters = GetJsonApiQueryParameters();
        var mappedIncludes = EfIncludePathHelper.MapIncludePathsToClrProperties<T>(
            parameters.Include
        );

        string baseUrl = GetFullRequestUrl();
        JsonApiCollectionDocument<ResourceObject> document = JsonApiMapper.ToCollectionDocument(
            entities,
            resourceType,
            baseUrl,
            paginationMeta,
            mappedIncludes,
            Logger
        );
        return Ok(document);
    }

    /// <summary>
    /// Returns 200 OK for queryable with full JSON:API query support (filter, sort, page, include).
    /// </summary>
    protected async Task<IActionResult> JsonApiQueryAsync<T>(
        IQueryable<T> queryable,
        string resourceType
    )
        where T : class
    {
        QueryParameters parameters = GetJsonApiQueryParameters();

        Logger.LogDebug(
            "Query for {EntityType}: Filters={FilterCount}, Sorts={SortCount}, Includes={IncludeCount}, Pagination={HasPagination}",
            typeof(T).Name,
            parameters.Filter?.Filters?.Count ?? 0,
            parameters.Sort?.Count ?? 0,
            parameters.Include?.Count ?? 0,
            parameters.Pagination != null
        );

        if (parameters.Filter?.Filters?.Count > 20)
        {
            Logger.LogInformation(
                "Complex query with {Count} filters on {EntityType}",
                parameters.Filter.Filters.Count,
                typeof(T).Name
            );
        }

        string baseUrl = GetFullRequestUrl();
        var mappedIncludes = EfIncludePathHelper.MapIncludePathsToClrProperties<T>(
            parameters.Include
        );

        if (parameters.Include?.Count > 0 && mappedIncludes.Count == 0)
        {
            Logger.LogWarning(
                "No valid includes for {EntityType}. Requested: {Includes}",
                typeof(T).Name,
                string.Join(", ", parameters.Include)
            );
        }

        var (mainFilters, includeFilters) = IncludeFilterParser.SeparateIncludeFilters(
            parameters.Filter,
            parameters.Include
        );

        IQueryable<T> filteredQuery = queryable;

        if (mainFilters != null)
            filteredQuery = filteredQuery.ApplyFilters(mainFilters, Logger);

        if (includeFilters.Count > 0)
        {
            Logger.LogDebug(
                "Applying {FilterCount} filtered includes for {EntityType}",
                includeFilters.Count,
                typeof(T).Name
            );
            filteredQuery = filteredQuery.ApplyFilteredIncludes(
                mappedIncludes,
                includeFilters,
                Logger
            );
        }
        else if (mappedIncludes.Count > 0)
        {
            // Use single query with pagination to avoid EF Core split query issues
            filteredQuery =
                parameters.Pagination != null
                    ? filteredQuery.ApplyIncludesSingleQuery(mappedIncludes)
                    : filteredQuery.ApplyIncludes(mappedIncludes);

            Logger.LogDebug(
                "Applied {IncludeCount} includes for {EntityType} using {QueryType}",
                mappedIncludes.Count,
                typeof(T).Name,
                parameters.Pagination != null ? "SingleQuery" : "SplitQuery"
            );
        }

        if (parameters.Sort?.Count > 0)
            filteredQuery = filteredQuery.ApplySorting(parameters.Sort, Logger);

        int totalCount = await filteredQuery.CountAsync().ConfigureAwait(false);

        if (totalCount == 0 && parameters.Filter?.Filters?.Count > 0)
        {
            Logger.LogInformation("Query returned 0 results for {EntityType}", typeof(T).Name);
        }
        else if (totalCount > 1000 && parameters.Pagination == null)
        {
            Logger.LogWarning(
                "Large result set ({TotalCount}) without pagination. Consider adding pagination to improve performance",
                totalCount
            );
        }

        if (parameters.Pagination != null)
            filteredQuery = filteredQuery.ApplyPagination(parameters.Pagination);

        PaginationMeta? paginationMeta = null;
        if (parameters.Pagination != null)
        {
            paginationMeta = new PaginationMeta
            {
                TotalResources = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)parameters.Pagination.Size),
                CurrentPage = parameters.Pagination.Number,
                PageSize = parameters.Pagination.Size,
            };
        }

        Logger.LogDebug(
            "Executing query for {EntityType}: TotalCount={TotalCount}, Returning={ReturnCount}",
            typeof(T).Name,
            totalCount,
            parameters.Pagination?.Size ?? totalCount
        );

        List<T> results = await filteredQuery.ToListAsync().ConfigureAwait(false);

        JsonApiCollectionDocument<ResourceObject> document = JsonApiMapper.ToCollectionDocument(
            results,
            resourceType,
            baseUrl,
            paginationMeta,
            mappedIncludes,
            Logger
        );

        return Ok(document);
    }

    /// <summary>
    /// Returns 201 Created with new resource and Location header.
    /// </summary>
    protected IActionResult JsonApiCreated<T>(T entity, string resourceType, string id)
        where T : class
    {
        QueryParameters parameters = GetJsonApiQueryParameters();
        var mappedIncludes = EfIncludePathHelper.MapIncludePathsToClrProperties<T>(
            parameters.Include
        );

        string baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}{Request.Path}";
        string selfUrl = $"{baseUrl}/{id}";
        JsonApiDocument<ResourceObject> document = JsonApiMapper.ToDocument(
            entity,
            resourceType,
            selfUrl,
            mappedIncludes,
            Logger
        );
        return Created(selfUrl, document);
    }

    /// <summary>
    /// Returns 204 No Content (for DELETE/PUT operations).
    /// </summary>
    protected IActionResult JsonApiNoContent() => NoContent();

    /// <summary>
    /// Returns 404 Not Found with JSON:API error.
    /// </summary>
    protected IActionResult JsonApiNotFound(string detail = "Resource not found")
    {
        var error = new JsonApiError
        {
            Status = "404",
            Title = "Not Found",
            Detail = detail,
        };
        return NotFound(new JsonApiErrorResponse { Errors = [error] });
    }

    /// <summary>
    /// Returns 400 Bad Request with JSON:API error.
    /// </summary>
    protected IActionResult JsonApiBadRequest(string detail)
    {
        var error = new JsonApiError
        {
            Status = "400",
            Title = "Bad Request",
            Detail = detail,
        };
        return BadRequest(new JsonApiErrorResponse { Errors = [error] });
    }

    /// <summary>
    /// Gets full request URL for self/pagination links.
    /// </summary>
    protected string GetFullRequestUrl() =>
        $"{Request.Scheme}://{Request.Host}{Request.Path}{Request.QueryString}";
}

using JsonApiToolkit.Configuration;
using JsonApiToolkit.Extensions;
using JsonApiToolkit.Extensions.Projection;
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
using Microsoft.Extensions.Options;

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
    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected ILogger<JsonApiController> Logger =>
        field ??= HttpContext.RequestServices.GetRequiredService<ILogger<JsonApiController>>();

    /// <summary>
    /// Gets the query parser service.
    /// </summary>
    protected IJsonApiQueryParser QueryParser =>
        field ??= HttpContext.RequestServices.GetRequiredService<IJsonApiQueryParser>();

    /// <summary>
    /// Gets the configured JsonApiOptions.
    /// </summary>
    protected JsonApiOptions Options =>
        field ??= HttpContext.RequestServices.GetRequiredService<IOptions<JsonApiOptions>>().Value;

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

        return queryable.ApplyFilters(parameters.Filter, Logger, Options.StrictQueryValidation);
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
            Logger,
            parameters.Fields
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
            parameters.Include,
            Options.StrictQueryValidation
        );

        IQueryable<T> filteredQuery = queryable;

        // Apply main entity filters
        if (mainFilters != null)
            filteredQuery = filteredQuery.ApplyFilters(
                mainFilters,
                Logger,
                Options.StrictQueryValidation
            );

        // Apply includes (with or without filters)
        if (includeFilters.Count > 0)
        {
            filteredQuery = filteredQuery.ApplyFilteredIncludes(
                mappedIncludes,
                includeFilters,
                Logger,
                Options.StrictQueryValidation
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
            Logger,
            parameters.Fields
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
        string baseUrl = GetFullRequestUrl();
        var mappedIncludes = EfIncludePathHelper.MapIncludePathsToClrProperties<T>(
            parameters.Include
        );

        LogQueryParameters<T>(parameters, mappedIncludes);

        IQueryable<T> filteredQuery = ApplyFiltersAndIncludes(
            queryable,
            parameters,
            mappedIncludes,
            paginating: parameters.Pagination != null
        );

        if (parameters.Sort?.Count > 0)
            filteredQuery = filteredQuery.ApplySorting(
                parameters.Sort,
                Logger,
                Options.StrictQueryValidation
            );

        int totalCount = await filteredQuery.CountAsync().ConfigureAwait(false);
        LogCountResults<T>(parameters, totalCount);

        EnforceStrictPagination(Options, parameters, totalCount);

        if (parameters.Pagination != null)
            filteredQuery = filteredQuery.ApplyPagination(parameters.Pagination, totalCount);

        PaginationMeta? paginationMeta =
            parameters.Pagination != null
                ? PaginationHandler.CreatePaginationMeta(parameters.Pagination, totalCount)
                : null;

        Logger.LogDebug(
            "Executing query for {EntityType}: TotalCount={TotalCount}, Returning={ReturnCount}",
            typeof(T).Name,
            totalCount,
            parameters.Pagination?.Size ?? totalCount
        );

        IActionResult? projectionResult = await TryApplyDatabaseProjectionAsync(
            filteredQuery,
            resourceType,
            baseUrl,
            paginationMeta,
            mappedIncludes,
            parameters
        );
        if (projectionResult != null)
            return projectionResult;

        List<T> results = await filteredQuery.ToListAsync().ConfigureAwait(false);

        JsonApiCollectionDocument<ResourceObject> document = JsonApiMapper.ToCollectionDocument(
            results,
            resourceType,
            baseUrl,
            paginationMeta,
            mappedIncludes,
            Logger,
            parameters.Fields
        );

        return Ok(document);
    }

    /// <summary>
    /// Builds a JSON:API query with filters, includes, and sorting applied, but WITHOUT pagination.
    /// Use this for custom operations like CSV exports, aggregations, or projections.
    /// </summary>
    /// <typeparam name="T">The entity type to query.</typeparam>
    /// <param name="queryable">The queryable to process.</param>
    /// <param name="resourceType">The JSON:API resource type name.</param>
    /// <param name="includeCount">Whether to execute a COUNT query. Set to false to skip for performance.</param>
    /// <returns>A result containing the processed query, parameters, and optional count.</returns>
    protected async Task<JsonApiQueryResult<T>> BuildJsonApiQueryAsync<T>(
        IQueryable<T> queryable,
        string resourceType,
        bool includeCount = true
    )
        where T : class
    {
        QueryParameters parameters = GetJsonApiQueryParameters();

        Logger.LogDebug(
            "BuildQuery for {EntityType}: Filters={FilterCount}, Sorts={SortCount}, Includes={IncludeCount}, Fields={FieldsCount}",
            typeof(T).Name,
            parameters.Filter?.Filters?.Count ?? 0,
            parameters.Sort?.Count ?? 0,
            parameters.Include?.Count ?? 0,
            parameters.Fields?.Count ?? 0
        );

        var mappedIncludes = EfIncludePathHelper.MapIncludePathsToClrProperties<T>(
            parameters.Include
        );

        LogInvalidIncludes<T>(parameters, mappedIncludes);

        IQueryable<T> processedQuery = ApplyFiltersAndIncludes(
            queryable,
            parameters,
            mappedIncludes,
            paginating: false
        );

        if (parameters.Sort?.Count > 0)
            processedQuery = processedQuery.ApplySorting(
                parameters.Sort,
                Logger,
                Options.StrictQueryValidation
            );

        // Get count if requested
        int totalCount = 0;
        if (includeCount)
        {
            totalCount = await processedQuery.CountAsync().ConfigureAwait(false);

            Logger.LogDebug(
                "BuildQuery for {EntityType}: TotalCount={TotalCount}",
                typeof(T).Name,
                totalCount
            );
        }

        return new JsonApiQueryResult<T>
        {
            Query = processedQuery,
            Parameters = parameters,
            TotalCount = totalCount,
        };
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
            Logger,
            parameters.Fields
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

    private void LogQueryParameters<T>(QueryParameters parameters, List<string> mappedIncludes)
    {
        Logger.LogDebug(
            "Query for {EntityType}: Filters={FilterCount}, Sorts={SortCount}, Includes={IncludeCount}, Pagination={HasPagination}, Fields={FieldsCount}",
            typeof(T).Name,
            parameters.Filter?.Filters?.Count ?? 0,
            parameters.Sort?.Count ?? 0,
            parameters.Include?.Count ?? 0,
            parameters.Pagination != null,
            parameters.Fields?.Count ?? 0
        );

        if (parameters.Filter?.Filters?.Count > 20)
        {
            Logger.LogInformation(
                "Complex query with {Count} filters on {EntityType}",
                parameters.Filter.Filters.Count,
                typeof(T).Name
            );
        }

        LogInvalidIncludes<T>(parameters, mappedIncludes);
    }

    // When `paginating` is true, includes use single-query mode to avoid the
    // EF Core warning/exception triggered by split-query + Skip/Take. Otherwise
    // split-query is preferred to avoid cartesian explosion on collection includes.
    private IQueryable<T> ApplyFiltersAndIncludes<T>(
        IQueryable<T> queryable,
        QueryParameters parameters,
        List<string> mappedIncludes,
        bool paginating
    )
        where T : class
    {
        var (mainFilters, includeFilters) = IncludeFilterParser.SeparateIncludeFilters(
            parameters.Filter,
            parameters.Include,
            Options.StrictQueryValidation
        );

        IQueryable<T> filteredQuery = queryable;

        if (mainFilters != null)
            filteredQuery = filteredQuery.ApplyFilters(
                mainFilters,
                Logger,
                Options.StrictQueryValidation
            );

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
                Logger,
                Options.StrictQueryValidation
            );
        }
        else if (mappedIncludes.Count > 0)
        {
            filteredQuery = paginating
                ? filteredQuery.ApplyIncludesSingleQuery(mappedIncludes)
                : filteredQuery.ApplyIncludes(mappedIncludes);

            Logger.LogDebug(
                "Applied {IncludeCount} includes for {EntityType} using {QueryType}",
                mappedIncludes.Count,
                typeof(T).Name,
                paginating ? "SingleQuery" : "SplitQuery"
            );
        }

        return filteredQuery;
    }

    private void LogInvalidIncludes<T>(QueryParameters parameters, List<string> mappedIncludes)
    {
        if (parameters.Include?.Count > 0 && mappedIncludes.Count == 0)
        {
            Logger.LogWarning(
                "No valid includes for {EntityType}. Requested: {Includes}",
                typeof(T).Name,
                string.Join(", ", parameters.Include)
            );
        }
    }

    private static void EnforceStrictPagination(
        JsonApiOptions options,
        QueryParameters parameters,
        int totalCount
    )
    {
        if (!options.StrictPagination || parameters.Pagination is null || totalCount == 0)
            return;

        int totalPages = (int)Math.Ceiling(totalCount / (double)parameters.Pagination.Size);
        if (parameters.Pagination.Number <= totalPages)
            return;

        throw new JsonApiNotFoundException(
            $"Page {parameters.Pagination.Number} does not exist. "
                + $"This collection has {totalPages} page(s). Request a page between 1 and {totalPages}.",
            JsonApiErrorCodes.InvalidPageNumber,
            new ErrorSource { Parameter = "page[number]" },
            new Dictionary<string, object>
            {
                ["value"] = parameters.Pagination.Number,
                ["totalPages"] = totalPages,
                ["totalResources"] = totalCount,
            }
        );
    }

    private void LogCountResults<T>(QueryParameters parameters, int totalCount)
    {
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
    }

    private async Task<IActionResult?> TryApplyDatabaseProjectionAsync<T>(
        IQueryable<T> filteredQuery,
        string resourceType,
        string baseUrl,
        PaginationMeta? paginationMeta,
        List<string> mappedIncludes,
        QueryParameters parameters
    )
        where T : class
    {
        if (!Options.EnableDatabaseProjection || parameters.Fields == null)
            return null;

        if (mappedIncludes.Count > 0)
        {
            Logger.LogDebug(
                "Database projection skipped for {EntityType}: includes are not compatible with Select() projection",
                typeof(T).Name
            );
            return null;
        }

        if (
            parameters.Fields.TryGetValue(resourceType, out List<string>? requestedFields)
            && requestedFields.Count > 0
        )
        {
            try
            {
                var projectionProperties = ProjectionPropertySelector.Determine(
                    typeof(T),
                    requestedFields
                );

                var (projectionType, projectionExpression) = ProjectionTypeCache.GetOrCreate(
                    typeof(T),
                    projectionProperties
                );

                IQueryable projectedQuery = DatabaseProjectionApplicator.ApplySelect(
                    filteredQuery,
                    projectionType,
                    projectionExpression
                );

                List<object> projectedResults = await DatabaseProjectionApplicator
                    .MaterializeAsync(projectedQuery, projectionType, HttpContext.RequestAborted)
                    .ConfigureAwait(false);

                Logger.LogDebug(
                    "Database projection applied for {EntityType}: {FieldCount} fields projected",
                    typeof(T).Name,
                    requestedFields.Count
                );

                JsonApiCollectionDocument<ResourceObject> projectedDocument =
                    JsonApiMapper.ToCollectionDocument(
                        projectedResults,
                        resourceType,
                        baseUrl,
                        paginationMeta,
                        mappedIncludes,
                        Logger,
                        parameters.Fields
                    );

                return Ok(projectedDocument);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(
                    ex,
                    "Database projection failed for {EntityType}, falling back to full entity load",
                    typeof(T).Name
                );
                return null;
            }
        }

        if (parameters.Fields.Count > 0)
        {
            Logger.LogDebug(
                "Database projection skipped for {EntityType}: fields[] present but no key matches resourceType '{ResourceType}'. Keys: {Keys}",
                typeof(T).Name,
                resourceType,
                string.Join(", ", parameters.Fields.Keys)
            );
        }

        return null;
    }
}

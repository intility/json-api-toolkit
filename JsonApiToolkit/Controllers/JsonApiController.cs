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
/// Base controller class that implements JSON:API specification-compliant responses and request handling.
/// Provides standardized methods for returning JSON:API document structures with proper content negotiation.
/// </summary>
/// <remarks>
/// Automatically configures content type handling for "application/vnd.api+json" and applies the JsonApiExceptionFilter.
/// Use this as the base class for all controllers that need to return JSON:API compliant responses.
/// </remarks>
[Produces("application/vnd.api+json")]
[Consumes("application/vnd.api+json")]
[ServiceFilter(typeof(JsonApiExceptionFilter))]
public abstract class JsonApiController : ControllerBase
{
    private ILogger<JsonApiController>? _logger;
    private IJsonApiQueryParser? _queryParser;

    /// <summary>
    /// Gets the logger instance from dependency injection.
    /// </summary>
    protected ILogger<JsonApiController> Logger => 
        _logger ??= HttpContext.RequestServices.GetRequiredService<ILogger<JsonApiController>>();

    /// <summary>
    /// Gets the query parser instance from dependency injection.
    /// </summary>
    protected IJsonApiQueryParser QueryParser => 
        _queryParser ??= HttpContext.RequestServices.GetRequiredService<IJsonApiQueryParser>();

    /// <summary>
    /// Extracts and parses JSON:API query parameters from the current HTTP request.
    /// </summary>
    /// <returns>A QueryParameters object containing parsed filter, sort, pagination, and include parameters.</returns>
    /// <remarks>
    /// Handles standard JSON:API query parameter formats including:
    /// <list type="bullet">
    ///   <item>
    ///     <description><c>filter[fieldName]=value</c></description>
    ///   </item>
    ///   <item>
    ///     <description><c>sort=field or sort=-descendingField</c></description>
    ///   </item>
    ///   <item>
    ///     <description><c>page[number]=1&amp;page[size]=10</c></description>
    ///   </item>
    ///   <item>
    ///     <description><c>include=relationship1,relationship2</c></description>
    ///   </item>
    /// </list>
    /// </remarks>
    protected QueryParameters GetJsonApiQueryParameters()
    {
        return QueryParser.Parse(Request);
    }

    /// <summary>
    /// Creates a 200 OK response containing a single resource as a JSON:API document.
    /// </summary>
    /// <typeparam name="T">The entity type being returned</typeparam>
    /// <param name="entity">The already-loaded entity to serialize into the response</param>
    /// <param name="resourceType">The JSON:API resource type identifier (typically the entity name in camelCase)</param>
    /// <returns>An IActionResult with a properly formatted JSON:API document</returns>
    /// <remarks>
    /// Serializes the provided entity into JSON:API format. Any relationships that are already loaded
    /// on the entity will be included in the response.
    /// </remarks>
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
    /// Creates a 200 OK response containing a collection of resources as a JSON:API document.
    /// </summary>
    /// <typeparam name="T">The entity type of the collection items</typeparam>
    /// <param name="entities">The already-loaded collection of entities to serialize into the response</param>
    /// <param name="resourceType">The JSON:API resource type identifier (typically the entity name in camelCase)</param>
    /// <param name="paginationMeta">Optional pagination metadata to include in the response</param>
    /// <returns>An IActionResult with a properly formatted JSON:API collection document</returns>
    /// <remarks>
    /// Serializes the provided collection into JSON:API format. Any relationships that are already loaded
    /// on the entities will be included in the response. When pagination metadata is provided, adds pagination links.
    /// </remarks>
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
    /// Creates a 200 OK response for a queryable collection with full JSON:API query parameter support.
    /// </summary>
    /// <typeparam name="T">The entity type of the queryable items</typeparam>
    /// <param name="queryable">The queryable collection to apply filters, sorting, and pagination to</param>
    /// <param name="resourceType">The JSON:API resource type identifier (typically the entity name in camelCase)</param>
    /// <returns>An IActionResult with a properly formatted JSON:API collection document with query parameters applied</returns>
    /// <remarks>
    /// This method provides comprehensive support for JSON:API query parameters:
    /// <list type="bullet">
    ///   <item>
    ///     <description>Automatically applies any filter parameters to the queryable</description>
    ///   </item>
    ///   <item>
    ///     <description>Applies sorting based on sort parameters</description>
    ///   </item>
    ///   <item>
    ///     <description>Handles pagination and generates pagination metadata and links</description>
    ///   </item>
    ///   <item>
    ///     <description>Processes includes to add related resources</description>
    ///   </item>
    /// </list>
    /// This is the recommended method for collection endpoints as it implements the complete JSON:API querying capabilities.
    /// </remarks>
    protected async Task<IActionResult> JsonApiQueryAsync<T>(
        IQueryable<T> queryable,
        string resourceType
    )
        where T : class
    {
        Logger.LogDebug(
            "Starting JSON:API query processing for resource type '{ResourceType}'",
            resourceType
        );

        QueryParameters parameters = GetJsonApiQueryParameters();
        Logger.LogDebug(
            "Parsed query parameters: Filters={FilterCount}, Sorts={SortCount}, Includes={IncludeCount}, Pagination={HasPagination}",
            parameters.Filter?.Filters?.Count ?? 0,
            parameters.Sort?.Count ?? 0,
            parameters.Include?.Count ?? 0,
            parameters.Pagination != null
        );

        // User-friendly warnings for common issues
        if (parameters.Include?.Count > 0)
        {
            Logger.LogInformation(
                "Processing includes: {Includes}. If you get errors, ensure these relationships exist on {EntityType}",
                string.Join(", ", parameters.Include), 
                typeof(T).Name
            );
        }

        if (parameters.Filter?.Filters?.Count > 10)
        {
            Logger.LogWarning(
                "Large number of filters detected ({FilterCount}). This may impact performance. Consider simplifying the query",
                parameters.Filter.Filters.Count
            );
        }

        string baseUrl = GetFullRequestUrl();
        var mappedIncludes = EfIncludePathHelper.MapIncludePathsToClrProperties<T>(
            parameters.Include
        );

        Logger.LogDebug(
            "Mapped {IncludeCount} include paths to CLR properties: {MappedIncludes}",
            mappedIncludes.Count,
            string.Join(", ", mappedIncludes)
        );

        // User-friendly warnings for include mapping issues
        if (parameters.Include?.Count > 0 && mappedIncludes.Count == 0)
        {
            Logger.LogWarning(
                "No valid include paths found for {EntityType}. Requested: {RequestedIncludes}. Check that these properties exist and are navigation properties",
                typeof(T).Name,
                string.Join(", ", parameters.Include)
            );
        }
        else if (parameters.Include?.Count > mappedIncludes.Count)
        {
            var unmapped = parameters.Include.Except(mappedIncludes.Select(m => m.Split('.')[0])).ToList();
            if (unmapped.Count > 0)
            {
                Logger.LogWarning(
                    "Some includes could not be mapped for {EntityType}: {UnmappedIncludes}. Check property names and navigation relationships",
                    typeof(T).Name,
                    string.Join(", ", unmapped)
                );
            }
        }

        // Separate include filters from main filters
        var (mainFilters, includeFilters) = IncludeFilterParser.SeparateIncludeFilters(
            parameters.Filter,
            parameters.Include
        );

        Logger.LogDebug(
            "Separated filters: MainFilters={MainFilterCount}, IncludeFilters={IncludeFilterCount}",
            mainFilters?.Filters?.Count ?? 0,
            includeFilters.Count
        );

        IQueryable<T> filteredQuery = queryable;

        // Apply main entity filters first
        if (mainFilters != null)
        {
            Logger.LogDebug(
                "Applying {FilterCount} main entity filters",
                mainFilters.Filters.Count
            );
            filteredQuery = filteredQuery.ApplyFilters(mainFilters, Logger);
        }

        // Standardized order: Filters -> Includes -> Sorting
        // This ensures consistent behavior regardless of include type

        // Apply includes (filtered or regular)
        if (includeFilters.Count > 0)
        {
            Logger.LogDebug(
                "Applying {FilteredIncludeCount} filtered includes",
                includeFilters.Count
            );
            filteredQuery = filteredQuery.ApplyFilteredIncludes(mappedIncludes, includeFilters);
        }
        else if (mappedIncludes.Count > 0)
        {
            Logger.LogDebug("Applying {IncludeCount} regular includes", mappedIncludes.Count);
            filteredQuery = filteredQuery.ApplyIncludes(mappedIncludes);
        }

        // Apply sorting after includes for consistency
        if (parameters.Sort?.Count > 0)
        {
            Logger.LogDebug("Applying {SortCount} sort parameters", parameters.Sort.Count);
            filteredQuery = filteredQuery.ApplySorting(parameters.Sort, Logger);
        }

        Logger.LogDebug("Executing count query to get total resource count");
        int totalCount = await filteredQuery.CountAsync().ConfigureAwait(false);
        Logger.LogDebug("Total count after filtering: {TotalCount}", totalCount);

        // User-friendly info about results
        if (totalCount == 0 && (parameters.Filter?.Filters?.Count > 0 || parameters.Include?.Count > 0))
        {
            Logger.LogInformation(
                "Query returned 0 results for {EntityType}. This might be due to filters or include conditions. Check your filter values and relationship data",
                typeof(T).Name
            );
        }
        else if (totalCount > 1000)
        {
            Logger.LogWarning(
                "Large result set detected ({TotalCount} records). Consider adding pagination or more specific filters for better performance",
                totalCount
            );
        }

        if (parameters.Pagination != null)
        {
            Logger.LogDebug(
                "Applying pagination: Page={PageNumber}, Size={PageSize}",
                parameters.Pagination.Number,
                parameters.Pagination.Size
            );
            filteredQuery = filteredQuery.ApplyPagination(parameters.Pagination);
        }

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

            Logger.LogDebug(
                "Created pagination metadata: TotalPages={TotalPages}, CurrentPage={CurrentPage}, PageSize={PageSize}",
                paginationMeta.TotalPages,
                paginationMeta.CurrentPage,
                paginationMeta.PageSize
            );
        }

        Logger.LogDebug("Executing final query to retrieve results");
        List<T> results = await filteredQuery.ToListAsync().ConfigureAwait(false);
        Logger.LogDebug("Retrieved {ResultCount} results from database", results.Count);

        Logger.LogDebug("Mapping results to JSON:API document structure");
        JsonApiCollectionDocument<ResourceObject> document = JsonApiMapper.ToCollectionDocument(
            results,
            resourceType,
            baseUrl,
            paginationMeta,
            mappedIncludes,
            Logger
        );

        Logger.LogDebug(
            "Successfully completed JSON:API query processing for resource type '{ResourceType}' with {ResourceCount} resources and {IncludedCount} included resources",
            resourceType,
            document.Data?.Count() ?? 0,
            document.Included?.Count() ?? 0
        );

        return Ok(document);
    }

    /// <summary>
    /// Creates a 201 Created response containing a newly created resource as a JSON:API document.
    /// </summary>
    /// <typeparam name="T">The entity type being returned</typeparam>
    /// <param name="entity">The newly created entity</param>
    /// <param name="resourceType">The JSON:API resource type identifier (typically the entity name in camelCase)</param>
    /// <param name="id">The ID of the newly created resource</param>
    /// <returns>An IActionResult with Status201Created and a properly formatted JSON:API document</returns>
    /// <remarks>
    /// Sets the Location header to the resource's URL and includes the resource in the response body.
    /// Serializes the provided entity into JSON:API format. Any relationships that are already loaded
    /// on the entity will be included in the response.
    /// </remarks>
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
    /// Creates a 204 No Content response for successful operations that don't return data.
    /// </summary>
    /// <returns>An IActionResult with Status204NoContent and an empty response body</returns>
    /// <remarks>
    /// Use this method for successful DELETE operations or updates that don't return the modified resource.
    /// </remarks>
    protected IActionResult JsonApiNoContent()
    {
        return NoContent();
    }

    /// <summary>
    /// Creates a 404 Not Found response with a JSON:API compliant error object.
    /// </summary>
    /// <param name="detail">Custom error message explaining what resource was not found</param>
    /// <returns>An IActionResult with Status404NotFound and a properly formatted JSON:API error document</returns>
    /// <remarks>
    /// Use this method when a requested resource doesn't exist to provide a consistent error response format.
    /// </remarks>
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
    /// Creates a 400 Bad Request response with a JSON:API compliant error object.
    /// </summary>
    /// <param name="detail">Specific error message explaining the validation or request problem</param>
    /// <returns>An IActionResult with Status400BadRequest and a properly formatted JSON:API error document</returns>
    /// <remarks>
    /// Use this method for validation errors, malformed requests, or other client errors.
    /// </remarks>
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
    /// Constructs the complete URL for the current request including scheme, host, path, and query string.
    /// </summary>
    /// <returns>The full URL of the current request as a string</returns>
    /// <remarks>
    /// Used internally to generate self links and pagination links in JSON:API responses.
    /// </remarks>
    protected string GetFullRequestUrl()
    {
        return $"{Request.Scheme}://{Request.Host}{Request.Path}{Request.QueryString}";
    }
}

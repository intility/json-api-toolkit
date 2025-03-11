using JsonApiToolkit.Extensions;
using JsonApiToolkit.Filters;
using JsonApiToolkit.Mapping;
using JsonApiToolkit.Models;
using JsonApiToolkit.Parsing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JsonApiToolkit.Controllers;

/// <summary>
/// Base class for JSON:API controllers. Provides helper methods for returning JSON:API documents.
/// </summary>
/// <remarks>
/// This class is intended to be used as a base class for JSON:API controllers.
/// </remarks>
[Produces("application/vnd.api+json")]
[Consumes("application/vnd.api+json")]
[ServiceFilter(typeof(JsonApiExceptionFilter))]
public abstract class JsonApiController : ControllerBase
{
    /// <summary>
    /// Parses the JSON:API query parameters from the current request.
    /// </summary>
    /// <returns>The parsed JSON:API query parameters.</returns>
    protected QueryParameters GetJsonApiQueryParameters()
    {
        return JsonApiQueryParser.Parse(Request);
    }

    /// <summary>
    /// Returns a 200 OK response with a JSON:API document. The document will include the entity
    /// and any included relationships specified in the query parameters.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="entity">The entity to return. </param>
    /// <param name="resourceType">The type of the resource object.</param>
    /// <returns>The 200 OK response.</returns>
    protected IActionResult JsonApiOk<T>(T entity, string resourceType)
        where T : class
    {
        string baseUrl = $"{Request.Scheme}://{Request.Host}{Request.Path}";
        QueryParameters queryParams = GetJsonApiQueryParameters();
        JsonApiDocument<ResourceObject> document = JsonApiMapper.ToDocument(
            entity,
            resourceType,
            baseUrl,
            queryParams.Include
        );
        return Ok(document);
    }

    /// <summary>
    /// Returns a 200 OK response for a collection of entities with a JSON:API document.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="entities">The entities to return.</param>
    /// <param name="resourceType">The type of the resource object.</param>
    /// <param name="paginationMeta">Optional pagination metadata.</param>
    /// <returns>The 200 OK response.</returns>
    protected IActionResult JsonApiOk<T>(
        IEnumerable<T> entities,
        string resourceType,
        PaginationMeta? paginationMeta = null
    )
        where T : class
    {
        string baseUrl = GetFullRequestUrl();
        QueryParameters queryParams = GetJsonApiQueryParameters();
        JsonApiCollectionDocument<ResourceObject> document = JsonApiMapper.ToCollectionDocument(
            entities,
            resourceType,
            baseUrl,
            paginationMeta,
            queryParams.Include
        );
        return Ok(document);
    }

    /// <summary>
    /// Returns a 200 OK response for a queryable collection with JSON:API query parameters applied.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="queryable">The queryable collection.</param>
    /// <param name="resourceType">The type of the resource object.</param>
    /// <returns>The 200 OK response.</returns>
    protected async Task<IActionResult> JsonApiOkAsync<T>(
        IQueryable<T> queryable,
        string resourceType
    )
        where T : class
    {
        QueryParameters parameters = GetJsonApiQueryParameters();
        string baseUrl = GetFullRequestUrl();

        IQueryable<T> filteredQuery = queryable;

        if (parameters.Filter != null)
            filteredQuery = filteredQuery.ApplyFilters(parameters.Filter);

        if (parameters.Sort?.Count > 0)
            filteredQuery = filteredQuery.ApplySorting(parameters.Sort);

        int totalCount = await filteredQuery.CountAsync();

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

        List<T> results = await filteredQuery.ToListAsync();

        JsonApiCollectionDocument<ResourceObject> document = JsonApiMapper.ToCollectionDocument(
            results,
            resourceType,
            baseUrl,
            paginationMeta,
            parameters.Include
        );

        return Ok(document);
    }

    /// <summary>
    /// Returns a 201 Created response with a JSON:API document.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="entity">The entity to return.</param>
    /// <param name="resourceType">The type of the resource object.</param>
    /// <param name="id">The ID of the entity.</param>
    /// <returns>The 201 Created response.</returns>
    protected IActionResult JsonApiCreated<T>(T entity, string resourceType, string id)
        where T : class
    {
        string baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}{Request.Path}";
        string selfUrl = $"{baseUrl}/{id}";
        QueryParameters queryParams = GetJsonApiQueryParameters();
        JsonApiDocument<ResourceObject> document = JsonApiMapper.ToDocument(
            entity,
            resourceType,
            selfUrl,
            queryParams.Include
        );
        return Created(selfUrl, document);
    }

    /// <summary>
    /// Returns a 204 No Content response with no body.
    /// </summary>
    /// <returns>The 204 No Content response.</returns>
    protected IActionResult JsonApiNoContent()
    {
        return NoContent();
    }

    /// <summary>
    /// Returns a 404 Not Found response with a JSON:API error object.
    /// </summary>
    /// <param name="detail">The error detail.</param>
    /// <returns>The 404 Not Found response.</returns>
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
    /// Returns a 400 Bad Request response with a JSON:API error object.
    /// </summary>
    /// <param name="detail">The error detail.</param>
    /// <returns>The 400 Bad Request response.</returns>
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
    /// Gets the full request URL including query string.
    /// </summary>
    /// <returns>The full request URL.</returns>
    protected string GetFullRequestUrl()
    {
        return $"{Request.Scheme}://{Request.Host}{Request.Path}{Request.QueryString}";
    }
}

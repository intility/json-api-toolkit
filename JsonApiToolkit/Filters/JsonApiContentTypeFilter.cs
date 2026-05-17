using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace JsonApiToolkit.Filters;

/// <summary>
/// Sets response content type to "application/vnd.api+json" for all JSON:API responses.
/// </summary>
public class JsonApiContentTypeFilter : IActionFilter
{
    private const string JsonApiMediaType = "application/vnd.api+json";

    /// <summary>
    /// Called before the action executes.
    /// </summary>
    public void OnActionExecuting(ActionExecutingContext context) { }

    /// <summary>
    /// Called after the action executes. Sets the content type to JSON:API media type.
    /// </summary>
    public void OnActionExecuted(ActionExecutedContext context)
    {
        if (context.Result is ObjectResult objectResult)
        {
            objectResult.ContentTypes.Clear();
            objectResult.ContentTypes.Add(JsonApiMediaType);
        }
        else if (context.Result is StatusCodeResult)
        {
            context.HttpContext.Response.ContentType = JsonApiMediaType;
        }
    }
}

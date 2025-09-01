using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace JsonApiToolkit.Filters;

/// <summary>
/// A filter that sets the content type of the response to "application/vnd.api+json"
/// for all JSON API responses.
/// </summary>
public class JsonApiContentTypeFilter : IActionFilter
{
    private const string s_jsonApiMediaType = "application/vnd.api+json";

    /// <summary>
    /// Does nothing before the action executes.
    /// </summary>
    public void OnActionExecuting(ActionExecutingContext context) { }

    /// <summary>
    /// Sets the content type of the response to "application/vnd.api+json"
    /// for all JSON API responses.
    /// </summary>
    public void OnActionExecuted(ActionExecutedContext context)
    {
        if (context.Result is ObjectResult objectResult)
        {
            objectResult.ContentTypes.Clear();
            objectResult.ContentTypes.Add(s_jsonApiMediaType);
        }
        else if (context.Result is StatusCodeResult)
        {
            context.HttpContext.Response.ContentType = s_jsonApiMediaType;
        }
    }
}

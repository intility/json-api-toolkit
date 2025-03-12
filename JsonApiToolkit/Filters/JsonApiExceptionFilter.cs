using JsonApiToolkit.Models.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JsonApiToolkit.Filters;

/// <summary>
/// Exception filter that transforms unhandled exceptions into JSON:API compliant error responses.
/// </summary>
/// <remarks>
/// <para>
/// This filter ensures that all unhandled exceptions in JSON:API controllers result in properly formatted
/// JSON:API error responses rather than the default ASP.NET Core error format.
/// </para>
/// <para>
/// In development environments, the filter includes detailed exception information in the response.
/// In production environments, it provides a generic error message to avoid exposing sensitive details.
/// </para>
/// <para>
/// The filter automatically logs all exceptions using the provided ILogger instance.
/// </para>
/// </remarks>
/// <param name="logger">Logger for recording exception details</param>
/// <param name="environment">Host environment to determine the level of error detail</param>
public class JsonApiExceptionFilter(
    ILogger<JsonApiExceptionFilter> logger,
    IHostEnvironment environment
) : IExceptionFilter
{
    private readonly ILogger<JsonApiExceptionFilter> _logger = logger;
    private readonly IHostEnvironment _environment = environment;

    /// <summary>
    /// Transforms an unhandled exception into a standardized JSON:API error response.
    /// </summary>
    /// <param name="context">The exception context containing the exception and controller context</param>
    /// <remarks>
    /// This method:
    /// <list type="number">
    /// <item>
    /// <description>Logs the exception using the configured logger</description>
    /// </item>
    /// <item>
    /// <description>Creates a JSON:API error object with a 500 status code</description>
    /// </item>
    /// <item>
    /// <description>Sets appropriate error detail based on the environment (detailed in development, generic in production)</description>
    /// </item>
    /// <item>
    /// <description>Returns the error response and marks the exception as handled</description>
    /// </item>
    /// </list>
    /// <para>
    /// The resulting error response follows the JSON:API specification for error objects.
    /// </para>
    /// </remarks>
    public void OnException(ExceptionContext context)
    {
        _logger.LogError(context.Exception, "An unhandled exception occurred");

        var error = new JsonApiError
        {
            Status = "500",
            Title = "Internal Server Error",
            Detail = _environment.IsDevelopment()
                ? context.Exception.Message
                : "An error occurred while processing your request.",
        };

        var response = new JsonApiErrorResponse { Errors = [error] };

        context.Result = new ObjectResult(response) { StatusCode = 500 };
        context.ExceptionHandled = true;
    }
}

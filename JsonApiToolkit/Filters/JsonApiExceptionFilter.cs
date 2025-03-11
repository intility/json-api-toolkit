using JsonApiToolkit.Models.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JsonApiToolkit.Filters;

/// <summary>
/// A filter that intercepts unhandled exceptions, logs the error, and constructs a JSON:API compliant error response.
/// </summary>
/// <remarks>
/// The filter logs the exception using the provided logger. It then creates a standardized error response
/// with a status code of "500", tailoring the error detail based on the current hosting environment:
/// returning the exception message in a development setting, or a generic error message otherwise.
/// </remarks>
/// <param name="logger">
/// The logger used to record the exception details.
/// </param>
/// <param name="environment">
/// The current hosting environment used to determine the level of detail included in the error response.
/// </param>
/// <seealso cref="IExceptionFilter"/>
public class JsonApiExceptionFilter(
    ILogger<JsonApiExceptionFilter> logger,
    IHostEnvironment environment
) : IExceptionFilter
{
    private readonly ILogger<JsonApiExceptionFilter> _logger = logger;
    private readonly IHostEnvironment _environment = environment;

    /// <summary>
    /// Handles unhandled exceptions that occur during the execution of a request.
    /// </summary>
    /// <param name="context">
    /// The context for the exception, which provides access to the exception object and allows
    /// setting the result to a structured JSON API error response.
    /// </param>
    /// <remarks>
    /// This method logs the exception, constructs a JSON API error response with a 500 status code,
    /// and provides additional error detail when running in a development environment.
    /// It then marks the exception as handled to prevent further propagation.
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

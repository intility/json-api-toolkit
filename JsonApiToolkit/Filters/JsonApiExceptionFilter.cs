using JsonApiToolkit.Models.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace JsonApiToolkit.Filters;

/// <summary>
/// Exception filter that transforms known and unknown exceptions into JSON:API compliant error responses.
/// </summary>
public class JsonApiExceptionFilter(ILogger<JsonApiExceptionFilter> logger) : IExceptionFilter
{
    private readonly ILogger<JsonApiExceptionFilter> _logger = logger;

    /// <summary>
    /// Handles exceptions thrown during the execution of a controller action.
    /// </summary>
    /// <param name="context">The context of the exception.</param>
    /// <remarks>
    /// <para>
    /// This method inspects the exception and determines the appropriate HTTP status code
    /// and error message to return in the JSON:API error response.
    /// </para>
    /// <para>
    /// It handles known exceptions (e.g., JsonApiBadRequestException, JsonApiNotFoundException)
    /// and logs unexpected exceptions (500 Internal Server Error).
    /// </para>
    /// </remarks>
    public void OnException(ExceptionContext context)
    {
        var (status, title) = context.Exception switch
        {
            JsonApiBadRequestException => (400, "Bad Request"),
            JsonApiNotFoundException => (404, "Not Found"),
            JsonApiConflictException => (409, "Conflict"),
            JsonApiUnauthorizedException => (401, "Unauthorized"),
            JsonApiForbiddenException => (403, "Forbidden"),
            _ => (500, "Internal Server Error"),
        };

        if (status == 500)
        {
            // Log full stack trace for unexpected errors
            _logger.LogError(context.Exception, "An unhandled exception occurred");
        }
        else
        {
            // Log only the message for handled exceptions
            _logger.LogInformation(
                "Handled JSON:API exception: {Type} - {Message}",
                context.Exception.GetType().Name,
                context.Exception.Message
            );
        }

        var error = new JsonApiError
        {
            Status = status.ToString(),
            Title = title,
            Detail =
                status != 500
                    ? context.Exception.Message
                    : "An error occurred while processing your request.",
        };

        var response = new JsonApiErrorResponse { Errors = [error] };

        context.Result = new ObjectResult(response) { StatusCode = status };
        context.ExceptionHandled = true;
    }
}

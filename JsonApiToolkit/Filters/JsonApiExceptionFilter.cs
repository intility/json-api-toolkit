using JsonApiToolkit.Models.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace JsonApiToolkit.Filters;

/// <summary>
/// Transforms exceptions into JSON:API compliant error responses.
/// Handles known JsonApiException types and logs unexpected errors.
/// </summary>
public class JsonApiExceptionFilter(ILogger<JsonApiExceptionFilter> logger) : IExceptionFilter
{
    private readonly ILogger<JsonApiExceptionFilter> _logger = logger;

    /// <summary>
    /// Handles exceptions and converts them to JSON:API error responses.
    /// </summary>
    public void OnException(ExceptionContext context)
    {
        int status;
        string title;
        JsonApiError error;

        if (context.Exception is JsonApiException jsonApiException)
        {
            // Handle structured JSON:API exceptions
            status = jsonApiException.StatusCode;
            title = GetTitleForStatusCode(status);

            error = new JsonApiError
            {
                Status = status.ToString(),
                Title = title,
                Detail = jsonApiException.Message,
                Code = jsonApiException.Code,
                Source = jsonApiException.ErrorSource,
                Meta = jsonApiException.Meta,
            };

            // Log handled exceptions
            _logger.LogInformation(
                "Handled JSON:API exception: {Type} - {Message}",
                jsonApiException.GetType().Name,
                jsonApiException.Message
            );
        }
        else
        {
            // Handle unexpected exceptions
            status = 500;
            title = "Internal Server Error";

            error = new JsonApiError
            {
                Status = status.ToString(),
                Title = title,
                Detail = "An error occurred while processing your request.",
            };

            // Log full stack trace for unexpected errors
            _logger.LogError(context.Exception, "An unhandled exception occurred");
        }

        var response = new JsonApiErrorResponse { Errors = [error] };

        context.Result = new ObjectResult(response) { StatusCode = status };
        context.ExceptionHandled = true;
    }

    private static string GetTitleForStatusCode(int statusCode) =>
        statusCode switch
        {
            400 => "Bad Request",
            401 => "Unauthorized",
            403 => "Forbidden",
            404 => "Not Found",
            409 => "Conflict",
            429 => "Too Many Requests",
            _ => "Error",
        };
}

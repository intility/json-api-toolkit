using JsonApiToolkit.Models.Errors;
using JsonApiToolkit.Models.Validation;
using JsonApiToolkit.Parsing;
using JsonApiToolkit.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace JsonApiToolkit.Attributes;

/// <summary>
/// Restricts which relationships can be included in responses.
/// Returns 403 Forbidden if requested includes don't match the allowlist.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class AllowedIncludesAttribute : ActionFilterAttribute
{
    private readonly string[] _allowedIncludes;
    private readonly Dictionary<string, IncludePattern> _compiledPatterns;

    /// <summary>
    /// Gets the allowed include patterns.
    /// </summary>
    public string[] AllowedIncludes => _allowedIncludes;

    /// <summary>
    /// Initializes a new instance with the specified allowed include patterns.
    /// </summary>
    /// <param name="allowedIncludes">Include patterns to allow (supports wildcards).</param>
    public AllowedIncludesAttribute(params string[] allowedIncludes)
    {
        _allowedIncludes = allowedIncludes ?? [];
        _compiledPatterns = new Dictionary<string, IncludePattern>(
            StringComparer.OrdinalIgnoreCase
        );

        foreach (var pattern in _allowedIncludes)
        {
            _compiledPatterns[pattern] = new IncludePattern(pattern);
        }
    }

    /// <summary>
    /// Validates requested includes and filter paths against the allowed patterns.
    /// </summary>
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        // Skip validation for JsonApiCreated methods
        var actionName = context.ActionDescriptor.DisplayName;
        if (
            actionName != null
            && actionName.Contains("JsonApiCreated", StringComparison.OrdinalIgnoreCase)
        )
        {
            base.OnActionExecuting(context);
            return;
        }

        var request = context.HttpContext.Request;
        var logger =
            context.HttpContext.RequestServices.GetService(
                typeof(ILogger<AllowedIncludesAttribute>)
            ) as ILogger<AllowedIncludesAttribute>;
        var queryParams = JsonApiQueryParser.Parse(request, logger);

        // Validate includes
        if (queryParams.Include != null && queryParams.Include.Count > 0)
        {
            // Empty array means no includes allowed
            if (_allowedIncludes.Length == 0)
            {
                ThrowForbiddenIncludeException(
                    queryParams.Include,
                    queryParams.Include,
                    _allowedIncludes,
                    context,
                    logger
                );
                return;
            }

            var validationResult = IncludeValidator.ValidateIncludes(
                queryParams.Include,
                _allowedIncludes
            );

            if (!validationResult.IsValid)
            {
                ThrowForbiddenIncludeException(
                    queryParams.Include,
                    validationResult.ForbiddenIncludes,
                    _allowedIncludes,
                    context,
                    logger
                );
                return;
            }
        }

        // Validate filter paths against allowed includes
        // This prevents filtering on relationships not in the allowed list
        if (queryParams.Filter != null && _allowedIncludes.Length > 0)
        {
            var filterValidation = IncludeValidator.ValidateFilterPaths(
                queryParams.Filter,
                _allowedIncludes
            );

            if (!filterValidation.IsValid)
            {
                ThrowForbiddenFilterException(
                    filterValidation.ForbiddenFilterPaths,
                    _allowedIncludes,
                    context,
                    logger
                );
                return;
            }
        }

        base.OnActionExecuting(context);
    }

    private static void ThrowForbiddenIncludeException(
        List<string> requestedIncludes,
        List<string> forbiddenIncludes,
        string[] allowedIncludes,
        ActionExecutingContext context,
        ILogger? logger
    )
    {
        if (logger != null && forbiddenIncludes.Count > 0)
        {
            logger.LogWarning(
                "Include access denied for request {RequestId}",
                context.HttpContext.TraceIdentifier
            );
        }

        var errorDetail =
            forbiddenIncludes.Count == 1
                ? $"The requested include '{forbiddenIncludes[0]}' was not found"
                : $"The requested includes '{string.Join(", ", forbiddenIncludes)}' were not found";

        var error = new JsonApiError
        {
            Status = "403",
            Code = JsonApiErrorCodes.IncludeNotAllowed,
            Title = "Forbidden Include",
            Detail = errorDetail,
            Source = new ErrorSource { Parameter = "include" },
            Meta = new Dictionary<string, object>
            {
                ["requestedIncludes"] = requestedIncludes,
                ["forbiddenIncludes"] = forbiddenIncludes,
                ["allowedIncludes"] =
                    allowedIncludes.Length > 0 ? allowedIncludes : Array.Empty<string>(),
            },
        };

        var errorResponse = new JsonApiErrorResponse { Errors = [error] };

        context.Result = new ObjectResult(errorResponse)
        {
            StatusCode = 403,
            ContentTypes = { "application/vnd.api+json" },
        };
    }

    private static void ThrowForbiddenFilterException(
        List<string> forbiddenFilterPaths,
        string[] allowedIncludes,
        ActionExecutingContext context,
        ILogger? logger
    )
    {
        if (logger != null && forbiddenFilterPaths.Count > 0)
        {
            logger.LogWarning(
                "Filter path access denied for request {RequestId}",
                context.HttpContext.TraceIdentifier
            );
        }

        var errorDetail =
            forbiddenFilterPaths.Count == 1
                ? $"Filtering on relationship '{forbiddenFilterPaths[0]}' is not allowed"
                : $"Filtering on relationships '{string.Join(", ", forbiddenFilterPaths)}' is not allowed";

        var error = new JsonApiError
        {
            Status = "403",
            Code = JsonApiErrorCodes.FilterNotAllowed,
            Title = "Forbidden Filter",
            Detail = errorDetail,
            Source = new ErrorSource { Parameter = "filter" },
            Meta = new Dictionary<string, object>
            {
                ["forbiddenFilterPaths"] = forbiddenFilterPaths,
                ["allowedIncludes"] =
                    allowedIncludes.Length > 0 ? allowedIncludes : Array.Empty<string>(),
            },
        };

        var errorResponse = new JsonApiErrorResponse { Errors = [error] };

        context.Result = new ObjectResult(errorResponse)
        {
            StatusCode = 403,
            ContentTypes = { "application/vnd.api+json" },
        };
    }
}

using JsonApiToolkit.Models.Errors;
using JsonApiToolkit.Models.Validation;
using JsonApiToolkit.Parsing;
using JsonApiToolkit.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace JsonApiToolkit.Attributes;

/// <summary>
/// Action filter attribute that restricts which relationships can be included in JSON:API responses.
/// </summary>
/// <remarks>
/// This attribute validates the 'include' query parameter against a whitelist of allowed includes.
/// If a client requests an include that is not in the whitelist, a 403 Forbidden error is returned.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class AllowedIncludesAttribute : ActionFilterAttribute
{
    private readonly string[] _allowedIncludes;
    private readonly Dictionary<string, IncludePattern> _compiledPatterns;

    /// <summary>
    /// Gets the list of allowed include patterns.
    /// </summary>
    public string[] AllowedIncludes => _allowedIncludes;

    /// <summary>
    /// Initializes a new instance of the <see cref="AllowedIncludesAttribute"/> class.
    /// </summary>
    /// <param name="allowedIncludes">The list of allowed include patterns. If empty, no includes are allowed.</param>
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
    /// Validates the include query parameters before the action executes.
    /// </summary>
    /// <param name="context">The action executing context.</param>
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
        var queryParams = JsonApiQueryParser.Parse(request);

        if (queryParams.Include == null || queryParams.Include.Count == 0)
        {
            base.OnActionExecuting(context);
            return;
        }

        // Empty array means no includes allowed
        if (_allowedIncludes.Length == 0)
        {
            ThrowForbiddenException(
                queryParams.Include,
                queryParams.Include,
                _allowedIncludes,
                context
            );
            return;
        }

        var validationResult = IncludeValidator.ValidateIncludes(
            queryParams.Include,
            _allowedIncludes
        );

        if (!validationResult.IsValid)
        {
            ThrowForbiddenException(
                queryParams.Include,
                validationResult.ForbiddenIncludes,
                _allowedIncludes,
                context
            );
        }

        base.OnActionExecuting(context);
    }

    private void ThrowForbiddenException(
        List<string> requestedIncludes,
        List<string> forbiddenIncludes,
        string[] allowedIncludes,
        ActionExecutingContext context
    )
    {
        var logger =
            context.HttpContext.RequestServices.GetService(
                typeof(ILogger<AllowedIncludesAttribute>)
            ) as ILogger<AllowedIncludesAttribute>;

        if (logger != null && forbiddenIncludes.Count > 0)
        {
            logger.LogWarning(
                "Forbidden includes requested: {ForbiddenIncludes}. Allowed includes: {AllowedIncludes}",
                string.Join(", ", forbiddenIncludes),
                string.Join(", ", allowedIncludes)
            );
        }

        var errorDetail =
            forbiddenIncludes.Count == 1
                ? $"The requested include '{forbiddenIncludes[0]}' was not found"
                : $"The requested includes '{string.Join(", ", forbiddenIncludes)}' were not found";

        var error = new JsonApiError
        {
            Status = "403",
            Title = "Forbidden Include",
            Detail = errorDetail,
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
}

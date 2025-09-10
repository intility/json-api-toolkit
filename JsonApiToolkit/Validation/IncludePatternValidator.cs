using System.Reflection;
using JsonApiToolkit.Attributes;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.Logging;

namespace JsonApiToolkit.Validation;

/// <summary>
/// Provides startup validation for include patterns in AllowedIncludesAttribute.
/// </summary>
public class IncludePatternValidator : IApplicationModelProvider
{
    private readonly ILogger<IncludePatternValidator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="IncludePatternValidator"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public IncludePatternValidator(ILogger<IncludePatternValidator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets the order in which providers are executed.
    /// </summary>
    public int Order => -1000;

    /// <summary>
    /// Validates include patterns during application startup.
    /// </summary>
    /// <param name="context">The application model provider context.</param>
    public void OnProvidersExecuting(ApplicationModelProviderContext context)
    {
        foreach (var controller in context.Result.Controllers)
        {
            foreach (var action in controller.Actions)
            {
                var allowedIncludesAttribute = action
                    .Attributes.OfType<AllowedIncludesAttribute>()
                    .FirstOrDefault();

                if (allowedIncludesAttribute != null)
                {
                    ValidatePatterns(
                        allowedIncludesAttribute.AllowedIncludes,
                        controller.ControllerName,
                        action.ActionName
                    );
                }
            }
        }
    }

    /// <summary>
    /// Called after providers have executed.
    /// </summary>
    /// <param name="context">The application model provider context.</param>
    public void OnProvidersExecuted(ApplicationModelProviderContext context)
    {
        // No action needed
    }

    private void ValidatePatterns(string[] patterns, string controllerName, string actionName)
    {
        var warnings = new List<string>();

        foreach (var pattern in patterns)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                warnings.Add($"Empty or whitespace pattern found");
                continue;
            }

            // Check for potentially problematic patterns
            if (pattern.Contains("**"))
            {
                warnings.Add(
                    $"Pattern '{pattern}' contains '**' which is not supported. Use single '*' for wildcards."
                );
            }

            if (pattern.StartsWith(".") || pattern.EndsWith("."))
            {
                warnings.Add(
                    $"Pattern '{pattern}' starts or ends with '.', which may not work as expected."
                );
            }

            if (pattern.Count(c => c == '*') > 1 && !pattern.Contains(".*"))
            {
                warnings.Add(
                    $"Pattern '{pattern}' contains multiple wildcards. Only '.*' wildcard pattern is fully supported."
                );
            }

            // Check for regex special characters that might cause issues
            var specialChars = new[]
            {
                '[',
                ']',
                '(',
                ')',
                '{',
                '}',
                '\\',
                '^',
                '$',
                '|',
                '?',
                '+',
            };
            if (specialChars.Any(c => pattern.Contains(c)))
            {
                warnings.Add(
                    $"Pattern '{pattern}' contains special regex characters that may not work as expected."
                );
            }
        }

        if (warnings.Count > 0)
        {
            _logger.LogWarning(
                "AllowedIncludesAttribute validation warnings for {Controller}.{Action}: {Warnings}",
                controllerName,
                actionName,
                string.Join("; ", warnings)
            );
        }
    }
}

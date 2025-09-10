using JsonApiToolkit.Models.Validation;

namespace JsonApiToolkit.Validation;

/// <summary>
/// Provides validation logic for JSON:API include parameters.
/// </summary>
public static class IncludeValidator
{
    /// <summary>
    /// Validates requested includes against allowed patterns.
    /// </summary>
    /// <param name="requestedIncludes">The includes requested by the client.</param>
    /// <param name="allowedPatterns">The allowed include patterns.</param>
    /// <returns>A validation result containing any forbidden includes.</returns>
    public static ValidationResult ValidateIncludes(
        IEnumerable<string> requestedIncludes,
        IEnumerable<string> allowedPatterns
    )
    {
        var patterns = allowedPatterns.Select(p => new IncludePattern(p)).ToList();
        var forbidden = new List<string>();

        foreach (var requested in requestedIncludes)
        {
            if (!IsIncludeAllowed(requested, patterns))
            {
                forbidden.Add(requested);
            }
        }

        return new ValidationResult
        {
            IsValid = forbidden.Count == 0,
            ForbiddenIncludes = forbidden,
        };
    }

    /// <summary>
    /// Checks if a specific include is allowed by any of the patterns.
    /// </summary>
    /// <param name="include">The include to check.</param>
    /// <param name="patterns">The allowed patterns.</param>
    /// <returns>True if the include is allowed, false otherwise.</returns>
    public static bool IsIncludeAllowed(string include, IEnumerable<IncludePattern> patterns)
    {
        return patterns.Any(pattern => pattern.Matches(include));
    }

    /// <summary>
    /// Compiles pattern strings into IncludePattern objects for efficient matching.
    /// </summary>
    /// <param name="patternStrings">The pattern strings to compile.</param>
    /// <returns>A collection of compiled patterns.</returns>
    public static IEnumerable<IncludePattern> CompilePatterns(IEnumerable<string> patternStrings)
    {
        return patternStrings.Select(p => new IncludePattern(p));
    }
}

/// <summary>
/// Represents the result of include validation.
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Gets or sets whether all requested includes are valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the list of forbidden includes.
    /// </summary>
    public List<string> ForbiddenIncludes { get; set; } = new();
}

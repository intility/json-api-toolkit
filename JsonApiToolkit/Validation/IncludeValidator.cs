using JsonApiToolkit.Models.Validation;

namespace JsonApiToolkit.Validation;

/// <summary>
/// Validates include parameters against allowed patterns.
/// </summary>
public static class IncludeValidator
{
    /// <summary>
    /// Validates requested includes against allowed patterns.
    /// </summary>
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
    /// Checks if an include path matches any of the allowed patterns.
    /// </summary>
    public static bool IsIncludeAllowed(string include, IEnumerable<IncludePattern> patterns)
    {
        return patterns.Any(pattern => pattern.Matches(include));
    }

    /// <summary>
    /// Compiles pattern strings into IncludePattern objects.
    /// </summary>
    public static IEnumerable<IncludePattern> CompilePatterns(IEnumerable<string> patternStrings)
    {
        return patternStrings.Select(p => new IncludePattern(p));
    }
}

/// <summary>
/// Result of include validation.
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Whether all includes are valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// List of forbidden includes.
    /// </summary>
    public List<string> ForbiddenIncludes { get; set; } = new();
}

using JsonApiToolkit.Models.Querying.Filtering;
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
    /// Validates filter paths against allowed patterns.
    /// Filters with dot-notation (e.g., filter[admin.password]) target relationships
    /// and must be validated against allowed includes.
    /// </summary>
    public static FilterPathValidationResult ValidateFilterPaths(
        FilterGroup? filterGroup,
        IEnumerable<string> allowedPatterns
    )
    {
        if (filterGroup == null)
            return new FilterPathValidationResult { IsValid = true };

        var patterns = allowedPatterns.Select(p => new IncludePattern(p)).ToList();
        var forbidden = new List<string>();

        CollectForbiddenFilterPaths(filterGroup, patterns, forbidden);

        return new FilterPathValidationResult
        {
            IsValid = forbidden.Count == 0,
            ForbiddenFilterPaths = forbidden,
        };
    }

    private static void CollectForbiddenFilterPaths(
        FilterGroup group,
        List<IncludePattern> patterns,
        List<string> forbidden
    )
    {
        foreach (var filter in group.Filters)
        {
            var relationshipPath = ExtractRelationshipPath(filter.Field);
            if (relationshipPath != null && !IsIncludeAllowed(relationshipPath, patterns))
            {
                // Only add if not already in the list
                if (!forbidden.Contains(relationshipPath, StringComparer.OrdinalIgnoreCase))
                {
                    forbidden.Add(relationshipPath);
                }
            }
        }

        foreach (var nestedGroup in group.Groups)
        {
            CollectForbiddenFilterPaths(nestedGroup, patterns, forbidden);
        }
    }

    /// <summary>
    /// Extracts the relationship path from a filter field.
    /// e.g., "admin.password" → "admin", "author.posts.title" → "author.posts"
    /// Returns null if the field doesn't contain a dot (no relationship path).
    /// </summary>
    private static string? ExtractRelationshipPath(string field)
    {
        if (string.IsNullOrEmpty(field) || !field.Contains('.'))
            return null;

        var lastDotIndex = field.LastIndexOf('.');
        return field[..lastDotIndex];
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

/// <summary>
/// Result of filter path validation.
/// </summary>
public class FilterPathValidationResult
{
    /// <summary>
    /// Whether all filter paths are valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// List of forbidden filter relationship paths.
    /// </summary>
    public List<string> ForbiddenFilterPaths { get; set; } = new();
}

using System.Text.RegularExpressions;

namespace JsonApiToolkit.Models.Validation;

/// <summary>
/// Represents a compiled include pattern for efficient matching.
/// </summary>
public class IncludePattern
{
    /// <summary>
    /// Gets the original pattern string.
    /// </summary>
    public string OriginalPattern { get; }

    /// <summary>
    /// Gets whether this pattern contains wildcards.
    /// </summary>
    public bool IsWildcard { get; }

    /// <summary>
    /// Gets the type of pattern.
    /// </summary>
    public PatternType Type { get; }

    /// <summary>
    /// Gets the compiled regex for wildcard patterns.
    /// </summary>
    public Regex? CompiledRegex { get; }

    /// <summary>
    /// Gets the pattern parts for non-wildcard patterns.
    /// </summary>
    public string[]? PatternParts { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="IncludePattern"/> class.
    /// </summary>
    /// <param name="pattern">The include pattern string.</param>
    public IncludePattern(string pattern)
    {
        OriginalPattern = pattern ?? throw new ArgumentNullException(nameof(pattern));

        if (pattern.Contains('*'))
        {
            IsWildcard = true;
            Type = DetermineWildcardType(pattern);
            CompiledRegex = CompileWildcardPattern(pattern);
        }
        else
        {
            IsWildcard = false;
            Type = PatternType.Exact;
            PatternParts = pattern.Split('.');
        }
    }

    private PatternType DetermineWildcardType(string pattern)
    {
        if (pattern == "*")
            return PatternType.TopLevelWildcard;

        if (pattern.EndsWith(".*"))
            return PatternType.SingleLevelWildcard;

        return PatternType.ComplexWildcard;
    }

    private Regex CompileWildcardPattern(string pattern)
    {
        // Escape special regex characters except *
        var escapedPattern = Regex.Escape(pattern).Replace("\\*", ".*");

        if (pattern == "*")
        {
            // Top-level wildcard: match only single segments (no dots)
            return new Regex("^[^.]+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        if (pattern.EndsWith(".*"))
        {
            // Single-level wildcard: "author.*" matches "author.posts" but not "author.posts.comments"
            var prefix = pattern[..^2]; // Remove ".*"
            var escapedPrefix = Regex.Escape(prefix);
            // Match prefix.something but not prefix.something.else
            return new Regex(
                $"^{escapedPrefix}\\.[^.]+$",
                RegexOptions.IgnoreCase | RegexOptions.Compiled
            );
        }

        // Complex wildcard (future use)
        return new Regex($"^{escapedPattern}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    /// <summary>
    /// Checks if the given include matches this pattern.
    /// </summary>
    /// <param name="include">The include to check.</param>
    /// <returns>True if the include matches, false otherwise.</returns>
    public bool Matches(string include)
    {
        if (string.IsNullOrEmpty(include))
            return false;

        if (IsWildcard && CompiledRegex != null)
        {
            // For wildcard patterns, also check if it's a partial path
            if (Type == PatternType.SingleLevelWildcard)
            {
                var prefix = OriginalPattern[..^2]; // Remove ".*"

                // Exact match with prefix (e.g., "author" matches "author.*")
                if (string.Equals(include, prefix, StringComparison.OrdinalIgnoreCase))
                    return true;

                // Full wildcard match (e.g., "author.posts" matches "author.*")
                return CompiledRegex.IsMatch(include);
            }

            return CompiledRegex.IsMatch(include);
        }

        // Non-wildcard exact match (case-insensitive)
        if (string.Equals(include, OriginalPattern, StringComparison.OrdinalIgnoreCase))
            return true;

        // Partial path matching for non-wildcard patterns
        if (PatternParts != null && PatternParts.Length > 0)
        {
            var includeParts = include.Split('.');

            // Check if include is a prefix of the pattern
            if (includeParts.Length < PatternParts.Length)
            {
                for (int i = 0; i < includeParts.Length; i++)
                {
                    if (
                        !string.Equals(
                            includeParts[i],
                            PatternParts[i],
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                        return false;
                }
                return true;
            }
        }

        return false;
    }
}

/// <summary>
/// Specifies the type of include pattern.
/// </summary>
public enum PatternType
{
    /// <summary>
    /// Exact string match pattern.
    /// </summary>
    Exact,

    /// <summary>
    /// Top-level wildcard (*) that matches any single segment.
    /// </summary>
    TopLevelWildcard,

    /// <summary>
    /// Single-level wildcard (e.g., author.*) that matches one level deep.
    /// </summary>
    SingleLevelWildcard,

    /// <summary>
    /// Complex wildcard pattern (reserved for future use).
    /// </summary>
    ComplexWildcard,
}

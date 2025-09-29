using System.Text.RegularExpressions;

namespace JsonApiToolkit.Models.Validation;

/// <summary>
/// Compiled include pattern for efficient matching (supports wildcards).
/// </summary>
public class IncludePattern
{
    /// <summary>
    /// The original pattern string.
    /// </summary>
    public string OriginalPattern { get; }

    /// <summary>
    /// Whether the pattern contains wildcards.
    /// </summary>
    public bool IsWildcard { get; }

    /// <summary>
    /// Type of pattern (exact, wildcard, etc.).
    /// </summary>
    public PatternType Type { get; }

    /// <summary>
    /// Compiled regex for wildcard matching.
    /// </summary>
    public Regex? CompiledRegex { get; }

    /// <summary>
    /// Pattern split into parts for exact matching.
    /// </summary>
    public string[]? PatternParts { get; }

    /// <summary>
    /// Initializes a new include pattern.
    /// </summary>
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
    /// Checks if an include path matches this pattern.
    /// </summary>
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
/// Type of include pattern.
/// </summary>
public enum PatternType
{
    /// <summary>Exact match with no wildcards.</summary>
    Exact,
    /// <summary>Top-level wildcard (*).</summary>
    TopLevelWildcard,
    /// <summary>Single-level wildcard (e.g., author.*).</summary>
    SingleLevelWildcard,
    /// <summary>Complex wildcard pattern.</summary>
    ComplexWildcard,
}

using System.Text.RegularExpressions;

namespace JsonApiToolkit.Extensions.Querying;

internal static partial class FilterLogSanitizer
{
    private const int MaxLogValueLength = 100;

    /// <summary>
    /// Sanitizes user input for safe logging by removing control characters
    /// and truncating long values to prevent log forging attacks.
    /// </summary>
    internal static string SanitizeForLog(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "(empty)";

        // Remove control characters (newlines, tabs, etc.) that could forge log entries
        string sanitized = ControlCharRegex().Replace(value, " ");

        // Truncate long values
        if (sanitized.Length > MaxLogValueLength)
            return string.Concat(sanitized.AsSpan(0, MaxLogValueLength), "...(truncated)");

        return sanitized;
    }

    [GeneratedRegex(@"[\x00-\x1F\x7F]")]
    private static partial Regex ControlCharRegex();
}

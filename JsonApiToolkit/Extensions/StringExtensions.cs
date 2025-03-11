namespace JsonApiToolkit.Extensions;

/// <summary>
/// Extension methods for different string operations.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Converts a string to camel case.
    /// </summary>
    /// <param name="str">The string to convert.</param>
    /// <returns>The string in camel case.</returns>
    public static string ToCamelCase(this string str)
    {
        if (string.IsNullOrEmpty(str) || !char.IsUpper(str[0]))
            return str;

        return char.ToLowerInvariant(str[0]) + str.Substring(1);
    }

    /// <summary>
    /// Converts a string to pascal case.
    /// </summary>
    /// <param name="str">The string to convert.</param>
    /// <returns>The string in pascal case.</returns>
    public static string ToPascalCase(string str)
    {
        if (string.IsNullOrEmpty(str))
            return str;

        return char.ToUpperInvariant(str[0]) + str.Substring(1);
    }
}

namespace JsonApiToolkit.Extensions;

/// <summary>
/// Extension methods for strings.
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
}

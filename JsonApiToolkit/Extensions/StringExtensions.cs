namespace JsonApiToolkit.Extensions;

/// <summary>
/// Provides extension methods for string manipulation related to JSON:API implementation.
/// </summary>
/// <remarks>
/// Contains utilities for case conversion between JSON and C# naming conventions.
/// </remarks>
public static class StringExtensions
{
    /// <summary>
    /// Converts a string from PascalCase to camelCase format.
    /// </summary>
    /// <param name="str">The string to convert</param>
    /// <returns>The camelCase version of the input string</returns>
    /// <remarks>
    /// Used for converting C# property names (PascalCase) to JSON property names (camelCase).
    /// Returns the original string if it's null, empty, or already in camelCase format.
    /// </remarks>
    public static string ToCamelCase(this string str)
    {
        if (string.IsNullOrEmpty(str) || !char.IsUpper(str[0]))
            return str;

        return char.ToLowerInvariant(str[0]) + str.Substring(1);
    }

    /// <summary>
    /// Converts a string from camelCase to PascalCase format.
    /// </summary>
    /// <param name="str">The string to convert</param>
    /// <returns>The PascalCase version of the input string</returns>
    /// <remarks>
    /// Used for converting JSON property names (camelCase) to C# property names (PascalCase).
    /// Returns the original string if it's null or empty.
    /// </remarks>
    public static string ToPascalCase(this string str)
    {
        if (string.IsNullOrEmpty(str))
            return str;

        return char.ToUpperInvariant(str[0]) + str.Substring(1);
    }
}

namespace JsonApiToolkit.Extensions;

/// <summary>
/// String extension methods for case conversion (PascalCase ↔ camelCase).
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Converts PascalCase to camelCase.
    /// </summary>
    public static string ToCamelCase(this string str)
    {
        if (string.IsNullOrEmpty(str) || !char.IsUpper(str[0]))
            return str;

        return char.ToLowerInvariant(str[0]) + str.Substring(1);
    }

    /// <summary>
    /// Converts camelCase to PascalCase.
    /// </summary>
    public static string ToPascalCase(this string str)
    {
        if (string.IsNullOrEmpty(str))
            return str;

        return char.ToUpperInvariant(str[0]) + str.Substring(1);
    }
}

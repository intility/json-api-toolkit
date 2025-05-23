using System.Globalization;
using System.Reflection;

namespace JsonApiToolkit.Extensions.Querying;

/// <summary>
/// Provides helper methods for interpreting and converting query parameters in JSON:API requests.
/// </summary>
/// <remarks>
/// Contains utilities for property name mapping between JSON and C# conventions, type conversion,
/// and other common query handling functions.
/// </remarks>
public static class QueryHelpers
{
    /// <summary>
    /// Resolves a JSON property name to the corresponding C# property in an entity type.
    /// </summary>
    /// <param name="entityType">The entity type to search for the property</param>
    /// <param name="jsonPropertyName">The JSON property name (typically camelCase)</param>
    /// <returns>The matching PropertyInfo if found, or null if no matching property exists</returns>
    /// <remarks>
    /// Attempts to match properties in the following order:
    /// <list type="number">
    /// <item>
    /// <description>Exact match (case-sensitive)</description>
    /// </item>
    /// <item>
    /// <description>PascalCase version of the JSON name</description>
    /// </item>
    /// <item>
    /// <description>Case-insensitive match</description>
    /// </item>
    /// </list>
    /// This handles the common case of converting between camelCase (JSON) and PascalCase (C#) property names.
    /// </remarks>
    public static PropertyInfo? GetPropertyByJsonName(Type entityType, string jsonPropertyName)
    {
        PropertyInfo? property = entityType.GetProperty(jsonPropertyName);

        if (property != null)
            return property;

        string pascalCase = StringExtensions.ToPascalCase(jsonPropertyName);
        property = entityType.GetProperty(pascalCase);

        return property
            ?? entityType
                .GetProperties()
                .FirstOrDefault(p =>
                    string.Equals(p.Name, jsonPropertyName, StringComparison.OrdinalIgnoreCase)
                );
    }

    /// <summary>
    /// Converts a string value from a query parameter to the appropriate target property type.
    /// </summary>
    /// <param name="value">The string value from the query parameter</param>
    /// <param name="targetType">The target property type to convert to</param>
    /// <returns>
    /// The converted value, or trows an exception if conversion fails or is not supported
    /// </returns>
    /// <remarks>
    /// Handles common primitive types (int, long, decimal, bool, DateTime, Guid, Uri, TimeSpan,
    /// byte[], etc.) and their nullable variants.
    /// Also supports enum types, converting the string to the corresponding enum value.
    /// For DateTime values, assumes UTC if no timezone is specified.
    /// </remarks>
    public static object? ConvertToPropertyType(string value, Type targetType)
    {
        try
        {
            Type nonNullableType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (nonNullableType.IsEnum)
                return Enum.Parse(nonNullableType, value, ignoreCase: true);

            if (nonNullableType == typeof(string))
                return value;

            if (nonNullableType == typeof(int))
                return int.Parse(value, CultureInfo.InvariantCulture);

            if (nonNullableType == typeof(long))
                return long.Parse(value, CultureInfo.InvariantCulture);

            if (nonNullableType == typeof(decimal))
                return decimal.Parse(value, CultureInfo.InvariantCulture);

            if (nonNullableType == typeof(double))
                return double.Parse(value, CultureInfo.InvariantCulture);

            if (nonNullableType == typeof(float))
                return float.Parse(value, CultureInfo.InvariantCulture);

            if (nonNullableType == typeof(bool))
                return bool.Parse(value);

            if (nonNullableType == typeof(DateTime))
            {
                return DateTime.Parse(
                    value,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal
                );
            }

            if (nonNullableType == typeof(Guid))
                return Guid.Parse(value);

            if (nonNullableType == typeof(TimeSpan))
                return TimeSpan.Parse(value, CultureInfo.InvariantCulture);

            if (nonNullableType == typeof(Uri))
                return new Uri(value, UriKind.RelativeOrAbsolute);

            if (nonNullableType == typeof(byte[]))
                return Convert.FromBase64String(value);

            // Add more types as needed...

            // If you want to support collections (e.g., int[], List<int>), you can add logic here

            // Fallback: try to use Convert.ChangeType
            if (typeof(IConvertible).IsAssignableFrom(nonNullableType))
                return Convert.ChangeType(value, nonNullableType, CultureInfo.InvariantCulture);

            // If you reach here, type is not supported
            throw new NotSupportedException(
                $"Conversion for type '{targetType.FullName}' is not supported."
            );
        }
        catch (Exception ex)
        {
            throw new FormatException(
                $"Failed to convert '{value}' to type '{targetType.FullName}': {ex.Message}",
                ex
            );
        }
    }
}

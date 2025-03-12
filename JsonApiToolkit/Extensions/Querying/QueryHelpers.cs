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
    /// <returns>The converted value of the appropriate type, or null if conversion fails</returns>
    /// <remarks>
    /// Handles common primitive types (int, long, decimal, bool, DateTime, Guid) and their nullable variants.
    /// For DateTime values, assumes UTC if no timezone is specified.
    /// Returns null if conversion fails, allowing for graceful handling of invalid filter values.
    /// </remarks>
    public static object? ConvertToPropertyType(string value, Type targetType)
    {
        try
        {
            if (targetType == typeof(string))
                return value;

            if (targetType == typeof(int) || targetType == typeof(int?))
                return int.Parse(value);

            if (targetType == typeof(long) || targetType == typeof(long?))
                return long.Parse(value);

            if (targetType == typeof(decimal) || targetType == typeof(decimal?))
                return decimal.Parse(value);

            if (targetType == typeof(double) || targetType == typeof(double?))
                return double.Parse(value);

            if (targetType == typeof(bool) || targetType == typeof(bool?))
                return bool.Parse(value);

            if (targetType == typeof(DateTime) || targetType == typeof(DateTime?))
            {
                return DateTime.Parse(
                    value,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal
                );
            }

            if (targetType == typeof(Guid) || targetType == typeof(Guid?))
                return Guid.Parse(value);

            return Convert.ChangeType(value, targetType);
        }
        catch
        {
            return null;
        }
    }
}

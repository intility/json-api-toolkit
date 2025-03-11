using System.Globalization;
using System.Reflection;

namespace JsonApiToolkit.Extensions.Querying;

/// <summary>
/// Helper class for query handling.
/// </summary>
public static class QueryHelpers
{
    /// <summary>
    /// Gets a property by its JSON name.
    /// </summary>
    /// <param name="entityType">The type of the entity.</param>
    /// <param name="jsonPropertyName">The JSON name of the property.</param>
    /// <returns>The property with the JSON name.</returns>
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
    /// Converts a string value to the target property type.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="targetType">The target property type.</param>
    /// <returns>The converted value.</returns>
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

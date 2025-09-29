using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;

namespace JsonApiToolkit.Extensions.Querying;

/// <summary>
/// Helper methods for query parameter interpretation and conversion.
/// Caches property lookups for performance.
/// </summary>
public static class QueryHelpers
{
    private static readonly ConcurrentDictionary<(Type, string), PropertyInfo?> s_propertyCache =
        new();

    /// <summary>
    /// Resolves JSON property name to C# property.
    /// Tries: exact match, PascalCase, then case-insensitive.
    /// </summary>
    public static PropertyInfo? GetPropertyByJsonName(Type entityType, string jsonPropertyName)
    {
        return s_propertyCache.GetOrAdd(
            (entityType, jsonPropertyName),
            key =>
            {
                var (type, name) = key;

                PropertyInfo? property = type.GetProperty(name);
                if (property != null)
                    return property;

                string pascalCase = name.ToPascalCase();
                property = type.GetProperty(pascalCase);

                return property
                    ?? type.GetProperties()
                        .FirstOrDefault(p =>
                            string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)
                        );
            }
        );
    }

    /// <summary>
    /// Converts query parameter string to target property type.
    /// Supports primitives, enums, DateTime (assumes UTC), Guid, Uri, TimeSpan, byte[].
    /// </summary>
    public static object? ConvertToPropertyType(string value, Type targetType)
    {
        try
        {
            Type nonNullableType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (nonNullableType.IsEnum)
            {
                if (Enum.TryParse(nonNullableType, value, ignoreCase: true, out object? result))
                    return result;
                throw new ArgumentException(
                    $"Invalid enum value '{value}' for type '{nonNullableType.Name}'"
                );
            }

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
            if (nonNullableType == typeof(DateOnly))
            {
                return DateOnly.Parse(value, CultureInfo.InvariantCulture);
            }

            if (nonNullableType == typeof(TimeOnly))
            {
                return TimeOnly.Parse(value, CultureInfo.InvariantCulture);
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
                $"Failed to convert filter value '{value}' to type '{targetType.FullName}'. "
                    + $"Expected format examples: "
                    + $"int: '42', decimal: '12.34', DateTime: '2023-12-25T10:30:00Z', bool: 'true'/'false', "
                    + $"Guid: '550e8400-e29b-41d4-a716-446655440000'. "
                    + $"Error: {ex.Message}",
                ex
            );
        }
    }
}

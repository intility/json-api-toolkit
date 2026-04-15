using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;

namespace JsonApiToolkit.Helpers;

/// <summary>
/// Maps include paths to CLR property names.
/// Caches property lookups for performance.
/// </summary>
public static class EfIncludePathHelper
{
    private static readonly ConcurrentDictionary<(Type, string), string> s_includePathCache = new();

    /// <summary>
    /// Maps include paths to CLR property names for the given type.
    /// </summary>
    public static List<string> MapIncludePathsToClrProperties<T>(List<string>? includePaths)
    {
        if (includePaths == null || includePaths.Count == 0)
            return [];

        var type = typeof(T);

        return includePaths
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(path =>
                s_includePathCache.GetOrAdd(
                    (type, path),
                    key => MapSinglePath(key.Item1, key.Item2)
                )
            )
            .ToList();
    }

    private static string MapSinglePath(Type startType, string path)
    {
        var parts = path.Split('.');
        var mappedParts = new string[parts.Length];
        var currentType = startType;

        for (int i = 0; i < parts.Length; i++)
        {
            PropertyInfo? prop = currentType
                .GetProperties()
                .FirstOrDefault(p =>
                    string.Equals(p.Name, parts[i], StringComparison.OrdinalIgnoreCase)
                );

            if (prop == null)
            {
                // Property not found, keep original and stop
                for (int j = i; j < parts.Length; j++)
                    mappedParts[j] = parts[j];
                break;
            }

            mappedParts[i] = prop.Name;
            currentType = prop.PropertyType;

            // Handle collections
            if (typeof(IEnumerable).IsAssignableFrom(currentType) && currentType != typeof(string))
            {
                currentType = currentType.IsGenericType
                    ? currentType.GetGenericArguments()[0]
                    : typeof(object);
            }
        }

        return string.Join('.', mappedParts);
    }
}

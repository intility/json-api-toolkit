using System.Collections;

namespace JsonApiToolkit.Helpers;

/// <summary>
/// Provides utilities for mapping between include paths and CLR properties.
/// </summary>
public static class EfIncludePathHelper
{
    /// <summary>
    /// Maps a list of include paths to CLR properties for a given type.
    /// </summary>
    /// <typeparam name="T">The type to map include paths to</typeparam>
    /// <param name="includePaths">The list of include paths to map</param>
    /// <returns>A list of mapped CLR property names</returns>
    public static List<string> MapIncludePathsToClrProperties<T>(List<string>? includePaths)
    {
        if (includePaths == null)
            return [];

        var type = typeof(T);
        var mapped = new List<string>();

        foreach (var path in includePaths)
        {
            if (string.IsNullOrWhiteSpace(path))
                continue;

            var parts = path.Split('.');
            var mappedParts = new List<string>();
            var currentType = type;

            foreach (var part in parts)
            {
                var prop = currentType
                    .GetProperties()
                    .FirstOrDefault(p =>
                        string.Equals(p.Name, part, StringComparison.OrdinalIgnoreCase)
                    );
                if (prop == null)
                {
                    mappedParts.Add(part);
                    break;
                }
                mappedParts.Add(prop.Name);
                currentType = prop.PropertyType;
                if (
                    typeof(IEnumerable).IsAssignableFrom(currentType)
                    && currentType != typeof(string)
                )
                {
                    currentType = currentType.IsGenericType
                        ? currentType.GetGenericArguments()[0]
                        : typeof(object);
                }
            }

            mapped.Add(string.Join('.', mappedParts));
        }

        return mapped;
    }
}

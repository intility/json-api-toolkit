using System.Reflection;
using JsonApiToolkit.Extensions;
using JsonApiToolkit.Mapping;

namespace JsonApiToolkit.Extensions.Projection;

/// <summary>
/// Determines which source properties should be included in a database projection.
/// </summary>
internal static class ProjectionPropertySelector
{
    /// <summary>
    /// Returns the properties from <paramref name="sourceType"/> to project into.
    /// Always includes the ID property, plus requested scalar attributes and navigations
    /// needed for active includes.
    /// </summary>
    /// <param name="sourceType">The entity type being projected.</param>
    /// <param name="requestedFieldsCamelCase">Field names from <c>fields[type]</c> (camelCase).</param>
    /// <param name="mappedIncludesCLRPaths">CLR property paths from mapped includes (e.g., "Author", "Author.Posts").</param>
    internal static IReadOnlyList<PropertyInfo> Determine(
        Type sourceType,
        List<string> requestedFieldsCamelCase,
        List<string> mappedIncludesCLRPaths
    )
    {
        var result = new List<PropertyInfo>();

        // ID property is always required for JSON:API resource objects
        PropertyInfo? idProp = EntityMapper.GetIdProperty(sourceType);
        if (idProp != null)
            result.Add(idProp);

        // Scalar attribute properties filtered to what the client requested
        var fieldSet = new HashSet<string>(
            requestedFieldsCamelCase,
            StringComparer.OrdinalIgnoreCase
        );
        foreach (PropertyInfo prop in EntityMapper.GetAttributeProperties(sourceType))
        {
            if (fieldSet.Contains(prop.Name.ToCamelCase()))
                result.Add(prop);
        }

        // Navigation (relationship) properties needed for active includes.
        // mappedIncludesCLRPaths may be nested (e.g., "Author.Posts") — only the first
        // segment is a direct property on sourceType.
        var topLevelIncludes = mappedIncludesCLRPaths
            .Select(path => path.Split('.')[0])
            .ToHashSet(StringComparer.Ordinal);

        foreach (PropertyInfo prop in EntityMapper.GetRelationshipProperties(sourceType))
        {
            if (topLevelIncludes.Contains(prop.Name))
                result.Add(prop);
        }

        return result;
    }
}

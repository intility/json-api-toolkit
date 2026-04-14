using System.Reflection;
using JsonApiToolkit.Mapping;

namespace JsonApiToolkit.Extensions.Projection;

/// <summary>
/// Determines which source properties should be included in a database projection.
/// Projection is only applied when no includes are active, so only scalar attributes
/// (plus the ID) are ever projected.
/// </summary>
internal static class ProjectionPropertySelector
{
    /// <summary>
    /// Returns the properties from <paramref name="sourceType"/> to project into.
    /// Always includes the ID property, plus the scalar attributes requested via
    /// <c>fields[type]</c>.
    /// </summary>
    /// <param name="sourceType">The entity type being projected.</param>
    /// <param name="requestedFieldsCamelCase">Field names from <c>fields[type]</c> (camelCase).</param>
    internal static IReadOnlyList<PropertyInfo> Determine(
        Type sourceType,
        List<string> requestedFieldsCamelCase
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
            if (fieldSet.Contains(EntityMapper.GetAttributeName(prop)))
                result.Add(prop);
        }

        return result;
    }
}

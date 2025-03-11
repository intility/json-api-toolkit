using System.Collections;
using System.Reflection;

namespace JsonApiToolkit.Mapping;

/// <summary>
/// Helper class for mapping relationships between entities.
/// </summary>
public static class RelationshipMapper
{
    /// <summary>
    /// Cache of relationship properties for each type.
    /// </summary>
    private static readonly Dictionary<Type, List<PropertyInfo>> s_relationshipPropertyCache = [];

    /// <summary>
    /// Gets the properties of the specified type that represent relationships to other entities.
    /// </summary>
    /// <param name="type">The type to get the relationship properties for.</param>
    /// <returns>The relationship properties.</returns>
    public static List<PropertyInfo> GetRelationshipProperties(Type type)
    {
        if (s_relationshipPropertyCache.TryGetValue(type, out List<PropertyInfo>? props))
            return props;

        props = type.GetProperties()
            .Where(p =>
                p.CanRead
                && p.GetMethod?.IsPublic == true
                && (
                    (
                        typeof(IEnumerable).IsAssignableFrom(p.PropertyType)
                        && p.PropertyType != typeof(string)
                    )
                    || (
                        !p.PropertyType.IsPrimitive
                        && !p.PropertyType.IsValueType
                        && p.PropertyType != typeof(string)
                        && p.PropertyType != typeof(DateTime)
                        && p.PropertyType != typeof(DateTime?)
                        && p.PropertyType != typeof(Guid)
                        && p.PropertyType != typeof(Guid?)
                    )
                )
            )
            .ToList();

        s_relationshipPropertyCache[type] = props;
        return props;
    }
}

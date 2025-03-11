using System.Collections;
using System.Reflection;
using JsonApiToolkit.Extensions;

namespace JsonApiToolkit.Mapping;

/// <summary>
/// Helper class for mapping entities.
/// </summary>
public static class EntityMapper
{
    private static readonly Dictionary<Type, PropertyInfo?> s_idPropertyCache = [];
    private static readonly Dictionary<Type, List<PropertyInfo>> s_attributePropertyCache = [];

    /// <summary>
    /// Gets the ID property of an entity.
    /// </summary>
    /// <param name="type">The type of the entity.</param>
    /// <returns>The ID property of the entity.</returns>
    public static PropertyInfo? GetIdProperty(Type type)
    {
        if (s_idPropertyCache.TryGetValue(type, out PropertyInfo? idProperty))
            return idProperty;

        idProperty =
            type.GetProperty("Id")
            ?? type.GetProperty($"{type.Name}Id")
            ?? type.GetProperties().FirstOrDefault(p => p.Name.EndsWith("Id"));

        s_idPropertyCache[type] = idProperty;
        return idProperty;
    }

    /// <summary>
    /// Gets the attribute properties of an entity.
    /// </summary>
    /// <param name="type">The type of the entity.</param>
    /// <returns>The attribute properties of the entity.</returns>
    public static List<PropertyInfo> GetAttributeProperties(Type type)
    {
        if (s_attributePropertyCache.TryGetValue(type, out List<PropertyInfo>? props))
            return props;

        List<PropertyInfo> relationshipProps = RelationshipMapper.GetRelationshipProperties(type);
        var relationshipNames = relationshipProps.Select(p => p.Name).ToHashSet();

        props = type.GetProperties()
            .Where(p =>
                !p.Name.EndsWith("Id")
                && p.Name != "Id"
                && !relationshipNames.Contains(p.Name)
                && p.CanRead
                && p.GetMethod?.IsPublic == true
                && (
                    p.PropertyType == typeof(string)
                    || (
                        !typeof(IEnumerable).IsAssignableFrom(p.PropertyType)
                        || p.PropertyType == typeof(string)
                    )
                )
            )
            .ToList();

        s_attributePropertyCache[type] = props;
        return props;
    }

    /// <summary>
    /// Gets the type of a resource object.
    /// </summary>
    /// <param name="type">The type of the resource object.</param>
    /// <returns>The type of the resource object.</returns>
    public static string GetResourceType(Type type)
    {
        string name = type.Name;
        return name.ToCamelCase();
    }
}

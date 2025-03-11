using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using JsonApiToolkit.Extensions;

namespace JsonApiToolkit.Mapping;

/// <summary>
/// Helper class for mapping entities.
/// </summary>
public static class EntityMapper
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo?> s_idPropertyCache = new();
    private static readonly ConcurrentDictionary<
        Type,
        List<PropertyInfo>
    > s_attributePropertyCache = new();
    private static readonly ConcurrentDictionary<
        Type,
        List<PropertyInfo>
    > s_relationshipPropertyCache = new();

    /// <summary>
    /// Gets the ID property of a resource object.
    /// </summary>
    /// <param name="type">The type of the resource object.</param>
    /// <returns>The ID property of the resource object.</returns>
    public static PropertyInfo? GetIdProperty(Type type)
    {
        return s_idPropertyCache.GetOrAdd(
            type,
            t =>
            {
                return t.GetProperty("Id")
                    ?? t.GetProperty($"{t.Name}Id")
                    ?? t.GetProperties().FirstOrDefault(p => p.Name.EndsWith("Id"));
            }
        );
    }

    /// <summary>
    /// Gets the attribute properties of a resource object.
    /// </summary>
    /// <param name="type">The type of the resource object.</param>
    /// <returns>The attribute properties of the resource object.</returns>
    public static List<PropertyInfo> GetAttributeProperties(Type type)
    {
        return s_attributePropertyCache.GetOrAdd(
            type,
            t =>
            {
                List<PropertyInfo> relationshipProps = GetRelationshipProperties(t);
                var relationshipNames = relationshipProps.Select(p => p.Name).ToHashSet();

                return t.GetProperties()
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
            }
        );
    }

    /// <summary>
    /// Gets the relationship properties of a resource object.
    /// </summary>
    /// <param name="type">The type of the resource object.</param>
    /// <returns>The relationship properties of the resource object.</returns>
    public static List<PropertyInfo> GetRelationshipProperties(Type type)
    {
        return s_relationshipPropertyCache.GetOrAdd(
            type,
            t =>
            {
                return t.GetProperties()
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
            }
        );
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

using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using JsonApiToolkit.Extensions;

namespace JsonApiToolkit.Mapping;

/// <summary>
/// Provides utilities for mapping between entity models and JSON:API resource objects.
/// </summary>
/// <remarks>
/// This static class contains methods for determining ID properties, attributes, and relationships
/// for entity mapping purposes. It caches property information to improve performance in repeated mapping operations.
/// </remarks>
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
    /// Identifies and retrieves the primary key property for an entity type.
    /// </summary>
    /// <param name="type">The entity type to analyze</param>
    /// <returns>The PropertyInfo for the ID property, or null if not found</returns>
    /// <remarks>
    /// Uses a cached approach to improve performance over repeated calls. Attempts to find the ID property in the following order:
    /// <list type="number">
    /// <item>
    /// <description>A property named "Id"</description>
    /// </item>
    /// <item>
    /// <description>A property named "{TypeName}Id" (e.g., "PersonId" for a "Person" entity)</description>
    /// </item>
    /// <item>
    /// <description>Any property ending with "Id"</description>
    /// </item>
    /// </list>
    /// </remarks>
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
    /// Identifies the properties that should be mapped as attributes in a JSON:API resource object.
    /// </summary>
    /// <param name="type">The entity type to analyze</param>
    /// <returns>A list of PropertyInfo objects representing the attributes</returns>
    /// <remarks>
    /// Uses a cached approach to improve performance over repeated calls. Excludes:
    /// <list type="bullet">
    /// <item>
    /// <description>The primary ID property (to avoid duplication with the resource's id field)</description>
    /// </item>
    /// <item>
    /// <description>Properties identified as relationships</description>
    /// </item>
    /// <item>
    /// <description>Properties that can't be read or aren't public</description>
    /// </item>
    /// <item>
    /// <description>Collection properties (except strings)</description>
    /// </item>
    /// </list>
    /// The resulting properties typically represent scalar values of the entity, including foreign key IDs.
    /// </remarks>
    public static List<PropertyInfo> GetAttributeProperties(Type type)
    {
        return s_attributePropertyCache.GetOrAdd(
            type,
            t =>
            {
                PropertyInfo? idProperty = GetIdProperty(t);
                List<PropertyInfo> relationshipProps = GetRelationshipProperties(t);
                var relationshipNames = relationshipProps.Select(p => p.Name).ToHashSet();

                return t.GetProperties()
                    .Where(p =>
                        p != idProperty // Exclude only the primary ID
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
    /// Identifies the properties that should be mapped as relationships in a JSON:API resource object.
    /// </summary>
    /// <param name="type">The entity type to analyze</param>
    /// <returns>A list of PropertyInfo objects representing the relationships</returns>
    /// <remarks>
    /// Uses a cached approach to improve performance over repeated calls.
    /// <para>Identifies two types of relationships:</para>
    /// <list type="bullet">
    /// <item>
    /// <description>Collections (IEnumerable properties that aren't strings) - representing to-many relationships</description>
    /// </item>
    /// <item>
    /// <description>Complex object properties (non-primitive, non-value types) - representing to-one relationships</description>
    /// </item>
    /// </list>
    /// <para>Excludes common value types like string, DateTime, and Guid.</para>
    /// </remarks>
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
    /// Determines the JSON:API resource type name for an entity type.
    /// </summary>
    /// <param name="type">The entity type</param>
    /// <returns>The camelCase resource type name</returns>
    /// <remarks>
    /// By convention, uses the entity class name in camelCase as the resource type.
    /// For example, a "Person" entity class becomes a "person" resource type.
    /// </remarks>
    public static string GetResourceType(Type type)
    {
        string name = type.Name;
        return name.ToCamelCase();
    }
}

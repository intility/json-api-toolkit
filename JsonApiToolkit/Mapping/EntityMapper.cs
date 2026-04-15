using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json.Serialization;
using JsonApiToolkit.Extensions;
using JsonApiToolkit.Extensions.Querying;

namespace JsonApiToolkit.Mapping;

/// <summary>
/// Maps entity models to JSON:API resource objects.
/// Caches property information for performance.
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
    /// Gets the primary key property for an entity.
    /// Searches for: "Id", "{TypeName}Id", or any property ending with "Id".
    /// </summary>
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
    /// Gets properties to map as JSON:API attributes.
    /// Excludes: ID property, relationships, non-public/unreadable properties.
    /// </summary>
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
                        && !relationshipNames.Contains(p.Name) // Exclude properties identified as relationships
                        && p.CanRead
                        && p.GetMethod?.IsPublic == true
                        && !HasJsonIgnoreAttribute(p) // Exclude properties marked with [JsonIgnore]
                    )
                    .ToList();
            }
        );
    }

    /// <summary>
    /// Gets properties to map as JSON:API relationships.
    /// Includes collections and complex objects that have ID properties.
    /// Excludes primitives, value types, strings, DateTime, Guid, and owned entities without IDs.
    /// </summary>
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
                        && !HasJsonIgnoreAttribute(p)
                        && (IsCollectionRelationship(p) || IsSingleObjectRelationship(p))
                    )
                    .ToList();
            }
        );
    }

    private static bool IsCollectionRelationship(PropertyInfo p) =>
        typeof(IEnumerable).IsAssignableFrom(p.PropertyType)
        && p.PropertyType != typeof(string)
        && HasIdProperty(TypeHelpers.GetCollectionElementType(p.PropertyType));

    private static bool IsSingleObjectRelationship(PropertyInfo p) =>
        !p.PropertyType.IsPrimitive
        && !p.PropertyType.IsValueType
        && p.PropertyType != typeof(string)
        && p.PropertyType != typeof(DateTime)
        && p.PropertyType != typeof(DateTime?)
        && p.PropertyType != typeof(Guid)
        && p.PropertyType != typeof(Guid?)
        && !typeof(IEnumerable).IsAssignableFrom(p.PropertyType)
        && HasIdProperty(p.PropertyType);

    /// <summary>
    /// Gets the JSON:API resource type name (entity class name in camelCase).
    /// Example: "Person" becomes "person".
    /// </summary>
    public static string GetResourceType(Type type)
    {
        string name = type.Name;
        return name.ToCamelCase();
    }

    /// <summary>
    /// Gets the JSON:API attribute name for a property.
    /// Centralizes name resolution so that the serializer and projection field matching
    /// always use the same logic and cannot silently diverge.
    /// </summary>
    public static string GetAttributeName(PropertyInfo property) =>
        property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
        ?? property.Name.ToCamelCase();

    /// <summary>
    /// Checks if a type has an ID property.
    /// </summary>
    /// <param name="type">The type to check</param>
    /// <returns>True if the type has an ID property, false otherwise</returns>
    private static bool HasIdProperty(Type? type)
    {
        if (type == null)
            return false;
        return GetIdProperty(type) != null;
    }

    /// <summary>
    /// Checks if a property has the JsonIgnore attribute.
    /// </summary>
    private static bool HasJsonIgnoreAttribute(PropertyInfo property)
    {
        return property.GetCustomAttribute<JsonIgnoreAttribute>() != null;
    }
}

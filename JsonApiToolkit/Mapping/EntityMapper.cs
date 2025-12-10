using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json.Serialization;
using JsonApiToolkit.Extensions;

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
                        && !HasJsonIgnoreAttribute(p) // Exclude properties marked with [JsonIgnore]
                        && (
                            (
                                typeof(IEnumerable).IsAssignableFrom(p.PropertyType)
                                && p.PropertyType != typeof(string)
                                && HasIdProperty(GetCollectionElementType(p.PropertyType)) // Only include collections where items have IDs
                            )
                            || (
                                !p.PropertyType.IsPrimitive
                                && !p.PropertyType.IsValueType
                                && p.PropertyType != typeof(string)
                                && p.PropertyType != typeof(DateTime)
                                && p.PropertyType != typeof(DateTime?)
                                && p.PropertyType != typeof(Guid)
                                && p.PropertyType != typeof(Guid?)
                                && !typeof(IEnumerable).IsAssignableFrom(p.PropertyType) // Exclude collections from complex object relationships
                                && HasIdProperty(p.PropertyType) // Only include single objects that have ID properties as relationships
                            )
                        )
                    )
                    .ToList();
            }
        );
    }

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

    /// <summary>
    /// Gets the element type of a collection.
    /// </summary>
    /// <param name="collectionType">The collection type</param>
    /// <returns>The element type, or null if not a collection</returns>
    private static Type? GetCollectionElementType(Type collectionType)
    {
        // String is not considered a collection for our purposes
        if (collectionType == typeof(string))
        {
            return null;
        }

        // Check if it's a generic collection
        if (collectionType.IsGenericType)
        {
            Type[] genericArgs = collectionType.GetGenericArguments();
            if (genericArgs.Length == 1)
            {
                return genericArgs[0];
            }
        }

        // Check if it implements IEnumerable<T>
        Type? enumerable = collectionType
            .GetInterfaces()
            .FirstOrDefault(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>)
            );

        if (enumerable != null)
        {
            return enumerable.GetGenericArguments()[0];
        }

        // For non-generic collections, we can't determine the element type
        return null;
    }
}

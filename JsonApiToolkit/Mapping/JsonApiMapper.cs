using System.Collections;
using System.Reflection;
using JsonApiToolkit.Extensions;
using JsonApiToolkit.Models;
using ResourceIdentifier = JsonApiToolkit.Models.ResourceIdentifier;

namespace JsonApiToolkit.Mapping;

/// <summary>
/// A JSON:API mapper. Maps entities to JSON:API resources.
/// </summary>
public static class JsonApiMapper
{
    private static readonly Dictionary<Type, PropertyInfo?> s_idPropertyCache = [];
    private static readonly Dictionary<Type, List<PropertyInfo>> s_attributePropertyCache = [];
    private static readonly Dictionary<Type, List<PropertyInfo>> s_relationshipPropertyCache = [];

    /// <summary>
    /// Maps an entity to a JSON:API resource object.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="entity">The entity to map.</param>
    /// <param name="resourceType">The type of the resource object.</param>
    /// <param name="includedRelationships">Optional list of relationship paths to include.</param>
    /// <returns>The JSON:API resource object.</returns>
    public static ResourceObject ToResourceObject<T>(
        T entity,
        string resourceType,
        List<string>? includedRelationships = null
    )
        where T : class
    {
        ArgumentNullException.ThrowIfNull(entity);

        Type type = typeof(T);

        PropertyInfo? idProperty = GetIdProperty(type);
        string id =
            idProperty?.GetValue(entity)?.ToString()
            ?? throw new InvalidOperationException("Entity Id cannot be null");

        var resourceObject = new ResourceObject
        {
            Id = id,
            Type = resourceType,
            Attributes = [],
        };

        foreach (PropertyInfo prop in GetAttributeProperties(type))
        {
            object? value = prop.GetValue(entity);
            if (value != null)
            {
                resourceObject.Attributes![prop.Name.ToCamelCase()] = value;
            }
        }

        if (includedRelationships?.Count > 0)
        {
            // Map relationships if any
            List<PropertyInfo> relationshipProperties = GetRelationshipProperties(type);
            if (relationshipProperties.Count > 0)
            {
                resourceObject.Relationships = [];

                foreach (PropertyInfo relationshipProp in relationshipProperties)
                {
                    string relationshipName = relationshipProp.Name;

                    if (
                        !includedRelationships.Any(include =>
                            string.Equals(
                                include,
                                relationshipName,
                                StringComparison.OrdinalIgnoreCase
                            )
                        )
                    )
                    {
                        continue;
                    }
                    object? relValue = relationshipProp.GetValue(entity);
                    var relationship = new Relationship();

                    if (relValue == null)
                    {
                        relationship.Data = null;
                    }
                    else
                    {
                        Type relType = relationshipProp.PropertyType;
                        bool isCollection =
                            typeof(IEnumerable).IsAssignableFrom(relType)
                            && relType != typeof(string);

                        if (isCollection)
                        {
                            var relData = new List<ResourceIdentifier>();
                            foreach (object? item in (IEnumerable)relValue)
                            {
                                if (item == null)
                                    continue;

                                Type itemType = item.GetType();
                                PropertyInfo? itemIdProp = GetIdProperty(itemType);
                                if (itemIdProp == null)
                                    continue;

                                string? itemId = itemIdProp.GetValue(item)?.ToString();
                                if (itemId == null)
                                    continue;

                                relData.Add(
                                    new ResourceIdentifier
                                    {
                                        Id = itemId,
                                        Type = GetResourceType(itemType),
                                    }
                                );
                            }
                            relationship.Data = relData;
                        }
                        else
                        {
                            Type relItemType = relValue.GetType();
                            PropertyInfo? relItemIdProp = GetIdProperty(relItemType);
                            if (relItemIdProp != null)
                            {
                                string? relItemId = relItemIdProp.GetValue(relValue)?.ToString();
                                if (relItemId != null)
                                {
                                    relationship.Data = new ResourceIdentifier
                                    {
                                        Id = relItemId,
                                        Type = GetResourceType(relItemType),
                                    };
                                }
                            }
                        }
                    }

                    resourceObject.Relationships[relationshipName.ToCamelCase()] = relationship;
                }

                if (resourceObject.Relationships.Count == 0)
                    resourceObject.Relationships = null;
            }
        }
        return resourceObject;
    }

    /// <summary>
    /// Maps an entity to a JSON:API document.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="entity">The entity to map.</param>
    /// <param name="resourceType">The type of the resource object.</param>
    /// <param name="selfLink">The self link of the resource object.</param>
    /// <param name="includedRelationships">Optional list of relationship paths to include.</param>
    /// <returns>The JSON:API document.</returns>
    public static JsonApiDocument<ResourceObject> ToDocument<T>(
        T entity,
        string resourceType,
        string selfLink,
        List<string>? includedRelationships = null
    )
        where T : class
    {
        ResourceObject resource = ToResourceObject(entity, resourceType, includedRelationships);
        resource.Links = new Links { Self = selfLink };

        var document = new JsonApiDocument<ResourceObject>
        {
            Data = resource,
            Links = new Links { Self = selfLink },
        };

        if (includedRelationships?.Count > 0)
        {
            var included = new List<ResourceObject>();
            AddIncludedResources(entity, includedRelationships, included);
            if (included.Count > 0)
            {
                document.Included = included;
            }
        }

        return document;
    }

    /// <summary>
    /// Maps a collection of entities to a JSON:API collection document with support for pagination.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="entities">The entities to map.</param>
    /// <param name="resourceType">The type of the resource object.</param>
    /// <param name="selfLink">The self link of the resource object.</param>
    /// <param name="paginationMeta">Optional pagination metadata.</param>
    /// <param name="includedRelationships">Optional list of relationship paths to include.</param>
    /// <returns>The JSON:API collection document.</returns>
    public static JsonApiCollectionDocument<ResourceObject> ToCollectionDocument<T>(
        IEnumerable<T> entities,
        string resourceType,
        string selfLink,
        PaginationMeta? paginationMeta = null,
        List<string>? includedRelationships = null
    )
        where T : class
    {
        string baseUrl = selfLink.Split('?')[0];

        var resources = entities
            .Select(e =>
            {
                ResourceObject resource = ToResourceObject(e, resourceType, includedRelationships);
                resource.Links = new Links { Self = $"{baseUrl}/{resource.Id}" };
                return resource;
            })
            .ToList();

        var document = new JsonApiCollectionDocument<ResourceObject>
        {
            Data = resources,
            Links = new Links { Self = selfLink },
        };

        if (paginationMeta != null)
        {
            document.Meta = new Dictionary<string, object>
            {
                ["pagination"] = new
                {
                    totalResources = paginationMeta.TotalResources,
                    totalPages = paginationMeta.TotalPages,
                    currentPage = paginationMeta.CurrentPage,
                    pageSize = paginationMeta.PageSize,
                },
            };

            int pageSize = paginationMeta.PageSize;

            document.Links.First = $"{baseUrl}?page[number]=1&page[size]={pageSize}";
            document.Links.Last =
                $"{baseUrl}?page[number]={paginationMeta.TotalPages}&page[size]={pageSize}";

            if (paginationMeta.CurrentPage > 1)
            {
                document.Links.Prev =
                    $"{baseUrl}?page[number]={paginationMeta.CurrentPage - 1}&page[size]={pageSize}";
            }

            if (paginationMeta.CurrentPage < paginationMeta.TotalPages)
            {
                document.Links.Next =
                    $"{baseUrl}?page[number]={paginationMeta.CurrentPage + 1}&page[size]={pageSize}";
            }
        }

        if (includedRelationships?.Count > 0)
        {
            var included = new List<ResourceObject>();
            foreach (T entity in entities)
            {
                AddIncludedResources(entity, includedRelationships, included);
            }

            if (included.Count > 0)
            {
                document.Included = included
                    .GroupBy(r => new { r.Type, r.Id })
                    .Select(g => g.First())
                    .ToList();
            }
        }

        return document;
    }

    private static PropertyInfo? GetIdProperty(Type type)
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

    private static List<PropertyInfo> GetAttributeProperties(Type type)
    {
        if (s_attributePropertyCache.TryGetValue(type, out List<PropertyInfo>? props))
            return props;

        List<PropertyInfo> relationshipProps = GetRelationshipProperties(type);
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

    private static List<PropertyInfo> GetRelationshipProperties(Type type)
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

    private static void AddIncludedResources<T>(
        T entity,
        List<string> includePaths,
        List<ResourceObject> included,
        HashSet<string>? processedEntities = null
    )
        where T : class
    {
        if (entity == null)
            return;

        processedEntities ??= [];

        foreach (string includePath in includePaths)
        {
            string[] pathParts = includePath.Split('.');
            AddIncludedResourcesRecursive(entity, pathParts, 0, included, processedEntities);
        }
    }

    private static void AddIncludedResourcesRecursive<T>(
        T entity,
        string[] pathParts,
        int depth,
        List<ResourceObject> included,
        HashSet<string> processedEntities
    )
        where T : class
    {
        if (entity == null || depth >= pathParts.Length)
            return;

        string propertyName = pathParts[depth];
        PropertyInfo? property = typeof(T)
            .GetProperties()
            .FirstOrDefault(p =>
                string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase)
            );

        if (property == null)
            return;

        object? value = property.GetValue(entity);
        if (value == null)
            return;

        if (
            typeof(IEnumerable).IsAssignableFrom(property.PropertyType)
            && property.PropertyType != typeof(string)
        )
        {
            foreach (object? item in (IEnumerable)value)
            {
                if (item == null)
                    continue;

                Type itemType = item.GetType();
                PropertyInfo? idProperty = GetIdProperty(itemType);
                if (idProperty == null)
                    continue;

                string? itemId = idProperty.GetValue(item)?.ToString();
                if (itemId == null)
                    continue;

                string entityKey = $"{itemType.Name}:{itemId}";

                if (processedEntities.Contains(entityKey))
                    continue;

                processedEntities.Add(entityKey);

                try
                {
                    var resourceObject = new ResourceObject
                    {
                        Id = itemId,
                        Type = GetResourceType(itemType),
                        Attributes = [],
                    };

                    // Add all attributes
                    foreach (PropertyInfo prop in GetAttributeProperties(itemType))
                    {
                        object? propValue = prop.GetValue(item);
                        if (propValue != null)
                            resourceObject.Attributes[prop.Name.ToCamelCase()] = propValue;
                    }

                    included.Add(resourceObject);

                    if (depth + 1 < pathParts.Length)
                    {
                        AddIncludedResourcesRecursive(
                            item,
                            pathParts,
                            depth + 1,
                            included,
                            processedEntities
                        );
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine("Failed to process included resource");
                }
            }
        }
        else
        {
            Type relType = value.GetType();
            PropertyInfo? idProperty = GetIdProperty(relType);
            if (idProperty == null)
                return;

            string? relId = idProperty.GetValue(value)?.ToString();
            if (relId == null)
                return;

            string entityKey = $"{relType.Name}:{relId}";

            if (processedEntities.Contains(entityKey))
                return;

            processedEntities.Add(entityKey);

            try
            {
                var resourceObject = new ResourceObject
                {
                    Id = relId,
                    Type = GetResourceType(relType),
                    Attributes = [],
                };

                foreach (PropertyInfo prop in GetAttributeProperties(relType))
                {
                    object? propValue = prop.GetValue(value);
                    if (propValue != null)
                    {
                        resourceObject.Attributes[prop.Name.ToCamelCase()] = propValue;
                    }
                }

                included.Add(resourceObject);

                if (depth + 1 < pathParts.Length)
                {
                    AddIncludedResourcesRecursive(
                        value,
                        pathParts,
                        depth + 1,
                        included,
                        processedEntities
                    );
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Failed to process included resource");
            }
        }
    }

    private static string GetResourceType(Type type)
    {
        string name = type.Name;
        return name.ToCamelCase();
    }
}

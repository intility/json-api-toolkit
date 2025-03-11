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

        var type = typeof(T);

        // Get ID property (cached)
        var idProperty = GetIdProperty(type);
        var id =
            idProperty?.GetValue(entity)?.ToString()
            ?? throw new InvalidOperationException("Entity Id cannot be null");

        var resourceObject = new ResourceObject
        {
            Id = id,
            Type = resourceType,
            Attributes = [],
        };

        // Map properties to attributes
        foreach (var prop in GetAttributeProperties(type))
        {
            var value = prop.GetValue(entity);
            if (value != null)
            {
                resourceObject.Attributes![prop.Name.ToCamelCase()] = value;
            }
        }

        // Map relationships if includedRelationships is not null or empty
        if (includedRelationships?.Count > 0)
        {
            // Map relationships if any
            var relationshipProperties = GetRelationshipProperties(type);
            if (relationshipProperties.Count > 0)
            {
                resourceObject.Relationships = [];

                foreach (var relationshipProp in relationshipProperties)
                {
                    var relationshipName = relationshipProp.Name;

                    // Only include this relationship if it's in the requested includes
                    // Case-insensitive comparison
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
                    var relValue = relationshipProp.GetValue(entity);
                    var relationship = new Relationship();

                    if (relValue == null)
                    {
                        relationship.Data = null;
                    }
                    else
                    {
                        var relType = relationshipProp.PropertyType;
                        var isCollection =
                            typeof(IEnumerable).IsAssignableFrom(relType)
                            && relType != typeof(string);

                        if (isCollection)
                        {
                            // Collection relationship (to-many)
                            var relData = new List<ResourceIdentifier>();
                            foreach (var item in (IEnumerable)relValue)
                            {
                                if (item == null)
                                    continue;

                                var itemType = item.GetType();
                                var itemIdProp = GetIdProperty(itemType);
                                if (itemIdProp == null)
                                    continue;

                                var itemId = itemIdProp.GetValue(item)?.ToString();
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
                            // Single relationship (to-one)
                            var relItemType = relValue.GetType(); // Use actual type
                            var relItemIdProp = GetIdProperty(relItemType);
                            if (relItemIdProp != null)
                            {
                                var relItemId = relItemIdProp.GetValue(relValue)?.ToString();
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
                // If no relationships were added, set to null to exclude from response
                if (resourceObject.Relationships.Count == 0)
                {
                    resourceObject.Relationships = null;
                }
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
        // Pass includedRelationships to ToResourceObject
        var resource = ToResourceObject(entity, resourceType, includedRelationships);
        resource.Links = new Links { Self = selfLink };

        var document = new JsonApiDocument<ResourceObject>
        {
            Data = resource,
            Links = new Links { Self = selfLink },
        };

        // Add included resources if needed
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
                // Pass includedRelationships to ToResourceObject
                var resource = ToResourceObject(e, resourceType, includedRelationships);
                resource.Links = new Links { Self = $"{baseUrl}/{resource.Id}" };
                return resource;
            })
            .ToList();

        var document = new JsonApiCollectionDocument<ResourceObject>
        {
            Data = resources,
            Links = new Links { Self = selfLink },
        };

        // Add pagination links if metadata is provided
        if (paginationMeta != null)
        {
            // Add pagination metadata
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

            // Add pagination links
            var pageSize = paginationMeta.PageSize;

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

        // Add included resources if needed
        if (includedRelationships?.Count > 0)
        {
            var included = new List<ResourceObject>();
            foreach (var entity in entities)
            {
                AddIncludedResources(entity, includedRelationships, included);
            }

            if (included.Count > 0)
            {
                // Remove duplicates by ID and type
                var uniqueIncluded = included
                    .GroupBy(r => new { r.Type, r.Id })
                    .Select(g => g.First())
                    .ToList();

                document.Included = uniqueIncluded;
            }
        }

        return document;
    }

    private static PropertyInfo? GetIdProperty(Type type)
    {
        if (s_idPropertyCache.TryGetValue(type, out var idProperty))
        {
            return idProperty;
        }

        // Try to find an Id property (Id, EntityId, EntityTypeId)
        idProperty =
            type.GetProperty("Id")
            ?? type.GetProperty($"{type.Name}Id")
            ?? type.GetProperties().FirstOrDefault(p => p.Name.EndsWith("Id"));

        s_idPropertyCache[type] = idProperty;
        return idProperty;
    }

    private static List<PropertyInfo> GetAttributeProperties(Type type)
    {
        if (s_attributePropertyCache.TryGetValue(type, out var props))
        {
            return props;
        }

        // Get relationship properties first so we can exclude them from attributes
        var relationshipProps = GetRelationshipProperties(type);
        var relationshipNames = relationshipProps.Select(p => p.Name).ToHashSet();

        props = type.GetProperties()
            .Where(p =>
                // Skip Id properties
                !p.Name.EndsWith("Id")
                && p.Name != "Id"
                &&
                // Skip relationship properties
                !relationshipNames.Contains(p.Name)
                &&
                // Only include readable properties
                p.CanRead
                && p.GetMethod?.IsPublic == true
                &&
                // Include string properties
                (
                    p.PropertyType == typeof(string)
                    ||
                    // Skip collections except strings
                    (
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
        if (s_relationshipPropertyCache.TryGetValue(type, out var props))
        {
            return props;
        }

        // First identify all navigation properties
        props = type.GetProperties()
            .Where(p =>
                p.CanRead
                && p.GetMethod?.IsPublic == true
                && (
                    // Collections (except strings)
                    (
                        typeof(IEnumerable).IsAssignableFrom(p.PropertyType)
                        && p.PropertyType != typeof(string)
                    )
                    ||
                    // Reference navigation properties
                    (
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

        // Initialize tracking set to prevent circular processing
        processedEntities ??= [];

        foreach (var includePath in includePaths)
        {
            var pathParts = includePath.Split('.');
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

        var propertyName = pathParts[depth];
        var property = typeof(T)
            .GetProperties()
            .FirstOrDefault(p =>
                string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase)
            );

        if (property == null)
            return;

        var value = property.GetValue(entity);
        if (value == null)
            return;

        if (
            typeof(IEnumerable).IsAssignableFrom(property.PropertyType)
            && property.PropertyType != typeof(string)
        )
        {
            // Collection relationship
            foreach (var item in (IEnumerable)value)
            {
                if (item == null)
                    continue;

                var itemType = item.GetType();
                var idProperty = GetIdProperty(itemType);
                if (idProperty == null)
                    continue;

                var itemId = idProperty.GetValue(item)?.ToString();
                if (itemId == null)
                    continue;

                // Create a unique identifier for this entity
                var entityKey = $"{itemType.Name}:{itemId}";

                // Skip if we've already processed this entity
                if (processedEntities.Contains(entityKey))
                    continue;

                // Mark this entity as processed
                processedEntities.Add(entityKey);

                try
                {
                    // Create a complete resource object for the included entity
                    var resourceObject = new ResourceObject
                    {
                        Id = itemId,
                        Type = GetResourceType(itemType),
                        Attributes = [],
                    };

                    // Add all attributes
                    foreach (var prop in GetAttributeProperties(itemType))
                    {
                        var propValue = prop.GetValue(item);
                        if (propValue != null)
                        {
                            resourceObject.Attributes[prop.Name.ToCamelCase()] = propValue;
                        }
                    }

                    included.Add(resourceObject);

                    // Process nested includes if there are more path parts
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
            // Single relationship
            var relType = value.GetType();
            var idProperty = GetIdProperty(relType);
            if (idProperty == null)
                return;

            var relId = idProperty.GetValue(value)?.ToString();
            if (relId == null)
                return;

            // Create a unique identifier for this entity
            var entityKey = $"{relType.Name}:{relId}";

            // Skip if we've already processed this entity
            if (processedEntities.Contains(entityKey))
                return;

            // Mark this entity as processed
            processedEntities.Add(entityKey);

            try
            {
                // Create a complete resource object for the included entity
                var resourceObject = new ResourceObject
                {
                    Id = relId,
                    Type = GetResourceType(relType),
                    Attributes = [],
                };

                // Add all attributes
                foreach (var prop in GetAttributeProperties(relType))
                {
                    var propValue = prop.GetValue(value);
                    if (propValue != null)
                    {
                        resourceObject.Attributes[prop.Name.ToCamelCase()] = propValue;
                    }
                }

                included.Add(resourceObject);

                // Process nested includes if there are more path parts
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

    // Add this helper method to consistently generate resource types
    private static string GetResourceType(Type type)
    {
        // Convert from PascalCase to kebab-case (e.g., "TodoItem" -> "todo-item")
        string name = type.Name;

        // Just convert to camelCase for now as it's more consistent with your existing code
        return name.ToCamelCase();
    }
}

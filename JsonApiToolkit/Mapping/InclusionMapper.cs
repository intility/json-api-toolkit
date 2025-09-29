using System.Collections;
using System.Reflection;
using JsonApiToolkit.Extensions;
using JsonApiToolkit.Models.Resources;
using Microsoft.Extensions.Logging;

namespace JsonApiToolkit.Mapping;

/// <summary>
/// Handles the mapping of included (related) resources in JSON:API responses.
/// </summary>
/// <remarks>
/// Responsible for processing the "include" query parameter and adding the specified related resources
/// to the "included" section of a JSON:API response. Handles both to-one and to-many relationships,
/// and supports nested inclusion paths (e.g., "author.comments").
/// </remarks>
public static class InclusionMapper
{
    /// <summary>
    /// Processes specified include paths and adds related resources to the included collection.
    /// </summary>
    /// <param name="entityOrCollection">The primary entity or collection of entities to process</param>
    /// <param name="includePaths">List of relationship paths to include (e.g., ["author", "comments.user"])</param>
    /// <param name="included">Collection to add included resources to</param>
    /// <param name="logger">Optional logger for debugging and tracing</param>
    /// <param name="processedEntities">Optional set tracking already processed entities to prevent duplicates</param>
    /// <remarks>
    /// <para>
    /// The entry point for inclusion processing. Starts from the primary entity and traverses all specified
    /// relationship paths, collecting related entities for inclusion in the response.
    /// </para>
    /// <para>
    /// Uses a HashSet to track processed entities and prevent duplicate inclusions.
    /// </para>
    /// </remarks>
    public static void AddIncludedResources(
        object entityOrCollection,
        List<string> includePaths,
        List<ResourceObject> included,
        ILogger? logger = null,
        HashSet<string>? processedEntities = null
    )
    {
        if (entityOrCollection == null || includePaths == null || includePaths.Count == 0)
        {
            logger?.LogDebug(
                "AddIncludedResources: Skipping - null entity/paths or empty include paths"
            );
            return;
        }

        processedEntities ??= [];

        logger?.LogDebug(
            "AddIncludedResources: Processing {PathCount} include paths for entity/collection of type {EntityType}",
            includePaths.Count,
            entityOrCollection.GetType().Name
        );

        // Group include paths by their first segment
        IEnumerable<IGrouping<string, string?>> grouped = includePaths
            .Select(path => path.Split('.', 2))
            .GroupBy(parts => parts[0], parts => parts.Length > 1 ? parts[1] : null);

        foreach (IGrouping<string, string?> group in grouped)
        {
            string relationshipName = group.Key;
            var nestedPaths = group.Where(x => x != null).Select(x => x!).ToList();

            logger?.LogDebug(
                "Processing relationship '{RelationshipName}' with {NestedPathCount} nested paths",
                relationshipName,
                nestedPaths.Count
            );

            if (entityOrCollection is IEnumerable enumerable and not string)
            {
                int entityCount = 0;
                foreach (object? entity in enumerable)
                {
                    entityCount++;
                    logger?.LogDebug(
                        "Processing entity {EntityIndex} for relationship '{RelationshipName}'",
                        entityCount,
                        relationshipName
                    );

                    AddIncludedForEntity(
                        entity,
                        relationshipName,
                        nestedPaths,
                        included,
                        processedEntities,
                        logger
                    );
                }
                logger?.LogDebug(
                    "Processed {EntityCount} entities for relationship '{RelationshipName}'",
                    entityCount,
                    relationshipName
                );
            }
            else
            {
                logger?.LogDebug(
                    "Processing single entity for relationship '{RelationshipName}'",
                    relationshipName
                );
                AddIncludedForEntity(
                    entityOrCollection,
                    relationshipName,
                    nestedPaths,
                    included,
                    processedEntities,
                    logger
                );
            }
        }
    }

    private static void AddIncludedForEntity(
        object entity,
        string relationshipName,
        List<string> nestedPaths,
        List<ResourceObject> included,
        HashSet<string> processedEntities,
        ILogger? logger = null
    )
    {
        if (entity == null)
        {
            logger?.LogDebug("AddIncludedForEntity: Skipping null entity");
            return;
        }

        Type type = entity.GetType();
        logger?.LogDebug(
            "AddIncludedForEntity: Looking for relationship '{RelationshipName}' on entity type '{EntityType}'",
            relationshipName,
            type.Name
        );

        PropertyInfo? relProp = type.GetProperties()
            .FirstOrDefault(p =>
                string.Equals(p.Name, relationshipName, StringComparison.OrdinalIgnoreCase)
            );
        if (relProp == null)
        {
            logger?.LogWarning(
                "AddIncludedForEntity: Relationship '{RelationshipName}' not found on entity type '{EntityType}'. Available properties: {PropertyNames}",
                relationshipName,
                type.Name,
                string.Join(", ", type.GetProperties().Select(p => p.Name))
            );
            return;
        }

        logger?.LogDebug(
            "AddIncludedForEntity: Found relationship property '{PropertyName}' of type '{PropertyType}'",
            relProp.Name,
            relProp.PropertyType.Name
        );

        object? relValue = relProp.GetValue(entity);
        if (relValue == null)
        {
            logger?.LogDebug(
                "AddIncludedForEntity: Relationship '{RelationshipName}' has null value on entity",
                relationshipName
            );
            return;
        }

        if (relValue is IEnumerable relCollection && relValue.GetType() != typeof(string))
        {
            int collectionCount = 0;
            foreach (object? relEntity in relCollection)
            {
                collectionCount++;
            }

            logger?.LogDebug(
                "AddIncludedForEntity: Processing to-many relationship '{RelationshipName}' with {CollectionCount} items",
                relationshipName,
                collectionCount
            );

            foreach (object? relEntity in relCollection)
            {
                AddSingleIncluded(relEntity, included, processedEntities, nestedPaths, logger);
            }
        }
        else
        {
            logger?.LogDebug(
                "AddIncludedForEntity: Processing to-one relationship '{RelationshipName}' with value type '{ValueType}'",
                relationshipName,
                relValue.GetType().Name
            );
            AddSingleIncluded(relValue, included, processedEntities, nestedPaths, logger);
        }
    }

    private static void AddSingleIncluded(
        object relEntity,
        List<ResourceObject> included,
        HashSet<string> processedEntities,
        List<string> nestedPaths,
        ILogger? logger = null
    )
    {
        if (relEntity == null)
        {
            logger?.LogDebug("AddSingleIncluded: Skipping null related entity");
            return;
        }

        Type type = relEntity.GetType();
        PropertyInfo? idProp = EntityMapper.GetIdProperty(type);
        if (idProp == null)
        {
            logger?.LogWarning(
                "AddSingleIncluded: No ID property found on entity type '{EntityType}', cannot include",
                type.Name
            );
            return;
        }

        object? idValue = idProp.GetValue(relEntity);
        if (idValue == null)
        {
            logger?.LogDebug(
                "AddSingleIncluded: Entity of type '{EntityType}' has null ID, skipping",
                type.Name
            );
            return; // <-- Defensive: skip if no ID
        }

        string id = idValue.ToString()!;
        string resourceType = EntityMapper.GetResourceType(type);
        string key = $"{resourceType}:{id}";

        logger?.LogDebug(
            "AddSingleIncluded: Processing entity '{ResourceType}' with ID '{EntityId}' (key: '{Key}')",
            resourceType,
            id,
            key
        );

        if (!processedEntities.Add(key))
        {
            logger?.LogDebug(
                "AddSingleIncluded: Entity '{Key}' already processed, skipping duplicate",
                key
            );
            return; // Already processed
        }

        logger?.LogDebug(
            "AddSingleIncluded: Mapping entity '{Key}' to ResourceObject with {NestedPathCount} nested paths",
            key,
            nestedPaths?.Count ?? 0
        );

        // Map the related entity to a ResourceObject (attributes + relationships)
        var resourceObject = JsonApiMapper.ToResourceObject(relEntity, resourceType, nestedPaths);
        included.Add(resourceObject);

        logger?.LogDebug(
            "AddSingleIncluded: Successfully added entity '{Key}' to included resources (total included: {IncludedCount})",
            key,
            included.Count
        );

        // Recursively process nested include paths
        if (nestedPaths?.Count > 0)
        {
            logger?.LogDebug(
                "AddSingleIncluded: Recursively processing {NestedPathCount} nested paths for entity '{Key}'",
                nestedPaths.Count,
                key
            );
            AddIncludedResources(relEntity, nestedPaths, included, logger, processedEntities);
        }
    }

    /// <summary>
    /// Recursively processes a single include path to extract related resources.
    /// </summary>
    /// <typeparam name="T">The entity type at the current recursion level</typeparam>
    /// <param name="entity">The entity at the current recursion level</param>
    /// <param name="pathParts">Array of relationship names forming the include path</param>
    /// <param name="depth">Current depth in the path</param>
    /// <param name="included">Collection to add included resources to</param>
    /// <param name="processedEntities">Set tracking already processed entities to prevent duplicates</param>
    /// <remarks>
    /// <para>
    /// Internal recursive method that handles both to-one and to-many relationships:
    /// <list type="bullet">
    /// <item>
    /// <description>For to-many relationships, iterates through the collection and processes each item</description>
    /// </item>
    /// <item>
    /// <description>For to-one relationships, processes the single related entity</description>
    /// </item>
    /// </list>
    /// </para>
    /// <para>
    /// For each related entity:
    /// <list type="number">
    /// <item>
    /// <description>Extracts its ID and type to form a unique key</description>
    /// </item>
    /// <item>
    /// <description>Checks if it has already been processed (to avoid duplicates)</description>
    /// </item>
    /// <item>
    /// <description>Adds it to the included collection with all its attributes</description>
    /// </item>
    /// <item>
    /// <description>Recursively processes the next part of the include path if needed</description>
    /// </item>
    /// </list>
    /// </para>
    /// <para>
    /// Handles nested includes (e.g., "author.comments") by recursively calling itself with an incremented depth.
    /// </para>
    /// </remarks>
    public static void AddIncludedResourcesRecursive<T>(
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
                PropertyInfo? idProperty = EntityMapper.GetIdProperty(itemType);
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
                        Type = EntityMapper.GetResourceType(itemType),
                        Attributes = [],
                    };

                    foreach (PropertyInfo prop in EntityMapper.GetAttributeProperties(itemType))
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
            PropertyInfo? idProperty = EntityMapper.GetIdProperty(relType);
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
                    Type = EntityMapper.GetResourceType(relType),
                    Attributes = [],
                };

                foreach (PropertyInfo prop in EntityMapper.GetAttributeProperties(relType))
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
}

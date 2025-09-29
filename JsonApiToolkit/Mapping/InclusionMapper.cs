using System.Collections;
using System.Reflection;
using JsonApiToolkit.Extensions;
using JsonApiToolkit.Models.Resources;
using Microsoft.Extensions.Logging;

namespace JsonApiToolkit.Mapping;

/// <summary>
/// Maps included (related) resources for JSON:API responses.
/// Handles to-one/to-many relationships and nested paths (e.g., "author.comments").
/// </summary>
public static class InclusionMapper
{
    /// <summary>
    /// Processes include paths and adds related resources to included collection.
    /// Uses HashSet to track processed entities and prevent duplicates.
    /// </summary>
    public static void AddIncludedResources(
        object entityOrCollection,
        List<string> includePaths,
        List<ResourceObject> included,
        ILogger? logger = null,
        HashSet<string>? processedEntities = null
    )
    {
        if (entityOrCollection == null || includePaths == null || includePaths.Count == 0)
            return;

        processedEntities ??= [];

        IEnumerable<IGrouping<string, string?>> grouped = includePaths
            .Select(path => path.Split('.', 2))
            .GroupBy(parts => parts[0], parts => parts.Length > 1 ? parts[1] : null);

        foreach (IGrouping<string, string?> group in grouped)
        {
            string relationshipName = group.Key;
            var nestedPaths = group.Where(x => x != null).Select(x => x!).ToList();

            if (entityOrCollection is IEnumerable enumerable and not string)
            {
                foreach (object? entity in enumerable)
                {
                    AddIncludedForEntity(
                        entity,
                        relationshipName,
                        nestedPaths,
                        included,
                        processedEntities,
                        logger
                    );
                }
            }
            else
            {
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
            return;

        Type type = entity.GetType();

        PropertyInfo? relProp = type.GetProperties()
            .FirstOrDefault(p =>
                string.Equals(p.Name, relationshipName, StringComparison.OrdinalIgnoreCase)
            );
        if (relProp == null)
        {
            logger?.LogWarning(
                "Relationship '{Relationship}' not found on {Type}",
                relationshipName,
                type.Name
            );
            return;
        }

        object? relValue = relProp.GetValue(entity);
        if (relValue == null)
            return;

        if (relValue is IEnumerable relCollection && relValue.GetType() != typeof(string))
        {
            foreach (object? relEntity in relCollection)
            {
                AddSingleIncluded(relEntity, included, processedEntities, nestedPaths, logger);
            }
        }
        else
        {
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
            return;

        Type type = relEntity.GetType();
        PropertyInfo? idProp = EntityMapper.GetIdProperty(type);
        if (idProp == null)
        {
            logger?.LogWarning("No ID property on {Type}", type.Name);
            return;
        }

        object? idValue = idProp.GetValue(relEntity);
        if (idValue == null)
            return;

        string id = idValue.ToString()!;
        string resourceType = EntityMapper.GetResourceType(type);
        string key = $"{resourceType}:{id}";

        if (!processedEntities.Add(key))
            return; // Already processed

        var resourceObject = JsonApiMapper.ToResourceObject(relEntity, resourceType, nestedPaths);
        included.Add(resourceObject);

        if (nestedPaths?.Count > 0)
        {
            AddIncludedResources(relEntity, nestedPaths, included, logger, processedEntities);
        }
    }

    /// <summary>
    /// Recursively processes include path to extract related resources.
    /// Handles to-one/to-many relationships and nested paths with duplicate prevention.
    /// </summary>
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

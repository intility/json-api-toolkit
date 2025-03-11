using System.Collections;
using System.Reflection;
using JsonApiToolkit.Extensions;
using JsonApiToolkit.Models.Resources;

namespace JsonApiToolkit.Mapping;

/// <summary>
/// Helper class for mapping included resources.
/// </summary>
public static class InclusionMapper
{
    /// <summary>
    /// Adds included resources to the list of included resources.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="entity">The entity to add included resources for.</param>
    /// <param name="includePaths">The include paths to add.</param>
    /// <param name="included">The list of included resources.</param>
    /// <param name="processedEntities">The set of processed entities.</param>
    public static void AddIncludedResources<T>(
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

    /// <summary>
    /// Adds included resources to the list of included resources.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="entity">The entity to add included resources for.</param>
    /// <param name="pathParts">The parts of the include path.</param>
    /// <param name="depth">The current depth in the path.</param>
    /// <param name="included">The list of included resources.</param>
    /// <param name="processedEntities">The set of processed entities.</param>
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

                    // Add all attributes
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

using System.Collections;
using System.Reflection;
using JsonApiToolkit.Extensions;
using JsonApiToolkit.Models.Documents;
using JsonApiToolkit.Models.Metadata;
using JsonApiToolkit.Models.Resources;
using Microsoft.Extensions.Logging;

namespace JsonApiToolkit.Mapping;

/// <summary>
/// Core mapper for converting entities and entity collections to JSON:API resource structures.
/// </summary>
/// <remarks>
/// <para>
/// This static class provides the primary mapping functionality between application entities and
/// JSON:API document structures. It handles mapping of attributes, relationships, included resources,
/// pagination, and links.
/// </para>
/// <para>
/// All JSON:API document creation should use these methods to ensure consistency and compliance
/// with the JSON:API specification.
/// </para>
/// </remarks>
public static class JsonApiMapper
{
    /// <summary>
    /// Maps an entity to a JSON:API resource object with attributes and relationships.
    /// </summary>
    /// <param name="entity">The entity to map</param>
    /// <param name="resourceType">The JSON:API resource type identifier</param>
    /// <param name="includedRelationships">Optional list of relationships to include in the resource object</param>
    /// <param name="logger">Optional logger for debugging and tracing</param>
    /// <returns>A fully populated ResourceObject representing the entity</returns>
    /// <remarks>
    /// Maps the entity to a JSON:API resource object by:
    /// <list type="number">
    /// <item>
    /// <description>Extracting the entity's ID</description>
    /// </item>
    /// <item>
    /// <description>Mapping primitive properties to attributes</description>
    /// </item>
    /// <item>
    /// <description>Mapping related entities to relationships (both to-one and to-many)</description>
    /// </item>
    /// </list>
    /// <para>
    /// Only maps relationships that are explicitly included in the includedRelationships parameter.
    /// Performs smart mapping of different relationship types (to-one vs to-many).
    /// </para>
    /// <para>
    /// This is the core mapping method used by all other document creation methods.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if the entity parameter is null</exception>
    /// <exception cref="InvalidOperationException">Thrown if the entity's ID cannot be determined</exception>
    public static ResourceObject ToResourceObject(
        object entity,
        string resourceType,
        List<string>? includedRelationships = null,
        ILogger? logger = null
    )
    {
        ArgumentNullException.ThrowIfNull(entity);

        Type type = entity.GetType();

        logger?.LogDebug(
            "Mapping entity of type {EntityType} to resource object with type '{ResourceType}' and {IncludeCount} included relationships",
            type.Name,
            resourceType,
            includedRelationships?.Count ?? 0
        );

        PropertyInfo? idProperty = EntityMapper.GetIdProperty(type);
        var idValue =
            (idProperty?.GetValue(entity))
            ?? throw new InvalidOperationException("Entity Id cannot be null");
        string id = idValue.ToString()!;

        var resourceObject = new ResourceObject
        {
            Id = id,
            Type = resourceType,
            Attributes = [],
        };

        foreach (PropertyInfo prop in EntityMapper.GetAttributeProperties(type))
        {
            object? value = prop.GetValue(entity);
            if (value != null)
            {
                resourceObject.Attributes![prop.Name.ToCamelCase()] = value;
            }
        }

        if (includedRelationships?.Count > 0)
        {
            List<PropertyInfo> relationshipProperties = EntityMapper.GetRelationshipProperties(
                type
            );
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
                                PropertyInfo? itemIdProp = EntityMapper.GetIdProperty(itemType);
                                if (itemIdProp == null)
                                    continue;

                                string? itemId = itemIdProp.GetValue(item)?.ToString();
                                if (itemId == null)
                                    continue;

                                relData.Add(
                                    new ResourceIdentifier
                                    {
                                        Id = itemId,
                                        Type = EntityMapper.GetResourceType(itemType),
                                    }
                                );
                            }
                            relationship.Data = relData;
                        }
                        else
                        {
                            Type relItemType = relValue.GetType();
                            PropertyInfo? relItemIdProp = EntityMapper.GetIdProperty(relItemType);
                            if (relItemIdProp != null)
                            {
                                string? relItemId = relItemIdProp.GetValue(relValue)?.ToString();
                                if (relItemId != null)
                                {
                                    relationship.Data = new ResourceIdentifier
                                    {
                                        Id = relItemId,
                                        Type = EntityMapper.GetResourceType(relItemType),
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
    /// Maps an entity to a complete JSON:API document with support for included resources.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="entity">The entity to map</param>
    /// <param name="resourceType">The JSON:API resource type identifier</param>
    /// <param name="selfLink">The self link URL for the resource</param>
    /// <param name="includedRelationships">Optional list of relationship paths to include</param>
    /// <param name="logger">Optional logger for debugging and tracing</param>
    /// <returns>A fully populated JSON:API document representing the entity</returns>
    /// <remarks>
    /// <para>
    /// Creates a complete JSON:API document with:
    /// <list type="number">
    /// <item>
    /// <description>The primary resource object (with attributes and relationships)</description>
    /// </item>
    /// <item>
    /// <description>Self links for the document and resource</description>
    /// </item>
    /// <item>
    /// <description>Related resources in the included array (if includedRelationships is specified)</description>
    /// </item>
    /// </list>
    /// Used for single-resource responses (GET on a specific resource, POST creating a resource, etc.).
    /// </para>
    /// </remarks>
    public static JsonApiDocument<ResourceObject> ToDocument<T>(
        T entity,
        string resourceType,
        string selfLink,
        List<string>? includedRelationships = null,
        ILogger? logger = null
    )
        where T : class
    {
        logger?.LogDebug(
            "Creating JSON:API document for entity of type {EntityType} with resource type '{ResourceType}'",
            typeof(T).Name,
            resourceType
        );

        ResourceObject resource = ToResourceObject(
            entity,
            resourceType,
            includedRelationships,
            logger
        );
        resource.Links = new Links { Self = selfLink };

        var document = new JsonApiDocument<ResourceObject>
        {
            Data = resource,
            Links = new Links { Self = selfLink },
        };

        if (includedRelationships?.Count > 0)
        {
            var included = new List<ResourceObject>();
            InclusionMapper.AddIncludedResources(entity, includedRelationships, included);
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
    /// <param name="logger">Optional logger for debugging and tracing</param>
    /// <returns>The JSON:API collection document.</returns>
    public static JsonApiCollectionDocument<ResourceObject> ToCollectionDocument<T>(
        IEnumerable<T> entities,
        string resourceType,
        string selfLink,
        PaginationMeta? paginationMeta = null,
        List<string>? includedRelationships = null,
        ILogger? logger = null
    )
        where T : class
    {
        logger?.LogDebug(
            "Creating JSON:API collection document for entities of type {EntityType} with resource type '{ResourceType}'",
            typeof(T).Name,
            resourceType
        );

        string baseUrl = selfLink.Split('?')[0];

        var resources = entities
            .Select(e =>
            {
                ResourceObject resource = ToResourceObject(
                    e,
                    resourceType,
                    includedRelationships,
                    logger
                );
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
            InclusionMapper.AddIncludedResources(entities, includedRelationships, included);
            if (included.Count > 0)
            {
                document.Included = included;
            }
        }

        return document;
    }
}

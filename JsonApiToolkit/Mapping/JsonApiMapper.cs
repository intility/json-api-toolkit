using System.Collections;
using System.Reflection;
using JsonApiToolkit.Extensions;
using JsonApiToolkit.Models.Documents;
using JsonApiToolkit.Models.Metadata;
using JsonApiToolkit.Models.Resources;

namespace JsonApiToolkit.Mapping;

/// <summary>
/// A JSON:API mapper. Maps entities to JSON:API resources.
/// </summary>
public static class JsonApiMapper
{
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

        PropertyInfo? idProperty = EntityMapper.GetIdProperty(type);
        string id =
            idProperty?.GetValue(entity)?.ToString()
            ?? throw new InvalidOperationException("Entity Id cannot be null");

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
            // Map relationships if any
            List<PropertyInfo> relationshipProperties =
                RelationshipMapper.GetRelationshipProperties(type);
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
                InclusionMapper.AddIncludedResources(entity, includedRelationships, included);
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
}

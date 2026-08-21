using System.Collections;
using System.Reflection;
using JsonApiToolkit.Extensions;
using JsonApiToolkit.Models.Documents;
using JsonApiToolkit.Models.Metadata;
using JsonApiToolkit.Models.Resources;
using Microsoft.Extensions.Logging;

namespace JsonApiToolkit.Mapping;

/// <summary>
/// Maps entities to JSON:API resource structures.
/// Handles attributes, relationships, included resources, pagination, and links.
/// </summary>
public static class JsonApiMapper
{
    /// <summary>
    /// Maps entity to JSON:API resource object.
    /// Extracts ID, maps properties to attributes, and maps relationships.
    /// </summary>
    public static ResourceObject ToResourceObject(
        object entity,
        string resourceType,
        List<string>? includedRelationships = null,
        ILogger? logger = null,
        Dictionary<string, List<string>>? fields = null
    )
    {
        ArgumentNullException.ThrowIfNull(entity);

        Type type = entity.GetType();

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

        List<string>? allowedFields = null;
        fields?.TryGetValue(resourceType, out allowedFields);

        foreach (PropertyInfo prop in EntityMapper.GetAttributeProperties(type))
        {
            string camelName = EntityMapper.GetAttributeName(prop);

            if (
                allowedFields != null
                && !allowedFields.Contains(camelName, StringComparer.OrdinalIgnoreCase)
            )
                continue;

            object? value = prop.GetValue(entity);
            if (value != null)
            {
                resourceObject.Attributes![camelName] = value;
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
    /// <param name="fields">Optional sparse fieldsets per resource type</param>
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
        ILogger? logger = null,
        Dictionary<string, List<string>>? fields = null
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
            logger,
            fields
        );
        resource.Links = new Links { Self = selfLink };

        var document = new JsonApiDocument<ResourceObject>
        {
            Data = resource,
            Links = new Links { Self = selfLink },
        };

        if (includedRelationships?.Count > 0)
        {
            logger?.LogDebug(
                "Processing includes for single entity: {IncludeCount} relationships requested",
                includedRelationships.Count
            );

            var included = new List<ResourceObject>();
            InclusionMapper.AddIncludedResources(
                entity,
                includedRelationships,
                included,
                logger,
                fields: fields
            );

            logger?.LogDebug(
                "Include processing completed for single entity: {IncludedCount} resources added to included section",
                included.Count
            );

            if (included.Count > 0)
            {
                document.Included = included;
            }
            else
            {
                logger?.LogWarning(
                    "No included resources were processed for single entity despite {IncludeCount} relationships being requested. Check if relationships are properly loaded",
                    includedRelationships.Count
                );
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
    /// <param name="fields">Optional sparse fieldsets per resource type</param>
    /// <param name="preserveQueryInLinks">When true, pagination links keep the full query string with only the page parameters replaced</param>
    /// <returns>The JSON:API collection document.</returns>
    public static JsonApiCollectionDocument<ResourceObject> ToCollectionDocument<T>(
        IEnumerable<T> entities,
        string resourceType,
        string selfLink,
        PaginationMeta? paginationMeta = null,
        List<string>? includedRelationships = null,
        ILogger? logger = null,
        Dictionary<string, List<string>>? fields = null,
        bool preserveQueryInLinks = false
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
                    logger,
                    fields
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

            string PageLink(int pageNumber) =>
                BuildPaginationLink(baseUrl, selfLink, pageNumber, pageSize, preserveQueryInLinks);

            document.Links.First = PageLink(1);
            document.Links.Last = PageLink(paginationMeta.TotalPages);

            if (paginationMeta.CurrentPage > 1)
            {
                document.Links.Prev = PageLink(paginationMeta.CurrentPage - 1);
            }

            if (paginationMeta.CurrentPage < paginationMeta.TotalPages)
            {
                document.Links.Next = PageLink(paginationMeta.CurrentPage + 1);
            }
        }

        if (includedRelationships?.Count > 0)
        {
            logger?.LogDebug(
                "Processing includes for collection: {IncludeCount} relationships requested for {EntityCount} entities",
                includedRelationships.Count,
                resources.Count
            );

            var included = new List<ResourceObject>();
            InclusionMapper.AddIncludedResources(
                entities,
                includedRelationships,
                included,
                logger,
                fields: fields
            );

            logger?.LogDebug(
                "Include processing completed: {IncludedCount} resources added to included section",
                included.Count
            );

            if (included.Count > 0)
            {
                document.Included = included;
            }
            else
            {
                logger?.LogWarning(
                    "No included resources were processed despite {IncludeCount} relationships being requested. Check if relationships are properly loaded on entities",
                    includedRelationships.Count
                );
            }
        }

        return document;
    }

    /// <summary>
    /// Builds a pagination link. Default: bare path + page params only (legacy).
    /// With <paramref name="preserveQuery"/>: the original query string with only
    /// the page parameters replaced, so filters/sort/include/fields survive.
    /// </summary>
    private static string BuildPaginationLink(
        string baseUrl,
        string selfLink,
        int pageNumber,
        int pageSize,
        bool preserveQuery
    )
    {
        if (!preserveQuery)
            return $"{baseUrl}?page[number]={pageNumber}&page[size]={pageSize}";

        int queryStart = selfLink.IndexOf('?');
        string query = queryStart >= 0 ? selfLink[queryStart..] : string.Empty;

        var builder = new Microsoft.AspNetCore.Http.Extensions.QueryBuilder();
        foreach (var kvp in Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(query))
        {
            if (
                kvp.Key.Equals("page[number]", StringComparison.OrdinalIgnoreCase)
                || kvp.Key.Equals("page[size]", StringComparison.OrdinalIgnoreCase)
            )
                continue;

            foreach (string? value in kvp.Value)
                builder.Add(kvp.Key, value ?? string.Empty);
        }

        builder.Add("page[number]", pageNumber.ToString());
        builder.Add("page[size]", pageSize.ToString());

        return baseUrl + builder.ToQueryString().Value;
    }
}

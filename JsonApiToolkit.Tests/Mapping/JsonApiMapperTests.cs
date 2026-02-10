using System.Text.Json.Serialization;
using JsonApiToolkit.Mapping;
using JsonApiToolkit.Models.Documents;
using JsonApiToolkit.Models.Metadata;
using JsonApiToolkit.Models.Resources;
using JsonApiToolkit.Tests.Models;

namespace JsonApiToolkit.Tests.Mapping;

public class JsonApiMapperTests
{
    [Fact]
    public void ToResourceObject_MapsEntityCorrectly()
    {
        var entity = new TestEntity
        {
            Id = 1,
            Name = "Test Entity",
            Description = "Test Description",
            CreatedAt = new DateTime(2023, 1, 1),
            IsActive = true,
        };

        var resourceObject = JsonApiMapper.ToResourceObject(entity, "testEntities");

        Assert.Equal("1", resourceObject.Id);
        Assert.Equal("testEntities", resourceObject.Type);
        Assert.NotNull(resourceObject.Attributes);
        Assert.Equal("Test Entity", resourceObject.Attributes["name"]);
        Assert.Equal("Test Description", resourceObject.Attributes["description"]);
        Assert.Equal(new DateTime(2023, 1, 1), resourceObject.Attributes["createdAt"]);
        Assert.Equal(true, resourceObject.Attributes["isActive"]);
    }

    [Fact]
    public void ToResourceObject_IncludesForeignKeyIdsInAttributes()
    {
        var entity = new TestEntity
        {
            Id = 1,
            Name = "Test Entity",
            RelatedEntityId = 42,
        };

        var resourceObject = JsonApiMapper.ToResourceObject(entity, "testEntities");

        // Foreign key IDs should be included in attributes
        Assert.NotNull(resourceObject.Attributes);
        Assert.True(resourceObject.Attributes.ContainsKey("relatedEntityId"));
        Assert.Equal(42, resourceObject.Attributes["relatedEntityId"]);
    }

    [Fact]
    public void ToResourceObject_WithRelationships_MapsRelationshipsCorrectly()
    {
        var relatedEntity = new TestRelatedEntity { Id = 2, Name = "Related Entity" };

        var entity = new TestEntity
        {
            Id = 1,
            Name = "Test Entity",
            RelatedEntity = relatedEntity,
            RelatedEntityId = 2,
        };

        var resourceObject = JsonApiMapper.ToResourceObject(
            entity,
            "testEntities",
            ["RelatedEntity"]
        );

        Assert.NotNull(resourceObject.Relationships);
        Assert.True(resourceObject.Relationships.ContainsKey("relatedEntity"));

        Relationship relationship = resourceObject.Relationships["relatedEntity"];
        ResourceIdentifier resourceIdentifier = Assert.IsType<ResourceIdentifier>(
            relationship.Data
        );
        Assert.Equal("2", resourceIdentifier.Id);
        Assert.Equal("testRelatedEntity", resourceIdentifier.Type);
    }

    [Fact]
    public void ToResourceObject_WithCollectionRelationship_MapsCollectionCorrectly()
    {
        var entity = new TestEntity
        {
            Id = 1,
            Name = "Test Entity",
            Children = [new() { Id = 10, Name = "Child 1" }, new() { Id = 11, Name = "Child 2" }],
        };

        var resourceObject = JsonApiMapper.ToResourceObject(entity, "testEntities", ["Children"]);

        Assert.NotNull(resourceObject.Relationships);
        Assert.True(resourceObject.Relationships.ContainsKey("children"));

        Relationship relationship = resourceObject.Relationships["children"];
        IEnumerable<ResourceIdentifier> identifiers = Assert.IsAssignableFrom<
            IEnumerable<ResourceIdentifier>
        >(relationship.Data);
        Assert.Equal(2, identifiers.Count());
        Assert.Contains(identifiers, id => id.Id == "10");
        Assert.Contains(identifiers, id => id.Id == "11");
        Assert.All(identifiers, id => Assert.Equal("testChildEntity", id.Type));
    }

    [Fact]
    public void ToDocument_IncludesRelatedResources()
    {
        var relatedEntity = new TestRelatedEntity { Id = 2, Name = "Related Entity" };

        var entity = new TestEntity
        {
            Id = 1,
            Name = "Test Entity",
            RelatedEntity = relatedEntity,
            RelatedEntityId = 2,
        };

        JsonApiToolkit.Models.Documents.JsonApiDocument<ResourceObject> document =
            JsonApiMapper.ToDocument(
                entity,
                "testEntities",
                "https://api.example.com/test-entities/1",
                ["RelatedEntity"]
            );

        Assert.NotNull(document.Included);
        Assert.Single(document.Included);

        ResourceObject includedResource = document.Included.First();
        Assert.Equal("2", includedResource.Id);
        Assert.Equal("testRelatedEntity", includedResource.Type);
        Assert.Equal("Related Entity", includedResource.Attributes?["name"]);
    }

    [Fact]
    public void ToCollectionDocument_WithPagination_IncludesCorrectLinks()
    {
        var entities = new List<TestEntity>
        {
            new() { Id = 1, Name = "Entity 1" },
            new() { Id = 2, Name = "Entity 2" },
        };

        var paginationMeta = new PaginationMeta
        {
            TotalResources = 10,
            TotalPages = 5,
            CurrentPage = 1,
            PageSize = 2,
        };

        JsonApiCollectionDocument<ResourceObject> document = JsonApiMapper.ToCollectionDocument(
            entities,
            "testEntities",
            "https://api.example.com/test-entities",
            paginationMeta
        );

        Assert.NotNull(document.Links);
        Assert.Equal("https://api.example.com/test-entities", document.Links.Self);
        Assert.Equal(
            "https://api.example.com/test-entities?page[number]=1&page[size]=2",
            document.Links.First
        );
        Assert.Equal(
            "https://api.example.com/test-entities?page[number]=5&page[size]=2",
            document.Links.Last
        );
        Assert.Equal(
            "https://api.example.com/test-entities?page[number]=2&page[size]=2",
            document.Links.Next
        );
        Assert.Null(document.Links.Prev);

        Assert.NotNull(document.Meta);
        Assert.True(document.Meta.ContainsKey("pagination"));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Sparse Fieldsets Tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ToResourceObject_WithSparseFieldset_ReturnsOnlyRequestedAttributes()
    {
        var entity = new TestEntity
        {
            Id = 1,
            Name = "Test Entity",
            Description = "Test Description",
            CreatedAt = new DateTime(2023, 1, 1),
            IsActive = true,
        };

        var fields = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["testEntities"] = ["name", "isActive"],
        };

        var resourceObject = JsonApiMapper.ToResourceObject(entity, "testEntities", fields: fields);

        Assert.NotNull(resourceObject.Attributes);
        Assert.Equal(2, resourceObject.Attributes.Count);
        Assert.True(resourceObject.Attributes.ContainsKey("name"));
        Assert.True(resourceObject.Attributes.ContainsKey("isActive"));
        Assert.False(resourceObject.Attributes.ContainsKey("description"));
        Assert.False(resourceObject.Attributes.ContainsKey("createdAt"));
    }

    [Fact]
    public void ToResourceObject_WithSparseFieldset_AlwaysIncludesIdAndType()
    {
        var entity = new TestEntity
        {
            Id = 1,
            Name = "Test Entity",
            Description = "Test Description",
        };

        var fields = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["testEntities"] = ["name"],
        };

        var resourceObject = JsonApiMapper.ToResourceObject(entity, "testEntities", fields: fields);

        Assert.Equal("1", resourceObject.Id);
        Assert.Equal("testEntities", resourceObject.Type);
        Assert.Single(resourceObject.Attributes!);
        Assert.True(resourceObject.Attributes!.ContainsKey("name"));
    }

    [Fact]
    public void ToResourceObject_WithNoFieldsetForType_ReturnsAllAttributes()
    {
        var entity = new TestEntity
        {
            Id = 1,
            Name = "Test Entity",
            Description = "Test Description",
            IsActive = true,
        };

        var fields = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["otherType"] = ["name"],
        };

        var resourceObject = JsonApiMapper.ToResourceObject(entity, "testEntities", fields: fields);

        Assert.NotNull(resourceObject.Attributes);
        Assert.True(resourceObject.Attributes.ContainsKey("name"));
        Assert.True(resourceObject.Attributes.ContainsKey("description"));
        Assert.True(resourceObject.Attributes.ContainsKey("isActive"));
    }

    [Fact]
    public void ToResourceObject_WithNullFields_ReturnsAllAttributes()
    {
        var entity = new TestEntity
        {
            Id = 1,
            Name = "Test Entity",
            Description = "Test Description",
            IsActive = true,
        };

        var resourceObject = JsonApiMapper.ToResourceObject(entity, "testEntities", fields: null);

        Assert.NotNull(resourceObject.Attributes);
        Assert.True(resourceObject.Attributes.ContainsKey("name"));
        Assert.True(resourceObject.Attributes.ContainsKey("description"));
        Assert.True(resourceObject.Attributes.ContainsKey("isActive"));
    }

    [Fact]
    public void ToResourceObject_WithSparseFieldset_RelationshipsNotFiltered()
    {
        var relatedEntity = new TestRelatedEntity { Id = 2, Name = "Related Entity" };

        var entity = new TestEntity
        {
            Id = 1,
            Name = "Test Entity",
            RelatedEntity = relatedEntity,
            RelatedEntityId = 2,
        };

        var fields = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["testEntities"] = ["name"],
        };

        var resourceObject = JsonApiMapper.ToResourceObject(
            entity,
            "testEntities",
            ["RelatedEntity"],
            fields: fields
        );

        // Attributes should be filtered
        Assert.Single(resourceObject.Attributes!);
        Assert.True(resourceObject.Attributes!.ContainsKey("name"));

        // Relationships should NOT be filtered
        Assert.NotNull(resourceObject.Relationships);
        Assert.True(resourceObject.Relationships.ContainsKey("relatedEntity"));
    }

    [Fact]
    public void ToDocument_WithSparseFieldset_FiltersIncludedResources()
    {
        var relatedEntity = new TestRelatedEntity { Id = 2, Name = "Related Entity" };

        var entity = new TestEntity
        {
            Id = 1,
            Name = "Test Entity",
            Description = "Desc",
            RelatedEntity = relatedEntity,
            RelatedEntityId = 2,
        };

        var fields = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["testEntities"] = ["name"],
            ["testRelatedEntity"] = ["name"],
        };

        JsonApiDocument<ResourceObject> document = JsonApiMapper.ToDocument(
            entity,
            "testEntities",
            "https://api.example.com/test/1",
            ["RelatedEntity"],
            fields: fields
        );

        // Primary resource should be filtered
        Assert.Single(document.Data!.Attributes!);
        Assert.True(document.Data!.Attributes!.ContainsKey("name"));

        // Included resource should also be filtered
        Assert.NotNull(document.Included);
        Assert.Single(document.Included);
        var included = document.Included.First();
        Assert.Equal("testRelatedEntity", included.Type);
        Assert.NotNull(included.Attributes);
        Assert.True(included.Attributes.ContainsKey("name"));
    }

    [Fact]
    public void ToResourceObject_ExcludesJsonIgnoreProperties()
    {
        var entity = new EntityWithIgnoredProperties
        {
            Id = 1,
            VisibleName = "Visible",
            SecretPassword = "should-not-appear",
            InternalData = "should-not-appear-either",
        };

        var resourceObject = JsonApiMapper.ToResourceObject(entity, "entities");

        Assert.NotNull(resourceObject.Attributes);
        Assert.True(resourceObject.Attributes.ContainsKey("visibleName"));
        Assert.Equal("Visible", resourceObject.Attributes["visibleName"]);
        Assert.False(resourceObject.Attributes.ContainsKey("secretPassword"));
        Assert.False(resourceObject.Attributes.ContainsKey("internalData"));
    }

    private class EntityWithIgnoredProperties
    {
        public int Id { get; set; }
        public string VisibleName { get; set; } = string.Empty;

        [JsonIgnore]
        public string SecretPassword { get; set; } = string.Empty;

        [JsonIgnore]
        public string InternalData { get; set; } = string.Empty;
    }
}

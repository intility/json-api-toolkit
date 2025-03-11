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
        // Arrange
        var entity = new TestEntity
        {
            Id = 1,
            Name = "Test Entity",
            Description = "Test Description",
            CreatedAt = new DateTime(2023, 1, 1),
            IsActive = true,
        };

        // Act
        var resourceObject = JsonApiMapper.ToResourceObject(entity, "testEntities");

        // Assert
        Assert.Equal("1", resourceObject.Id);
        Assert.Equal("testEntities", resourceObject.Type);
        Assert.NotNull(resourceObject.Attributes);
        Assert.Equal("Test Entity", resourceObject.Attributes["name"]);
        Assert.Equal("Test Description", resourceObject.Attributes["description"]);
        Assert.Equal(new DateTime(2023, 1, 1), resourceObject.Attributes["createdAt"]);
        Assert.Equal(true, resourceObject.Attributes["isActive"]);
    }

    [Fact]
    public void ToResourceObject_WithRelationships_MapsRelationshipsCorrectly()
    {
        // Arrange
        var relatedEntity = new TestRelatedEntity { Id = 2, Name = "Related Entity" };

        var entity = new TestEntity
        {
            Id = 1,
            Name = "Test Entity",
            RelatedEntity = relatedEntity,
            RelatedEntityId = 2,
        };

        // Act
        var resourceObject = JsonApiMapper.ToResourceObject(
            entity,
            "testEntities",
            ["RelatedEntity"]
        );

        // Assert
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
        // Arrange
        var entity = new TestEntity
        {
            Id = 1,
            Name = "Test Entity",
            Children = [new() { Id = 10, Name = "Child 1" }, new() { Id = 11, Name = "Child 2" }],
        };

        // Act
        var resourceObject = JsonApiMapper.ToResourceObject(entity, "testEntities", ["Children"]);

        // Assert
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
        // Arrange
        var relatedEntity = new TestRelatedEntity { Id = 2, Name = "Related Entity" };

        var entity = new TestEntity
        {
            Id = 1,
            Name = "Test Entity",
            RelatedEntity = relatedEntity,
            RelatedEntityId = 2,
        };

        // Act
        JsonApiToolkit.Models.Documents.JsonApiDocument<ResourceObject> document =
            JsonApiMapper.ToDocument(
                entity,
                "testEntities",
                "https://api.example.com/test-entities/1",
                ["RelatedEntity"]
            );

        // Assert
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
        // Arrange
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

        // Act
        JsonApiCollectionDocument<ResourceObject> document = JsonApiMapper.ToCollectionDocument(
            entities,
            "testEntities",
            "https://api.example.com/test-entities",
            paginationMeta
        );

        // Assert
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
}

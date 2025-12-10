using System.Text.Json.Serialization;
using JsonApiToolkit.Mapping;
using JsonApiToolkit.Tests.Models;

namespace JsonApiToolkit.Tests.Mapping;

public class EntityMapperTests
{
    [Fact]
    public void GetAttributeProperties_IncludesForeignKeyIds()
    {
        var attributeProperties = EntityMapper.GetAttributeProperties(typeof(TestEntity));
        var propertyNames = attributeProperties.Select(p => p.Name).ToList();

        // Should include foreign key ID
        Assert.Contains("RelatedEntityId", propertyNames);

        // Should NOT include the primary ID
        Assert.DoesNotContain("Id", propertyNames);

        // Should include other regular properties
        Assert.Contains("Name", propertyNames);
        Assert.Contains("Description", propertyNames);
        Assert.Contains("CreatedAt", propertyNames);
        Assert.Contains("IsActive", propertyNames);
        Assert.Contains("Status", propertyNames);
    }

    [Fact]
    public void GetAttributeProperties_ExcludesOnlyPrimaryId()
    {
        var attributeProperties = EntityMapper.GetAttributeProperties(typeof(TestChildEntity));
        var propertyNames = attributeProperties.Select(p => p.Name).ToList();

        // Should include foreign key ID
        Assert.Contains("TestEntityId", propertyNames);

        // Should NOT include the primary ID
        Assert.DoesNotContain("Id", propertyNames);

        // Should include other properties
        Assert.Contains("Name", propertyNames);
    }

    [Fact]
    public void GetRelationshipProperties_DoesNotIncludeForeignKeyIds()
    {
        var relationshipProperties = EntityMapper.GetRelationshipProperties(typeof(TestEntity));
        var propertyNames = relationshipProperties.Select(p => p.Name).ToList();

        // Should include actual relationships
        Assert.Contains("RelatedEntity", propertyNames);
        Assert.Contains("Children", propertyNames);

        // Should NOT include foreign key IDs
        Assert.DoesNotContain("RelatedEntityId", propertyNames);
    }

    [Fact]
    public void GetIdProperty_IdentifiesPrimaryId()
    {
        var idProperty = EntityMapper.GetIdProperty(typeof(TestEntity));

        Assert.NotNull(idProperty);
        Assert.Equal("Id", idProperty.Name);
    }

    [Fact]
    public void GetAttributeProperties_ExcludesJsonIgnoreProperties()
    {
        var attributeProperties = EntityMapper.GetAttributeProperties(typeof(EntityWithJsonIgnore));
        var propertyNames = attributeProperties.Select(p => p.Name).ToList();

        Assert.Contains("VisibleProperty", propertyNames);
        Assert.DoesNotContain("HiddenProperty", propertyNames);
    }

    [Fact]
    public void GetRelationshipProperties_ExcludesJsonIgnoreProperties()
    {
        var relationshipProperties = EntityMapper.GetRelationshipProperties(
            typeof(EntityWithJsonIgnore)
        );
        var propertyNames = relationshipProperties.Select(p => p.Name).ToList();

        Assert.Contains("VisibleRelation", propertyNames);
        Assert.DoesNotContain("HiddenRelation", propertyNames);
    }

    private class EntityWithJsonIgnore
    {
        public int Id { get; set; }
        public string VisibleProperty { get; set; } = string.Empty;

        [JsonIgnore]
        public string HiddenProperty { get; set; } = string.Empty;

        public RelatedEntityForIgnoreTest? VisibleRelation { get; set; }

        [JsonIgnore]
        public RelatedEntityForIgnoreTest? HiddenRelation { get; set; }
    }

    private class RelatedEntityForIgnoreTest
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}

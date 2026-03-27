using JsonApiToolkit.Mapping;
using JsonApiToolkit.Models.Resources;

namespace JsonApiToolkit.Tests.Mapping;

public class JsonColumnMappingTests
{
    public class EntityWithJsonColumns
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        // These collections have no ID properties, simulating EF Core owned entities stored as JSON
        public List<JsonData> JsonDataList { get; set; } = new();
        public ICollection<ExploitationReport> ExploitationReports { get; set; } =
            new List<ExploitationReport>();

        // Single object with no ID property, simulating EF Core owned entity stored as JSON
        public ComplexJsonData ComplexData { get; set; } = new();

        // This has an ID property, so should be a relationship
        public List<RelatedEntity> RelatedEntities { get; set; } = new();
    }

    public class JsonData
    {
        public string Type { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        // No Id property - this is owned entity stored as JSON
    }

    public class ExploitationReport
    {
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        // No Id property - this is owned entity stored as JSON
    }

    public class ComplexJsonData
    {
        public string Category { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
        public Dictionary<string, object> Properties { get; set; } = new();
        // No Id property - this is owned entity stored as JSON
    }

    public class RelatedEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public void GetAttributeProperties_IncludesJsonColumns()
    {
        // Arrange
        Type entityType = typeof(EntityWithJsonColumns);

        // Act
        var attributeProperties = EntityMapper.GetAttributeProperties(entityType);
        var attributeNames = attributeProperties.Select(p => p.Name).ToList();

        // Assert
        Assert.Contains("Name", attributeNames);
        Assert.Contains("JsonDataList", attributeNames); // Should be included as attribute (no IDs)
        Assert.Contains("ExploitationReports", attributeNames); // Should be included as attribute (no IDs)
        Assert.Contains("ComplexData", attributeNames); // Should be included as attribute (single object with no ID)
        Assert.DoesNotContain("RelatedEntities", attributeNames); // Should NOT be attribute (has IDs)
    }

    [Fact]
    public void GetRelationshipProperties_ExcludesJsonColumns()
    {
        // Arrange
        Type entityType = typeof(EntityWithJsonColumns);

        // Act
        var relationshipProperties = EntityMapper.GetRelationshipProperties(entityType);
        var relationshipNames = relationshipProperties.Select(p => p.Name).ToList();

        // Assert
        Assert.DoesNotContain("JsonDataList", relationshipNames); // Should NOT be relationship (no IDs)
        Assert.DoesNotContain("ExploitationReports", relationshipNames); // Should NOT be relationship (no IDs)
        Assert.DoesNotContain("ComplexData", relationshipNames); // Should NOT be relationship (single object with no ID)
        Assert.Contains("RelatedEntities", relationshipNames); // Should be relationship (has IDs)
    }

    [Fact]
    public void ToResourceObject_MapsJsonColumnsAsAttributes()
    {
        // Arrange
        var entity = new EntityWithJsonColumns
        {
            Id = 1,
            Name = "Test Entity",
            JsonDataList = new List<JsonData>
            {
                new()
                {
                    Type = "warning",
                    Value = "test warning",
                    Timestamp = DateTime.Now,
                },
                new()
                {
                    Type = "error",
                    Value = "test error",
                    Timestamp = DateTime.Now,
                },
            },
            ExploitationReports = new List<ExploitationReport>
            {
                new() { Description = "CVE-2024-1234", Severity = "High" },
            },
            ComplexData = new ComplexJsonData
            {
                Category = "Security",
                Tags = new List<string> { "vulnerability", "critical" },
                Properties = new Dictionary<string, object> { { "score", 9.5 } },
            },
            RelatedEntities = new List<RelatedEntity>
            {
                new() { Id = 10, Name = "Related 1" },
            },
        };

        // Act
        ResourceObject resource = JsonApiMapper.ToResourceObject(entity, "entityWithJsonColumns");

        // Assert
        Assert.NotNull(resource.Attributes);
        Assert.Equal("Test Entity", resource.Attributes["name"]);

        // JSON columns should be in attributes
        Assert.True(resource.Attributes.TryGetValue("jsonDataList", out var jsonDataListValue));
        Assert.True(
            resource.Attributes.TryGetValue("exploitationReports", out var exploitationReportsValue)
        );
        Assert.True(resource.Attributes.TryGetValue("complexData", out var complexDataValue));

        // Verify the JSON data is preserved
        var jsonDataList = jsonDataListValue as List<JsonData>;
        Assert.NotNull(jsonDataList);
        Assert.Equal(2, jsonDataList.Count);

        var exploitationReports = exploitationReportsValue as ICollection<ExploitationReport>;
        Assert.NotNull(exploitationReports);
        Assert.Single(exploitationReports);

        var complexData = complexDataValue as ComplexJsonData;
        Assert.NotNull(complexData);
        Assert.Equal("Security", complexData.Category);
        Assert.Equal(2, complexData.Tags.Count);
        Assert.True(complexData.Properties.ContainsKey("score"));

        // RelatedEntities should NOT be in attributes (it's a relationship)
        Assert.False(resource.Attributes.ContainsKey("relatedEntities"));
    }
}

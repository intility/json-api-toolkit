using System.Text.Json.Serialization;

namespace JsonApiToolkit.Tests.Models;

public class TestEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public TestStatus Status { get; set; }
    public TestRelatedEntity? RelatedEntity { get; set; }
    public int? RelatedEntityId { get; set; }
    public List<TestChildEntity> Children { get; set; } = new();
}

public enum TestStatus
{
    Draft,
    Published,
    Archived,
}

public class TestRelatedEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public TestNestedEntity? NestedEntity { get; set; }
}

public class TestNestedEntity
{
    public int Id { get; set; }
    public string Value { get; set; } = string.Empty;
}

public class TestChildEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int TestEntityId { get; set; }
    public TestEntity? TestEntity { get; set; }
    public List<string> Tags { get; set; } = new();
}

public class TestEntityWithJsonPropertyName
{
    public int Id { get; set; }

    [JsonPropertyName("customId")]
    public string? ActualPropertyName { get; set; }

    [JsonPropertyName("display_name")]
    public string? InternalName { get; set; }
}

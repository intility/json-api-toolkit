namespace JsonApiToolkit.Tests.Models;

public class TestEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public TestRelatedEntity? RelatedEntity { get; set; }
    public int? RelatedEntityId { get; set; }
    public List<TestChildEntity> Children { get; set; } = new();
}

public class TestRelatedEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class TestChildEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int TestEntityId { get; set; }
    public TestEntity? TestEntity { get; set; }
}

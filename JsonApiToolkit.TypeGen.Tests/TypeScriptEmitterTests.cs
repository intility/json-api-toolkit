using JsonApiToolkit.Attributes;

namespace JsonApiToolkit.TypeGen.Tests;

/// <summary>
/// Fixture covering nullable reference types, nullable value types, primitive
/// collections, FK scalars, to-one/to-many relationships, and unmapped targets.
/// </summary>
[JsonApiResource("widgets")]
public class Widget
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public DateTime? RetiredAt { get; set; }
    public List<string> Tags { get; set; } = [];
    public int OwnerId { get; set; }
    public Owner? Owner { get; set; }
    public List<Part> Parts { get; set; } = [];
    public UnmappedThing? Extra { get; set; }
}

[JsonApiResource("owners")]
public class Owner
{
    public int Id { get; set; }
    public required string Name { get; set; }
}

[JsonApiResource("parts")]
public class Part
{
    public int Id { get; set; }
    public required string Sku { get; set; }
}

public class UnmappedThing
{
    public int Id { get; set; }
}

public class TypeScriptEmitterTests
{
    [Fact]
    public void Generate_maps_attributes_and_relationships_and_skips_unmapped_types()
    {
        var resources = new (Type Type, string WireType)[]
        {
            (typeof(Widget), "widgets"),
            (typeof(Owner), "owners"),
            (typeof(Part), "parts"),
        };

        var ts = TypeScriptEmitter.Generate(resources);

        Assert.Contains("description: string | null;", ts);
        Assert.Contains("retiredAt: string | null;", ts);
        Assert.Contains("tags: string[];", ts);
        Assert.Contains("ownerId: number;", ts);
        Assert.Contains("owner: Owner | null;", ts);
        Assert.Contains("parts: Part[];", ts);
        Assert.Contains("Widget: { type: \"widgets\", relationships: [\"owner\", \"parts\"] }", ts);

        // UnmappedThing has no [JsonApiResource]: the property is dropped, not guessed at.
        Assert.DoesNotContain("extra", ts);
        Assert.DoesNotContain("UnmappedThing", ts);
    }
}

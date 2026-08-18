using JsonApiToolkit.Extensions.Querying;
using JsonApiToolkit.Models.Errors;
using JsonApiToolkit.Models.Querying.Filtering;

namespace JsonApiToolkit.Tests.Extensions.Filtering;

public class RecursionDepthGuardTests
{
    private static readonly FilterExpressionComposer Composer = new();

    private static FilterGroup GroupFor(string field, string value = "1")
    {
        return new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = field,
                    Value = value,
                    Operator = FilterOperator.Eq,
                },
            },
        };
    }

    // Test entity with nested collections to trigger recursion
    private class Level0
    {
        public int Id { get; set; }
        public List<Level1> Items { get; set; } = [];
    }

    private class Level1
    {
        public int Id { get; set; }
        public List<Level2> Items { get; set; } = [];
    }

    private class Level2
    {
        public int Id { get; set; }
        public List<Level3> Items { get; set; } = [];
    }

    private class Level3
    {
        public int Id { get; set; }
        public List<Level4> Items { get; set; } = [];
    }

    private class Level4
    {
        public int Id { get; set; }
        public List<Level5> Items { get; set; } = [];
    }

    private class Level5
    {
        public int Id { get; set; }
        public List<Level6> Items { get; set; } = [];
    }

    private class Level6
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    [Fact]
    public void Compose_WithShallowNesting_Succeeds()
    {
        // 2 levels of collection nesting should work fine
        var lambda = Composer.Compose<Level0>(GroupFor("items.items.id"));

        Assert.NotNull(lambda);
    }

    [Fact]
    public void Compose_WithDeeplyNestedCollections_ThrowsBadRequest()
    {
        // 6 levels of collection nesting should exceed the limit (MaxRecursionDepth = 5)
        var group = GroupFor("items.items.items.items.items.items.name", "test");

        var exception = Assert.Throws<JsonApiBadRequestException>(() =>
            Composer.Compose<Level0>(group)
        );

        Assert.Contains("recursion depth", exception.Message.ToLower());
        Assert.Contains("5", exception.Message); // MaxRecursionDepth
        Assert.Equal(JsonApiErrorCodes.QueryTooComplex, exception.Code);
    }

    [Fact]
    public void Compose_AtExactLimit_Succeeds()
    {
        // 5 levels should be exactly at the limit and work
        var lambda = Composer.Compose<Level0>(GroupFor("items.items.items.items.items.id"));

        Assert.NotNull(lambda);
    }

    [Fact]
    public void Compose_JustOverLimit_ThrowsBadRequest()
    {
        // 6 levels should be just over the limit
        var group = GroupFor("items.items.items.items.items.items.id");

        var exception = Assert.Throws<JsonApiBadRequestException>(() =>
            Composer.Compose<Level0>(group)
        );

        Assert.Contains("recursion depth", exception.Message.ToLower());
    }

    [Fact]
    public void Compose_ErrorMetadata_ContainsFieldInfo()
    {
        var group = GroupFor("items.items.items.items.items.items.name", "test");

        var exception = Assert.Throws<JsonApiBadRequestException>(() =>
            Composer.Compose<Level0>(group)
        );

        Assert.NotNull(exception.ErrorSource);
        Assert.StartsWith("filter[", exception.ErrorSource.Parameter);
        Assert.NotNull(exception.Meta);
        Assert.Equal(5, exception.Meta["maxDepth"]);
        Assert.True((int)exception.Meta["actualDepth"] > 5); // Should exceed the limit
    }
}

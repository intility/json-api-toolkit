using System.Linq.Expressions;
using JsonApiToolkit.Extensions.Querying;
using JsonApiToolkit.Models.Errors;
using JsonApiToolkit.Models.Querying.Filtering;

namespace JsonApiToolkit.Tests.Extensions;

public class RecursionDepthGuardTests
{
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
    public void BuildFilterExpression_WithShallowNesting_Succeeds()
    {
        // 2 levels of collection nesting should work fine
        var filter = new FilterParameter
        {
            Field = "items.items.id",
            Value = "1",
            Operator = FilterOperator.Eq,
        };

        var parameter = Expression.Parameter(typeof(Level0), "x");

        // This should not throw
        var expression = PropertyNavigator.BuildSafeNestedFilterExpression(parameter, filter);

        Assert.NotNull(expression);
    }

    [Fact]
    public void BuildFilterExpression_WithDeeplyNestedCollections_ThrowsBadRequest()
    {
        // 7 levels of collection nesting should exceed the limit (MaxRecursionDepth = 5)
        var filter = new FilterParameter
        {
            Field = "items.items.items.items.items.items.name",
            Value = "test",
            Operator = FilterOperator.Eq,
        };

        var parameter = Expression.Parameter(typeof(Level0), "x");

        var exception = Assert.Throws<JsonApiBadRequestException>(() =>
            PropertyNavigator.BuildSafeNestedFilterExpression(parameter, filter)
        );

        Assert.Contains("recursion depth", exception.Message.ToLower());
        Assert.Contains("5", exception.Message); // MaxRecursionDepth
        Assert.Equal(JsonApiErrorCodes.QueryTooComplex, exception.Code);
    }

    [Fact]
    public void BuildFilterExpression_AtExactLimit_Succeeds()
    {
        // 5 levels should be exactly at the limit and work
        var filter = new FilterParameter
        {
            Field = "items.items.items.items.items.id",
            Value = "1",
            Operator = FilterOperator.Eq,
        };

        var parameter = Expression.Parameter(typeof(Level0), "x");

        // This should not throw - exactly at limit
        var expression = PropertyNavigator.BuildSafeNestedFilterExpression(parameter, filter);

        Assert.NotNull(expression);
    }

    [Fact]
    public void BuildFilterExpression_JustOverLimit_ThrowsBadRequest()
    {
        // 6 levels should be just over the limit
        var filter = new FilterParameter
        {
            Field = "items.items.items.items.items.items.id",
            Value = "1",
            Operator = FilterOperator.Eq,
        };

        var parameter = Expression.Parameter(typeof(Level0), "x");

        var exception = Assert.Throws<JsonApiBadRequestException>(() =>
            PropertyNavigator.BuildSafeNestedFilterExpression(parameter, filter)
        );

        Assert.Contains("recursion depth", exception.Message.ToLower());
    }

    [Fact]
    public void BuildFilterExpression_ErrorMetadata_ContainsFieldInfo()
    {
        var filter = new FilterParameter
        {
            Field = "items.items.items.items.items.items.name",
            Value = "test",
            Operator = FilterOperator.Eq,
        };

        var parameter = Expression.Parameter(typeof(Level0), "x");

        var exception = Assert.Throws<JsonApiBadRequestException>(() =>
            PropertyNavigator.BuildSafeNestedFilterExpression(parameter, filter)
        );

        Assert.NotNull(exception.ErrorSource);
        Assert.StartsWith("filter[", exception.ErrorSource.Parameter);
        Assert.NotNull(exception.Meta);
        Assert.Equal(5, exception.Meta["maxDepth"]);
        Assert.True((int)exception.Meta["actualDepth"] > 5); // Should exceed the limit
    }
}

using System.Linq.Expressions;
using JsonApiToolkit.Extensions.Querying;
using JsonApiToolkit.Models.Querying.Filtering;
using JsonApiToolkit.Tests.Models;

namespace JsonApiToolkit.Tests.Extensions;

public class FilterExpressionBuilderTests
{
    private IQueryable<TestEntity> GetTestData()
    {
        return new List<TestEntity>
        {
            new TestEntity
            {
                Id = 1,
                Name = "Alpha",
                IsActive = true,
                Status = TestStatus.Published,
            },
            new TestEntity
            {
                Id = 2,
                Name = "Beta",
                IsActive = false,
                Status = TestStatus.Draft,
            },
            new TestEntity
            {
                Id = 3,
                Name = "Gamma",
                IsActive = true,
                Status = TestStatus.Archived,
            },
            new TestEntity
            {
                Id = 4,
                Name = "Delta",
                IsActive = false,
                Status = TestStatus.Published,
            },
        }.AsQueryable();
    }

    [Fact]
    public void BuildFilterExpression_WithNotOperator_AppliesCorrectLogic()
    {
        var filterGroup = new FilterGroup
        {
            LogicalOperator = LogicalOperator.Not,
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "IsActive",
                    Operator = FilterOperator.Eq,
                    Value = "true",
                },
            },
        };
        var parameter = Expression.Parameter(typeof(TestEntity), "x");

        var expression = FilterExpressionBuilder.BuildFilterExpression<TestEntity>(
            filterGroup,
            parameter
        );

        Assert.NotNull(expression);
        var compiledExpression = Expression
            .Lambda<Func<TestEntity, bool>>(expression, parameter)
            .Compile();

        var testData = GetTestData();
        var result = testData.Where(compiledExpression).ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, entity => Assert.False(entity.IsActive));
    }

    [Fact]
    public void BuildFilterExpression_WithMultipleFiltersAndNotOperator_AppliesCorrectLogic()
    {
        var filterGroup = new FilterGroup
        {
            LogicalOperator = LogicalOperator.Not,
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "IsActive",
                    Operator = FilterOperator.Eq,
                    Value = "true",
                },
                new FilterParameter
                {
                    Field = "Id",
                    Operator = FilterOperator.Gt,
                    Value = "2",
                },
            },
        };
        var parameter = Expression.Parameter(typeof(TestEntity), "x");

        var expression = FilterExpressionBuilder.BuildFilterExpression<TestEntity>(
            filterGroup,
            parameter
        );

        Assert.NotNull(expression);
        var compiledExpression = Expression
            .Lambda<Func<TestEntity, bool>>(expression, parameter)
            .Compile();

        var testData = GetTestData();
        var result = testData.Where(compiledExpression).ToList();

        Assert.Equal(3, result.Count);
        Assert.DoesNotContain(result, e => e.Id == 3);
    }

    [Fact]
    public void BuildFilterExpression_WithOrOperator_AppliesCorrectLogic()
    {
        var filterGroup = new FilterGroup
        {
            LogicalOperator = LogicalOperator.Or,
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "Name",
                    Operator = FilterOperator.Eq,
                    Value = "Alpha",
                },
                new FilterParameter
                {
                    Field = "Name",
                    Operator = FilterOperator.Eq,
                    Value = "Gamma",
                },
            },
        };
        var parameter = Expression.Parameter(typeof(TestEntity), "x");

        var expression = FilterExpressionBuilder.BuildFilterExpression<TestEntity>(
            filterGroup,
            parameter
        );

        Assert.NotNull(expression);
        var compiledExpression = Expression
            .Lambda<Func<TestEntity, bool>>(expression, parameter)
            .Compile();

        var testData = GetTestData();
        var result = testData.Where(compiledExpression).ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, e => e.Name == "Alpha");
        Assert.Contains(result, e => e.Name == "Gamma");
    }

    [Fact]
    public void BuildFilterExpression_WithAndOperator_AppliesCorrectLogic()
    {
        var filterGroup = new FilterGroup
        {
            LogicalOperator = LogicalOperator.And,
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "IsActive",
                    Operator = FilterOperator.Eq,
                    Value = "true",
                },
                new FilterParameter
                {
                    Field = "Id",
                    Operator = FilterOperator.Gt,
                    Value = "1",
                },
            },
        };
        var parameter = Expression.Parameter(typeof(TestEntity), "x");

        var expression = FilterExpressionBuilder.BuildFilterExpression<TestEntity>(
            filterGroup,
            parameter
        );

        Assert.NotNull(expression);
        var compiledExpression = Expression
            .Lambda<Func<TestEntity, bool>>(expression, parameter)
            .Compile();

        var testData = GetTestData();
        var result = testData.Where(compiledExpression).ToList();

        Assert.Single(result);
        Assert.Equal(3, result[0].Id);
        Assert.Equal("Gamma", result[0].Name);
    }

    [Fact]
    public void BuildFilterExpression_WithEmptyGroup_ReturnsNull()
    {
        var filterGroup = new FilterGroup();
        var parameter = Expression.Parameter(typeof(TestEntity), "x");

        var expression = FilterExpressionBuilder.BuildFilterExpression<TestEntity>(
            filterGroup,
            parameter
        );

        Assert.Null(expression);
    }

    [Fact]
    public void BuildFilterExpression_WithInvalidProperty_IgnoresFilter()
    {
        var filterGroup = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "NonExistentProperty",
                    Operator = FilterOperator.Eq,
                    Value = "test",
                },
                new FilterParameter
                {
                    Field = "Name",
                    Operator = FilterOperator.Eq,
                    Value = "Alpha",
                },
            },
        };
        var parameter = Expression.Parameter(typeof(TestEntity), "x");

        var expression = FilterExpressionBuilder.BuildFilterExpression<TestEntity>(
            filterGroup,
            parameter
        );

        Assert.NotNull(expression);
        var compiledExpression = Expression
            .Lambda<Func<TestEntity, bool>>(expression, parameter)
            .Compile();

        var testData = GetTestData();
        var result = testData.Where(compiledExpression).ToList();

        Assert.Single(result);
        Assert.Equal("Alpha", result[0].Name);
    }

    [Fact]
    public void BuildFilterExpression_WithNestedProperty_FiltersCorrectly()
    {
        var testData = new List<TestEntity>
        {
            new TestEntity
            {
                Id = 1,
                Name = "Alpha",
                RelatedEntity = new TestRelatedEntity
                {
                    Id = 10,
                    Name = "Related1",
                    NestedEntity = new TestNestedEntity { Id = 100, Value = "NestedValue1" },
                },
            },
            new TestEntity
            {
                Id = 2,
                Name = "Beta",
                RelatedEntity = new TestRelatedEntity
                {
                    Id = 20,
                    Name = "Related2",
                    NestedEntity = new TestNestedEntity { Id = 200, Value = "NestedValue2" },
                },
            },
        }.AsQueryable();

        var filterGroup = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "RelatedEntity.NestedEntity.Value",
                    Operator = FilterOperator.Eq,
                    Value = "NestedValue1",
                },
            },
        };
        var parameter = Expression.Parameter(typeof(TestEntity), "x");

        var expression = FilterExpressionBuilder.BuildFilterExpression<TestEntity>(
            filterGroup,
            parameter
        );

        Assert.NotNull(expression);
        var compiledExpression = Expression
            .Lambda<Func<TestEntity, bool>>(expression, parameter)
            .Compile();

        var result = testData.Where(compiledExpression).ToList();

        Assert.Single(result);
        Assert.Equal("Alpha", result[0].Name);
    }

    [Fact]
    public void BuildFilterExpression_WithInvalidNestedProperty_IgnoresFilter()
    {
        var filterGroup = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "RelatedEntity.NonExistent.Property",
                    Operator = FilterOperator.Eq,
                    Value = "test",
                },
            },
        };
        var parameter = Expression.Parameter(typeof(TestEntity), "x");

        var expression = FilterExpressionBuilder.BuildFilterExpression<TestEntity>(
            filterGroup,
            parameter
        );

        Assert.Null(expression);
    }

    [Fact]
    public void BuildFilterExpression_WithNullNestedProperty_HandlesGracefully()
    {
        var testData = new List<TestEntity>
        {
            new TestEntity
            {
                Id = 1,
                Name = "Alpha",
                RelatedEntity = null,
            },
            new TestEntity
            {
                Id = 2,
                Name = "Beta",
                RelatedEntity = new TestRelatedEntity { Id = 20, Name = "Related2" },
            },
        }.AsQueryable();

        var filterGroup = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "RelatedEntity.Name",
                    Operator = FilterOperator.Eq,
                    Value = "Related2",
                },
            },
        };
        var parameter = Expression.Parameter(typeof(TestEntity), "x");

        var expression = FilterExpressionBuilder.BuildFilterExpression<TestEntity>(
            filterGroup,
            parameter
        );

        Assert.NotNull(expression);
        var compiledExpression = Expression
            .Lambda<Func<TestEntity, bool>>(expression, parameter)
            .Compile();

        // Should safely handle null references and only return matching entities
        var result = testData.Where(compiledExpression).ToList();

        Assert.Single(result);
        Assert.Equal("Beta", result[0].Name);
    }
}

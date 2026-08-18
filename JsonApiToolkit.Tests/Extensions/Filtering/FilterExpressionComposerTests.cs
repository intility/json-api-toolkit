using System.Linq.Expressions;
using JsonApiToolkit.Extensions.Querying;
using JsonApiToolkit.Models.Querying.Filtering;
using JsonApiToolkit.Tests.Models;

namespace JsonApiToolkit.Tests.Extensions.Filtering;

public class FilterExpressionComposerTests
{
    private static readonly FilterExpressionComposer Composer = new();

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
    public void Compose_WithNotOperator_AppliesCorrectLogic()
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

        var lambda = Composer.Compose<TestEntity>(filterGroup);

        Assert.NotNull(lambda);
        var result = GetTestData().Where(lambda.Compile()).ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, entity => Assert.False(entity.IsActive));
    }

    [Fact]
    public void Compose_WithMultipleFiltersAndNotOperator_AppliesCorrectLogic()
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

        var lambda = Composer.Compose<TestEntity>(filterGroup);

        Assert.NotNull(lambda);
        var result = GetTestData().Where(lambda.Compile()).ToList();

        Assert.Equal(3, result.Count);
        Assert.DoesNotContain(result, e => e.Id == 3);
    }

    [Fact]
    public void Compose_WithOrOperator_AppliesCorrectLogic()
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

        var lambda = Composer.Compose<TestEntity>(filterGroup);

        Assert.NotNull(lambda);
        var result = GetTestData().Where(lambda.Compile()).ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, e => e.Name == "Alpha");
        Assert.Contains(result, e => e.Name == "Gamma");
    }

    [Fact]
    public void Compose_WithAndOperator_AppliesCorrectLogic()
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

        var lambda = Composer.Compose<TestEntity>(filterGroup);

        Assert.NotNull(lambda);
        var result = GetTestData().Where(lambda.Compile()).ToList();

        Assert.Single(result);
        Assert.Equal(3, result[0].Id);
        Assert.Equal("Gamma", result[0].Name);
    }

    [Fact]
    public void Compose_WithEmptyGroup_ReturnsNull()
    {
        var lambda = Composer.Compose<TestEntity>(new FilterGroup());

        Assert.Null(lambda);
    }

    [Fact]
    public void Compose_WithInvalidProperty_IgnoresFilter()
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

        var lambda = Composer.Compose<TestEntity>(filterGroup);

        Assert.NotNull(lambda);
        var result = GetTestData().Where(lambda.Compile()).ToList();

        Assert.Single(result);
        Assert.Equal("Alpha", result[0].Name);
    }

    [Fact]
    public void Compose_WithNestedProperty_FiltersCorrectly()
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

        var lambda = Composer.Compose<TestEntity>(filterGroup);

        Assert.NotNull(lambda);
        var result = testData.Where(lambda.Compile()).ToList();

        Assert.Single(result);
        Assert.Equal("Alpha", result[0].Name);
    }

    [Fact]
    public void Compose_WithInvalidNestedProperty_IgnoresFilter()
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

        var lambda = Composer.Compose<TestEntity>(filterGroup);

        Assert.Null(lambda);
    }

    [Fact]
    public void Compose_WithNullNestedProperty_HandlesGracefully()
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

        var lambda = Composer.Compose<TestEntity>(filterGroup);

        Assert.NotNull(lambda);

        // Should safely handle null references and only return matching entities
        var result = testData.Where(lambda.Compile()).ToList();

        Assert.Single(result);
        Assert.Equal("Beta", result[0].Name);
    }

    [Fact]
    public void Compose_NonGenericOverload_ProducesSamePredicate()
    {
        var filterGroup = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "Name",
                    Operator = FilterOperator.Eq,
                    Value = "Alpha",
                },
            },
        };

        var lambda = Composer.Compose(filterGroup, typeof(TestEntity));

        Assert.NotNull(lambda);
        var typed = Assert.IsAssignableFrom<Expression<Func<TestEntity, bool>>>(lambda);
        var result = GetTestData().Where(typed.Compile()).ToList();

        Assert.Single(result);
        Assert.Equal("Alpha", result[0].Name);
    }

    [Fact]
    public void Compose_WithCustomPropertyResolver_UsesResolver()
    {
        var composer = new FilterExpressionComposer(
            propertyResolver: (type, name) => type.GetProperty(name == "alias" ? "Name" : name)
        );

        var filterGroup = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "alias",
                    Operator = FilterOperator.Eq,
                    Value = "Alpha",
                },
            },
        };

        var lambda = composer.Compose<TestEntity>(filterGroup);

        Assert.NotNull(lambda);
        var result = GetTestData().Where(lambda.Compile()).ToList();

        Assert.Single(result);
        Assert.Equal("Alpha", result[0].Name);
    }
}

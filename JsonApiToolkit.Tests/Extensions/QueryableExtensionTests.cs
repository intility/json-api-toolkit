using JsonApiToolkit.Extensions.Querying;
using JsonApiToolkit.Models.Querying;
using JsonApiToolkit.Models.Querying.Filtering;
using JsonApiToolkit.Tests.Models;

namespace JsonApiToolkit.Tests.Extensions;

public class QueryableExtensionsTests
{
    private IQueryable<TestEntity> GetTestData()
    {
        return new List<TestEntity>
        {
            new TestEntity
            {
                Id = 1,
                Name = "Alpha",
                Description = "First",
                CreatedAt = new DateTime(2023, 1, 1),
                IsActive = true,
                Status = TestStatus.Published,
            },
            new TestEntity
            {
                Id = 2,
                Name = "Beta",
                Description = null,
                CreatedAt = new DateTime(2023, 2, 1),
                IsActive = false,
                Status = TestStatus.Draft,
            },
            new TestEntity
            {
                Id = 3,
                Name = "Gamma",
                Description = "Third",
                CreatedAt = new DateTime(2023, 3, 1),
                IsActive = true,
                Status = TestStatus.Archived,
            },
            new TestEntity
            {
                Id = 4,
                Name = "Delta",
                Description = "Fourth",
                CreatedAt = new DateTime(2023, 4, 1),
                IsActive = false,
                Status = TestStatus.Published,
            },
            new TestEntity
            {
                Id = 5,
                Name = "Epsilon",
                Description = null,
                CreatedAt = new DateTime(2023, 5, 1),
                IsActive = true,
                Status = TestStatus.Draft,
            },
        }.AsQueryable();
    }

    [Fact]
    public void ApplyFilters_WithEqualityFilter_FiltersCorrectly()
    {
        var query = GetTestData();
        var filterGroup = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "Name",
                    Operator = FilterOperator.Eq,
                    Value = "Beta",
                },
            },
        };

        var result = query.ApplyFilters(filterGroup).ToList();

        Assert.Single(result);
        Assert.Equal(2, result[0].Id);
        Assert.Equal("Beta", result[0].Name);
    }

    [Fact]
    public void ApplyFilters_WithGreaterThanFilter_FiltersCorrectly()
    {
        var query = GetTestData();
        var filterGroup = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "Id",
                    Operator = FilterOperator.Gt,
                    Value = "3",
                },
            },
        };

        var result = query.ApplyFilters(filterGroup).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(4, result[0].Id);
        Assert.Equal(5, result[1].Id);
    }

    [Fact]
    public void ApplyFilters_WithLikeFilter_FiltersCorrectly()
    {
        var query = GetTestData();
        var filterGroup = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "Name",
                    Operator = FilterOperator.Like,
                    Value = "a",
                },
            },
        };

        var result = query.ApplyFilters(filterGroup).ToList();

        Assert.Equal(4, result.Count);
        Assert.Contains(result, e => e.Name == "Alpha");
        Assert.Contains(result, e => e.Name == "Gamma");
        Assert.Contains(result, e => e.Name == "Delta");
        Assert.Contains(result, e => e.Name == "Beta");
    }

    [Fact]
    public void ApplyFilters_WithLikeFilter_StripsPercentWildcards()
    {
        var query = GetTestData();
        var filterGroup = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "Name",
                    Operator = FilterOperator.Like,
                    Value = "%lph%", // Should match "Alpha" after stripping %
                },
            },
        };

        var result = query.ApplyFilters(filterGroup).ToList();

        Assert.Single(result);
        Assert.Equal("Alpha", result[0].Name);
    }

    [Fact]
    public void ApplyFilters_WithLikeFilter_PreservesLiteralPercent()
    {
        // Test that literal % in values is preserved when not in wildcard pattern
        var testData = new List<TestEntity>
        {
            new TestEntity { Id = 1, Name = "100% Complete" },
            new TestEntity { Id = 2, Name = "50% Done" },
            new TestEntity { Id = 3, Name = "Finished" },
        }.AsQueryable();

        var filterGroup = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "Name",
                    Operator = FilterOperator.Like,
                    Value = "100%", // Should match literal "100%", not be stripped
                },
            },
        };

        var result = testData.ApplyFilters(filterGroup).ToList();

        Assert.Single(result);
        Assert.Equal("100% Complete", result[0].Name);
    }

    [Fact]
    public void ApplyFilters_WithLogicalAndGroup_FiltersCorrectly()
    {
        var query = GetTestData();
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

        var result = query.ApplyFilters(filterGroup).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(3, result[0].Id);
        Assert.Equal(5, result[1].Id);
    }

    [Fact]
    public void ApplySorting_WithMultipleSorts_SortsCorrectly()
    {
        var query = GetTestData();

        query = query
            .ToList()
            .Select(e =>
            {
                if (e.Id == 1 || e.Id == 3)
                    e.IsActive = true;
                else
                    e.IsActive = false;
                return e;
            })
            .AsQueryable();

        var sortParameters = new List<SortParameter>
        {
            new SortParameter { Field = "IsActive", IsDescending = true },
            new SortParameter { Field = "Name", IsDescending = false },
        };

        var result = query.ApplySorting(sortParameters).ToList();

        Assert.Equal(5, result.Count);

        Assert.True(result[0].IsActive);
        Assert.True(result[1].IsActive);
        Assert.Equal("Alpha", result[0].Name);
        Assert.Equal("Gamma", result[1].Name);

        Assert.False(result[2].IsActive);
        Assert.False(result[3].IsActive);
        Assert.False(result[4].IsActive);
        Assert.Equal("Beta", result[2].Name);
        Assert.Equal("Delta", result[3].Name);
        Assert.Equal("Epsilon", result[4].Name);
    }

    [Fact]
    public void ApplyPagination_PaginatesCorrectly()
    {
        var query = GetTestData();
        var pagination = new PaginationParameters { Number = 2, Size = 2 };

        var result = query.ApplyPagination(pagination).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(3, result[0].Id);
        Assert.Equal(4, result[1].Id);
    }

    [Fact]
    public async Task CreatePaginationMetaAsync_CreatesCorrectMetadataAsync()
    {
        var query = GetTestData();
        var pagination = new PaginationParameters { Number = 2, Size = 2 };

        var meta = await query.CreatePaginationMetaAsync(pagination);

        Assert.Equal(5, meta.TotalResources);
        Assert.Equal(3, meta.TotalPages);
        Assert.Equal(2, meta.CurrentPage);
        Assert.Equal(2, meta.PageSize);
    }

    [Fact]
    public void ApplyPagination_WithInvalidPageNumber_ReturnsLastPage()
    {
        var query = GetTestData();
        var pagination = new PaginationParameters { Number = 10, Size = 2 };
        var result = query.ApplyPagination(pagination).ToList();

        Assert.Single(result);
        Assert.Equal(5, result[0].Id);
        Assert.Equal("Epsilon", result[0].Name);
    }

    [Fact]
    public void ApplyPagination_WithPageZero_ReturnsFirstPage()
    {
        var query = GetTestData();
        var pagination = new PaginationParameters { Number = 0, Size = 2 }; // Invalid page 0

        var result = query.ApplyPagination(pagination).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(1, result[0].Id);
        Assert.Equal(2, result[1].Id);
    }

    [Fact]
    public async Task CreatePaginationMetaAsync_WithInvalidPageNumber_ReturnsLastPageInMetadataAsync()
    {
        var query = GetTestData();
        var pagination = new PaginationParameters { Number = 10, Size = 2 };
        var totalCount = query.Count();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pagination.Size);
        var expectedCurrentPage = Math.Min(Math.Max(pagination.Number, 1), Math.Max(totalPages, 1));

        var meta = await query.CreatePaginationMetaAsync(pagination);

        Assert.Equal(5, meta.TotalResources);
        Assert.Equal(3, meta.TotalPages);
        Assert.Equal(3, meta.CurrentPage);
        Assert.Equal(2, meta.PageSize);
    }

    [Fact]
    public async Task CreatePaginationMetaAsync_WithPageZero_ReturnsFirstPageInMetadataAsync()
    {
        var query = GetTestData();
        var pagination = new PaginationParameters { Number = 0, Size = 2 };

        var meta = await query.CreatePaginationMetaAsync(pagination);

        Assert.Equal(5, meta.TotalResources);
        Assert.Equal(3, meta.TotalPages);
        Assert.Equal(1, meta.CurrentPage);
        Assert.Equal(2, meta.PageSize);
    }

    [Fact]
    public void ApplyPagination_WithEmptyDataset_ReturnsEmptyResult()
    {
        var emptyQuery = new List<TestEntity>().AsQueryable();
        var pagination = new PaginationParameters { Number = 2, Size = 10 };

        var result = emptyQuery.ApplyPagination(pagination).ToList();

        Assert.Empty(result);
    }

    [Fact]
    public async Task Issue_Scenario_PageTwoOfOneTotal_ReturnsLastPageDataAsync()
    {
        var query = GetTestData();
        var largePageQuery = query.Take(6).AsQueryable();
        var pagination = new PaginationParameters { Number = 2, Size = 10 };

        var result = largePageQuery.ApplyPagination(pagination).ToList();
        var meta = await largePageQuery.CreatePaginationMetaAsync(pagination);

        Assert.Equal(5, result.Count);
        Assert.Equal(5, meta.TotalResources);
        Assert.Equal(1, meta.TotalPages);
        Assert.Equal(1, meta.CurrentPage);
        Assert.Equal(10, meta.PageSize);

        Assert.True(result.Any());
        Assert.Equal(1, result.First().Id);
    }

    [Fact]
    public void ApplyFilters_WithInFilter_FiltersCorrectly()
    {
        var query = GetTestData();
        var filterGroup = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "Status",
                    Operator = FilterOperator.In,
                    Value = "Published,Draft",
                },
            },
        };

        var result = query.ApplyFilters(filterGroup).ToList();

        Assert.Equal(4, result.Count);
        Assert.All(
            result,
            e => Assert.True(e.Status == TestStatus.Published || e.Status == TestStatus.Draft)
        );
        Assert.DoesNotContain(result, e => e.Status == TestStatus.Archived);
    }

    [Fact]
    public void ApplyFilters_WithNinFilter_FiltersCorrectly()
    {
        var query = GetTestData();
        var filterGroup = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "Status",
                    Operator = FilterOperator.Nin,
                    Value = "Archived",
                },
            },
        };

        var result = query.ApplyFilters(filterGroup).ToList();

        Assert.Equal(4, result.Count);
        Assert.All(result, e => Assert.NotEqual(TestStatus.Archived, e.Status));
    }

    [Fact]
    public void ApplyFilters_WithIsNullFilter_FiltersCorrectly()
    {
        var query = GetTestData();
        var filterGroup = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "Description",
                    Operator = FilterOperator.IsNull,
                    Value = "true",
                },
            },
        };

        var result = query.ApplyFilters(filterGroup).ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, e => Assert.Null(e.Description));
        Assert.Contains(result, e => e.Name == "Beta");
        Assert.Contains(result, e => e.Name == "Epsilon");
    }

    [Fact]
    public void ApplyFilters_WithIsNotNullFilter_FiltersCorrectly()
    {
        var query = GetTestData();
        var filterGroup = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "Description",
                    Operator = FilterOperator.IsNotNull,
                    Value = "true",
                },
            },
        };

        var result = query.ApplyFilters(filterGroup).ToList();

        Assert.Equal(3, result.Count);
        Assert.All(result, e => Assert.NotNull(e.Description));
        Assert.Contains(result, e => e.Name == "Alpha");
        Assert.Contains(result, e => e.Name == "Gamma");
        Assert.Contains(result, e => e.Name == "Delta");
    }

    [Fact]
    public void ApplyFilters_WithNeFilter_FiltersCorrectly()
    {
        var query = GetTestData();
        var filterGroup = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "Status",
                    Operator = FilterOperator.Ne,
                    Value = "Draft",
                },
            },
        };

        var result = query.ApplyFilters(filterGroup).ToList();

        Assert.Equal(3, result.Count);
        Assert.All(result, e => Assert.NotEqual(TestStatus.Draft, e.Status));
    }

    [Fact]
    public void ApplyFilters_WithLogicalOrGroup_FiltersCorrectly()
    {
        var query = GetTestData();
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
                    Field = "Status",
                    Operator = FilterOperator.Eq,
                    Value = "Archived",
                },
            },
        };

        var result = query.ApplyFilters(filterGroup).ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, e => e.Name == "Alpha");
        Assert.Contains(result, e => e.Status == TestStatus.Archived);
    }

    [Fact]
    public void ApplyFilters_WithLogicalNotGroup_FiltersCorrectly()
    {
        var query = GetTestData();
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

        var result = query.ApplyFilters(filterGroup).ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, e => Assert.False(e.IsActive));
    }

    [Fact]
    public void ApplyFilters_WithThreeLevelNestedFilter_FiltersCorrectly()
    {
        // Test three-level nesting: Entity.RelatedEntity.NestedEntity.Value
        var testData = new List<TestEntity>
        {
            new TestEntity
            {
                Id = 1,
                Name = "Entity1",
                RelatedEntity = new TestRelatedEntity
                {
                    Id = 10,
                    Name = "Related1",
                    NestedEntity = new TestNestedEntity { Id = 100, Value = "TargetValue" },
                },
            },
            new TestEntity
            {
                Id = 2,
                Name = "Entity2",
                RelatedEntity = new TestRelatedEntity
                {
                    Id = 20,
                    Name = "Related2",
                    NestedEntity = new TestNestedEntity { Id = 200, Value = "OtherValue" },
                },
            },
            new TestEntity
            {
                Id = 3,
                Name = "Entity3",
                RelatedEntity = null, // No related entity
            },
        }.AsQueryable();

        var filterGroup = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "relatedEntity.nestedEntity.value",
                    Operator = FilterOperator.Eq,
                    Value = "TargetValue",
                },
            },
        };

        var result = testData.ApplyFilters(filterGroup).ToList();

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
        Assert.Equal("TargetValue", result[0].RelatedEntity?.NestedEntity?.Value);
    }

    [Fact]
    public void ApplyFilters_WithCollectionNestedFilter_FiltersCorrectly()
    {
        // Test filtering through a collection: Entity.Children.Name
        var testData = new List<TestEntity>
        {
            new TestEntity
            {
                Id = 1,
                Name = "Entity1",
                Children = new List<TestChildEntity>
                {
                    new TestChildEntity { Id = 10, Name = "TargetChild" },
                    new TestChildEntity { Id = 11, Name = "OtherChild" },
                },
            },
            new TestEntity
            {
                Id = 2,
                Name = "Entity2",
                Children = new List<TestChildEntity>
                {
                    new TestChildEntity { Id = 20, Name = "DifferentChild" },
                },
            },
            new TestEntity
            {
                Id = 3,
                Name = "Entity3",
                Children = new List<TestChildEntity>(), // Empty collection
            },
        }.AsQueryable();

        var filterGroup = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "children.name",
                    Operator = FilterOperator.Eq,
                    Value = "TargetChild",
                },
            },
        };

        var result = testData.ApplyFilters(filterGroup).ToList();

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
        Assert.Contains(result[0].Children, c => c.Name == "TargetChild");
    }

    [Fact]
    public void ApplyFilters_WithCollectionPropertyFilter_FiltersCorrectly()
    {
        // Test filtering where the final property is a collection: Entity.Children.Tags contains value
        // This simulates: filter[children.tags][in]=important
        var testData = new List<TestEntity>
        {
            new TestEntity
            {
                Id = 1,
                Name = "Entity1",
                Children = new List<TestChildEntity>
                {
                    new TestChildEntity
                    {
                        Id = 10,
                        Name = "Child1",
                        Tags = new List<string> { "important", "urgent" },
                    },
                    new TestChildEntity
                    {
                        Id = 11,
                        Name = "Child2",
                        Tags = new List<string> { "normal" },
                    },
                },
            },
            new TestEntity
            {
                Id = 2,
                Name = "Entity2",
                Children = new List<TestChildEntity>
                {
                    new TestChildEntity
                    {
                        Id = 20,
                        Name = "Child3",
                        Tags = new List<string> { "low-priority" },
                    },
                },
            },
            new TestEntity
            {
                Id = 3,
                Name = "Entity3",
                Children = new List<TestChildEntity>(), // Empty collection
            },
        }.AsQueryable();

        var filterGroup = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "children.tags",
                    Operator = FilterOperator.In,
                    Value = "important",
                },
            },
        };

        var result = testData.ApplyFilters(filterGroup).ToList();

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
        Assert.Contains(result[0].Children, c => c.Tags.Contains("important"));
    }

    [Fact]
    public void ApplyFilters_WithNullCollectionInMemory_ThrowsNullReferenceException()
    {
        // Note: This test documents behavior for in-memory LINQ with null collections.
        // In EF Core, collection navigations are never null - they're empty lists.
        // We removed the null check for collections because it broke EF Core many-to-many translation.
        // This is an acceptable trade-off since:
        // 1. EF Core (the primary use case) never has null collections
        // 2. Modern C# code initializes collections (= new()) to avoid nulls
        var testData = new List<TestEntity>
        {
            new TestEntity
            {
                Id = 1,
                Name = "Entity1",
                Children = null!, // Explicitly null - unusual but possible in memory
            },
            new TestEntity
            {
                Id = 2,
                Name = "Entity2",
                Children = new List<TestChildEntity>
                {
                    new TestChildEntity { Id = 20, Name = "TargetChild" },
                },
            },
        }.AsQueryable();

        var filterGroup = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "children.name",
                    Operator = FilterOperator.Eq,
                    Value = "TargetChild",
                },
            },
        };

        // In-memory LINQ with null collection will throw - this is expected
        // because we prioritize EF Core compatibility over in-memory null handling
        Assert.Throws<ArgumentNullException>(() => testData.ApplyFilters(filterGroup).ToList());
    }
}

public static class TaskExtensions
{
    public static async Task<T> ToTaskResultAsync<T>(this T result)
    {
        await Task.Delay(1); // Simulate async
        return result;
    }
}

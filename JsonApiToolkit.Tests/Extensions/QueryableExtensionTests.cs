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
            },
            new TestEntity
            {
                Id = 2,
                Name = "Beta",
                Description = "Second",
                CreatedAt = new DateTime(2023, 2, 1),
                IsActive = false,
            },
            new TestEntity
            {
                Id = 3,
                Name = "Gamma",
                Description = "Third",
                CreatedAt = new DateTime(2023, 3, 1),
                IsActive = true,
            },
            new TestEntity
            {
                Id = 4,
                Name = "Delta",
                Description = "Fourth",
                CreatedAt = new DateTime(2023, 4, 1),
                IsActive = false,
            },
            new TestEntity
            {
                Id = 5,
                Name = "Epsilon",
                Description = "Fifth",
                CreatedAt = new DateTime(2023, 5, 1),
                IsActive = true,
            },
        }.AsQueryable();
    }

    [Fact]
    public void ApplyFilters_WithEqualityFilter_FiltersCorrectly()
    {
        // Arrange
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

        // Act
        var result = query.ApplyFilters(filterGroup).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(2, result[0].Id);
        Assert.Equal("Beta", result[0].Name);
    }

    [Fact]
    public void ApplyFilters_WithGreaterThanFilter_FiltersCorrectly()
    {
        // Arrange
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

        // Act
        var result = query.ApplyFilters(filterGroup).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(4, result[0].Id);
        Assert.Equal(5, result[1].Id);
    }

    [Fact]
    public void ApplyFilters_WithLikeFilter_FiltersCorrectly()
    {
        // Arrange
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

        // Act
        var result = query.ApplyFilters(filterGroup).ToList();

        // Assert
        Assert.Equal(4, result.Count); // changed to 4
        Assert.Contains(result, e => e.Name == "Alpha");
        Assert.Contains(result, e => e.Name == "Gamma");
        Assert.Contains(result, e => e.Name == "Delta");
        Assert.Contains(result, e => e.Name == "Beta");
    }

    [Fact]
    public void ApplyFilters_WithLogicalAndGroup_FiltersCorrectly()
    {
        // Arrange
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

        // Act
        var result = query.ApplyFilters(filterGroup).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(3, result[0].Id);
        Assert.Equal(5, result[1].Id);
    }

    // [Fact]
    // public void ApplySorting_WithSingleSort_SortsCorrectly()
    // {
    //     // Arrange
    //     var query = GetTestData();
    //     var sortParameters = new List<SortParameter>
    //     {
    //         new SortParameter { Field = "Name", IsDescending = true },
    //     };

    //     // Act
    //     var result = query.ApplySorting(sortParameters).ToList();

    //     // Assert
    //     Assert.Equal(5, result.Count);
    //     Assert.Equal("Epsilon", result[0].Name);
    //     Assert.Equal("Delta", result[1].Name);
    //     Assert.Equal("Gamma", result[2].Name);
    //     Assert.Equal("Beta", result[3].Name);
    //     Assert.Equal("Alpha", result[4].Name);
    // }

    [Fact]
    public void ApplySorting_WithMultipleSorts_SortsCorrectly()
    {
        // Arrange
        var query = GetTestData();

        // Modify some data to test secondary sorting
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

        // Act
        var result = query.ApplySorting(sortParameters).ToList();

        // Assert
        Assert.Equal(5, result.Count);

        // First the active ones (sorted by name)
        Assert.True(result[0].IsActive);
        Assert.True(result[1].IsActive);
        Assert.Equal("Alpha", result[0].Name);
        Assert.Equal("Gamma", result[1].Name);

        // Then the inactive ones (sorted by name)
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
        // Arrange
        var query = GetTestData();
        var pagination = new PaginationParameters { Number = 2, Size = 2 };

        // Act
        var result = query.ApplyPagination(pagination).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(3, result[0].Id);
        Assert.Equal(4, result[1].Id);
    }

    [Fact]
    public async Task CreatePaginationMetaAsync_CreatesCorrectMetadata()
    {
        // Arrange
        var query = GetTestData();
        var pagination = new PaginationParameters { Number = 2, Size = 2 };

        // Act
        var meta = await query.CreatePaginationMetaAsync(pagination);

        // Assert
        Assert.Equal(5, meta.TotalResources);
        Assert.Equal(3, meta.TotalPages);
        Assert.Equal(2, meta.CurrentPage);
        Assert.Equal(2, meta.PageSize);
    }

    [Fact]
    public void ApplyPagination_WithInvalidPageNumber_ReturnsLastPage()
    {
        // Arrange
        var query = GetTestData(); // 5 items
        var pagination = new PaginationParameters { Number = 10, Size = 2 }; // Request page 10, but only 3 pages exist

        // Act
        var result = query.ApplyPagination(pagination).ToList();

        // Assert - Should return the last page (page 3) which has 1 item (item 5)
        Assert.Single(result);
        Assert.Equal(5, result[0].Id);
        Assert.Equal("Epsilon", result[0].Name);
    }

    [Fact]
    public void ApplyPagination_WithPageZero_ReturnsFirstPage()
    {
        // Arrange
        var query = GetTestData();
        var pagination = new PaginationParameters { Number = 0, Size = 2 }; // Invalid page 0

        // Act
        var result = query.ApplyPagination(pagination).ToList();

        // Assert - Should return first page
        Assert.Equal(2, result.Count);
        Assert.Equal(1, result[0].Id);
        Assert.Equal(2, result[1].Id);
    }

    [Fact]
    public async Task CreatePaginationMetaAsync_WithInvalidPageNumber_ReturnsLastPageInMetadata()
    {
        // Arrange
        var query = GetTestData(); // 5 items
        var pagination = new PaginationParameters { Number = 10, Size = 2 }; // Request page 10, but only 3 pages exist

        // Create a simplified test scenario by manually implementing the meta logic
        var totalCount = query.Count();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pagination.Size);
        var expectedCurrentPage = Math.Min(Math.Max(pagination.Number, 1), Math.Max(totalPages, 1));

        // Act - for now we'll test the current behavior
        var meta = await query.CreatePaginationMetaAsync(pagination);

        // Assert
        Assert.Equal(5, meta.TotalResources);
        Assert.Equal(3, meta.TotalPages);
        Assert.Equal(3, meta.CurrentPage); // Should be clamped to last page (3)
        Assert.Equal(2, meta.PageSize);
    }

    [Fact]
    public async Task CreatePaginationMetaAsync_WithPageZero_ReturnsFirstPageInMetadata()
    {
        // Arrange
        var query = GetTestData();
        var pagination = new PaginationParameters { Number = 0, Size = 2 };

        // Act - for now we'll test the current behavior
        var meta = await query.CreatePaginationMetaAsync(pagination);

        // Assert
        Assert.Equal(5, meta.TotalResources);
        Assert.Equal(3, meta.TotalPages);
        Assert.Equal(1, meta.CurrentPage); // Should be clamped to first page (1)
        Assert.Equal(2, meta.PageSize);
    }

    [Fact]
    public void ApplyPagination_WithEmptyDataset_ReturnsEmptyResult()
    {
        // Arrange
        var emptyQuery = new List<TestEntity>().AsQueryable();
        var pagination = new PaginationParameters { Number = 2, Size = 10 };

        // Act
        var result = emptyQuery.ApplyPagination(pagination).ToList();

        // Assert
        Assert.Empty(result);
    }
}

// Helper extension to simulate async for in-memory testing
public static class TaskExtensions
{
    public static async Task<T> ToTaskResult<T>(this T result)
    {
        await Task.Delay(1); // Simulate async
        return result;
    }
}

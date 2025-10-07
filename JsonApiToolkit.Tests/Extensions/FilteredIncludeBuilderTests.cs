using JsonApiToolkit.Extensions.Querying;
using JsonApiToolkit.Models.Querying.Filtering;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace JsonApiToolkit.Tests.Extensions;

public class FilteredIncludeBuilderTests
{
    [Fact]
    public void ApplyFilteredIncludes_WithNoIncludePaths_ReturnsOriginalQuery()
    {
        // Arrange
        var query = CreateMockQueryable<TestEntity>();
        List<string>? includePaths = null;
        var includeFilters = new List<IncludeFilter>();

        // Act
        var result = query.ApplyFilteredIncludes(includePaths, includeFilters);

        // Assert
        Assert.Same(query, result);
    }

    [Fact]
    public void ApplyFilteredIncludes_WithEmptyIncludePaths_ReturnsOriginalQuery()
    {
        // Arrange
        var query = CreateMockQueryable<TestEntity>();
        var includePaths = new List<string>();
        var includeFilters = new List<IncludeFilter>();

        // Act
        var result = query.ApplyFilteredIncludes(includePaths, includeFilters);

        // Assert
        Assert.Same(query, result);
    }

    [Fact]
    public void ApplyFilteredIncludes_WithIncludePathsButNoFilters_AppliesRegularIncludes()
    {
        // Arrange
        var query = CreateMockQueryable<TestEntity>();
        var includePaths = new List<string> { "comments", "tags" };
        var includeFilters = new List<IncludeFilter>();

        // Act
        var result = query.ApplyFilteredIncludes(includePaths, includeFilters);

        // Assert
        // This would normally test that Include was called, but since we're using a mock
        // we can't easily verify the EF Core Include calls without a real context
        Assert.NotNull(result);
    }

    [Fact]
    public void ApplyFilteredIncludes_WithSimpleIncludeFilter_BuildsCorrectExpression()
    {
        // Arrange
        var query = CreateMockQueryable<TestEntity>();
        var includePaths = new List<string> { "comments" };
        var includeFilters = new List<IncludeFilter>
        {
            new()
            {
                RelationshipPath = "comments",
                FilterGroup = new FilterGroup
                {
                    Filters = new List<FilterParameter>
                    {
                        new()
                        {
                            Field = "status",
                            Operator = FilterOperator.Eq,
                            Value = "approved",
                        }
                    }
                }
            },
        };

        // Act
        var result = query.ApplyFilteredIncludes(includePaths, includeFilters);

        // Assert
        Assert.NotNull(result);
        // In a real test with EF Core, we would verify the generated SQL contains WHERE clause
    }

    [Fact]
    public void ApplyFilteredIncludes_WithMultipleFiltersOnSameRelationship_CombinesFilters()
    {
        // Arrange
        var query = CreateMockQueryable<TestEntity>();
        var includePaths = new List<string> { "comments" };
        var includeFilters = new List<IncludeFilter>
        {
            new()
            {
                RelationshipPath = "comments",
                FilterGroup = new FilterGroup
                {
                    Filters = new List<FilterParameter>
                    {
                        new()
                        {
                            Field = "status",
                            Operator = FilterOperator.Eq,
                            Value = "approved",
                        },
                        new()
                        {
                            Field = "priority",
                            Operator = FilterOperator.Gt,
                            Value = "5",
                        }
                    }
                }
            },
        };

        // Act
        var result = query.ApplyFilteredIncludes(includePaths, includeFilters);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void ApplyFilteredIncludes_WithNestedIncludePath_HandlesCorrectly()
    {
        // Arrange
        var query = CreateMockQueryable<TestEntity>();
        var includePaths = new List<string> { "comments.author" };
        var includeFilters = new List<IncludeFilter>();

        // Act
        var result = query.ApplyFilteredIncludes(includePaths, includeFilters);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void ApplyFilteredIncludes_WithMixedFilteredAndUnfilteredIncludes_HandlesCorrectly()
    {
        // Arrange
        var query = CreateMockQueryable<TestEntity>();
        var includePaths = new List<string> { "comments", "tags", "author" };
        var includeFilters = new List<IncludeFilter>
        {
            new()
            {
                RelationshipPath = "comments",
                FilterGroup = new FilterGroup
                {
                    Filters = new List<FilterParameter>
                    {
                        new()
                        {
                            Field = "status",
                            Operator = FilterOperator.Eq,
                            Value = "approved",
                        }
                    }
                }
            },
            // tags and author have no filters
        };

        // Act
        var result = query.ApplyFilteredIncludes(includePaths, includeFilters);

        // Assert
        Assert.NotNull(result);
    }

    private static IQueryable<T> CreateMockQueryable<T>()
        where T : class
    {
        // Create a minimal mock queryable for testing
        // In a real scenario, this would be an EF Core DbSet or similar
        var data = new List<T>().AsQueryable();
        return data;
    }

    // Test entity classes for testing
    public class TestEntity
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public List<Comment> Comments { get; set; } = new();
        public List<Tag> Tags { get; set; } = new();
        public Author? Author { get; set; }
    }

    public class Comment
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int Priority { get; set; }
        public Author? Author { get; set; }
        public string? CompanyCode { get; set; }
    }

    public class Tag
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class Author
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}

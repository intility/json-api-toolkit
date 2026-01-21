using JsonApiToolkit.Extensions.Querying;
using JsonApiToolkit.Models.Errors;
using JsonApiToolkit.Models.Querying.Filtering;
using Xunit;

namespace JsonApiToolkit.Tests.Extensions;

public class IncludeFilterParserTests
{
    [Fact]
    public void SeparateIncludeFilters_WithNoFilters_ReturnsEmpty()
    {
        // Arrange
        FilterGroup? filters = null;
        var includePaths = new List<string> { "comments" };

        // Act
        var (mainFilters, includeFilters) = IncludeFilterParser.SeparateIncludeFilters(
            filters,
            includePaths
        );

        // Assert
        Assert.Null(mainFilters);
        Assert.Empty(includeFilters);
    }

    [Fact]
    public void SeparateIncludeFilters_WithOnlyMainFilters_ReturnsMainFilters()
    {
        // Arrange
        var filters = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new()
                {
                    Field = "title",
                    Operator = FilterOperator.Eq,
                    Value = "Test",
                },
                new()
                {
                    Field = "status",
                    Operator = FilterOperator.Eq,
                    Value = "active",
                },
            },
        };
        var includePaths = new List<string> { "comments" };

        // Act
        var (mainFilters, includeFilters) = IncludeFilterParser.SeparateIncludeFilters(
            filters,
            includePaths
        );

        // Assert
        Assert.NotNull(mainFilters);
        Assert.Equal(2, mainFilters.Filters.Count);
        Assert.Empty(includeFilters);
    }

    [Fact]
    public void SeparateIncludeFilters_WithSimpleIncludeFilter_SeparatesCorrectly()
    {
        // Arrange - Include filters must have IsIncludeFilter=true (set by parser for bracket syntax)
        var filters = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new()
                {
                    Field = "title",
                    Operator = FilterOperator.Eq,
                    Value = "Test",
                },
                new()
                {
                    Field = "comments.status",
                    Operator = FilterOperator.Eq,
                    Value = "approved",
                    IsIncludeFilter = true, // Bracket syntax: filter[comments][status][eq]=approved
                },
            },
        };
        var includePaths = new List<string> { "comments" };

        // Act
        var (mainFilters, includeFilters) = IncludeFilterParser.SeparateIncludeFilters(
            filters,
            includePaths
        );

        // Assert
        Assert.NotNull(mainFilters);
        Assert.Single(mainFilters.Filters);
        Assert.Equal("title", mainFilters.Filters[0].Field);

        Assert.Single(includeFilters);
        Assert.Equal("comments", includeFilters[0].RelationshipPath);
        Assert.Single(includeFilters[0].FilterGroup.Filters);
        Assert.Equal("status", includeFilters[0].FilterGroup.Filters[0].Field);
        Assert.Equal("approved", includeFilters[0].FilterGroup.Filters[0].Value);
    }

    [Fact]
    public void SeparateIncludeFilters_WithDotNotation_TreatedAsPrimaryFilter()
    {
        // Arrange - Dot notation filters are primary filters (filter main resource through relationship)
        var filters = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new()
                {
                    Field = "title",
                    Operator = FilterOperator.Eq,
                    Value = "Test",
                },
                new()
                {
                    Field = "comments.status", // Dot notation without IsIncludeFilter = primary filter
                    Operator = FilterOperator.Eq,
                    Value = "approved",
                    IsIncludeFilter = false,
                },
            },
        };
        var includePaths = new List<string> { "comments" };

        // Act
        var (mainFilters, includeFilters) = IncludeFilterParser.SeparateIncludeFilters(
            filters,
            includePaths
        );

        // Assert - Both filters should be main filters (dot notation = primary filter)
        Assert.NotNull(mainFilters);
        Assert.Equal(2, mainFilters.Filters.Count);
        Assert.Contains(mainFilters.Filters, f => f.Field == "title");
        Assert.Contains(mainFilters.Filters, f => f.Field == "comments.status");

        // No include filters - dot notation is now primary filter
        Assert.Empty(includeFilters);
    }

    [Fact]
    public void SeparateIncludeFilters_WithKebabCaseInclude_HandlesCorrectly()
    {
        // Arrange - Include filters must have IsIncludeFilter=true
        var filters = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new()
                {
                    Field = "cveComments.companyCode",
                    Operator = FilterOperator.Eq,
                    Value = "AA",
                    IsIncludeFilter = true, // Bracket syntax: filter[cveComments][companyCode][eq]=AA
                },
            },
        };
        var includePaths = new List<string> { "cve-comments" };

        // Act
        var (mainFilters, includeFilters) = IncludeFilterParser.SeparateIncludeFilters(
            filters,
            includePaths
        );

        // Assert
        Assert.Single(includeFilters);
        Assert.Equal("cveComments", includeFilters[0].RelationshipPath);
        Assert.Single(includeFilters[0].FilterGroup.Filters);
        Assert.Equal("companyCode", includeFilters[0].FilterGroup.Filters[0].Field);
    }

    [Fact]
    public void SeparateIncludeFilters_WithNestedIncludeFilter_SeparatesCorrectly()
    {
        // Arrange - Include filters must have IsIncludeFilter=true
        var filters = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new()
                {
                    Field = "comments.author.department",
                    Operator = FilterOperator.Eq,
                    Value = "Security",
                    IsIncludeFilter = true, // Bracket syntax for nested: filter[comments.author][department][eq]=Security
                },
            },
        };
        var includePaths = new List<string> { "comments.author" };

        // Act
        var (mainFilters, includeFilters) = IncludeFilterParser.SeparateIncludeFilters(
            filters,
            includePaths
        );

        // Assert
        Assert.Single(includeFilters);
        Assert.Equal("comments.author", includeFilters[0].RelationshipPath);
        Assert.Single(includeFilters[0].FilterGroup.Filters);
        Assert.Equal("department", includeFilters[0].FilterGroup.Filters[0].Field);
    }

    [Fact]
    public void SeparateIncludeFilters_WithComplexOrFilter_HandlesCorrectly()
    {
        // Arrange - Include filters must have IsIncludeFilter=true
        var filters = new FilterGroup
        {
            LogicalOperator = LogicalOperator.Or,
            Filters = new List<FilterParameter>
            {
                new()
                {
                    Field = "comments.companyCode",
                    Operator = FilterOperator.Eq,
                    Value = "AA",
                    IsIncludeFilter = true,
                },
                new()
                {
                    Field = "comments.companyCode",
                    Operator = FilterOperator.IsNull,
                    Value = "true",
                    IsIncludeFilter = true,
                },
            },
        };
        var includePaths = new List<string> { "comments" };

        // Act
        var (mainFilters, includeFilters) = IncludeFilterParser.SeparateIncludeFilters(
            filters,
            includePaths
        );

        // Assert
        Assert.Single(includeFilters);
        Assert.Equal("comments", includeFilters[0].RelationshipPath);
        Assert.Equal(LogicalOperator.Or, includeFilters[0].FilterGroup.LogicalOperator);
        Assert.Equal(2, includeFilters[0].FilterGroup.Filters.Count);
        Assert.All(
            includeFilters[0].FilterGroup.Filters,
            f => Assert.Equal("companyCode", f.Field)
        );
    }

    [Fact]
    public void SeparateIncludeFilters_WithFilterOnNonIncludedRelationship_ReturnsAsMainFilter()
    {
        // Arrange - Even with IsIncludeFilter=true, if relationship is not included, it becomes main filter
        var filters = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new()
                {
                    Field = "comments.status",
                    Operator = FilterOperator.Eq,
                    Value = "approved",
                    IsIncludeFilter = true, // Marked as include filter but relationship not in includes
                },
            },
        };
        var includePaths = new List<string> { "author" }; // comments not included

        // Act
        var (mainFilters, includeFilters) = IncludeFilterParser.SeparateIncludeFilters(
            filters,
            includePaths
        );

        // Assert
        // When the relationship is not included, the filter should be treated as a main filter
        Assert.NotNull(mainFilters);
        Assert.Single(mainFilters.Filters);
        Assert.Equal("comments.status", mainFilters.Filters[0].Field);
        Assert.Empty(includeFilters);
    }

    [Fact]
    public void SeparateIncludeFilters_WithTooDeepNesting_ThrowsException()
    {
        // Arrange - Include filters must have IsIncludeFilter=true for depth checking
        var filters = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new()
                {
                    Field = "a.b.c.d.e",
                    Operator = FilterOperator.Eq,
                    Value = "test",
                    IsIncludeFilter = true, // 5 levels deep
                },
            },
        };
        var includePaths = new List<string> { "a.b.c.d" };

        // Act & Assert
        var exception = Assert.Throws<JsonApiBadRequestException>(
            () => IncludeFilterParser.SeparateIncludeFilters(filters, includePaths)
        );

        Assert.Contains("Filter depth exceeds maximum", exception.Message);
    }

    [Fact]
    public void SeparateIncludeFilters_WithMixedMainAndIncludeFilters_SeparatesCorrectly()
    {
        // Arrange - Include filters must have IsIncludeFilter=true
        var filters = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new()
                {
                    Field = "status",
                    Operator = FilterOperator.Eq,
                    Value = "active",
                },
                new()
                {
                    Field = "comments.approved",
                    Operator = FilterOperator.Eq,
                    Value = "true",
                    IsIncludeFilter = true, // Bracket syntax: filter[comments][approved][eq]=true
                },
                new()
                {
                    Field = "priority",
                    Operator = FilterOperator.Gt,
                    Value = "5",
                },
            },
        };
        var includePaths = new List<string> { "comments", "author" };

        // Act
        var (mainFilters, includeFilters) = IncludeFilterParser.SeparateIncludeFilters(
            filters,
            includePaths
        );

        // Assert
        Assert.NotNull(mainFilters);
        Assert.Equal(2, mainFilters.Filters.Count);
        Assert.Contains(mainFilters.Filters, f => f.Field == "status");
        Assert.Contains(mainFilters.Filters, f => f.Field == "priority");

        Assert.Single(includeFilters);
        Assert.Equal("comments", includeFilters[0].RelationshipPath);
        Assert.Single(includeFilters[0].FilterGroup.Filters);
        Assert.Equal("approved", includeFilters[0].FilterGroup.Filters[0].Field);
    }

    [Fact]
    public void SeparateIncludeFilters_WithNestedGroups_HandlesCorrectly()
    {
        // Arrange - Include filters must have IsIncludeFilter=true
        var filters = new FilterGroup
        {
            LogicalOperator = LogicalOperator.And,
            Filters = new List<FilterParameter>
            {
                new()
                {
                    Field = "title",
                    Operator = FilterOperator.Eq,
                    Value = "Test",
                },
            },
            Groups = new List<FilterGroup>
            {
                new FilterGroup
                {
                    LogicalOperator = LogicalOperator.Or,
                    Filters = new List<FilterParameter>
                    {
                        new()
                        {
                            Field = "comments.status",
                            Operator = FilterOperator.Eq,
                            Value = "approved",
                            IsIncludeFilter = true,
                        },
                        new()
                        {
                            Field = "comments.status",
                            Operator = FilterOperator.Eq,
                            Value = "pending",
                            IsIncludeFilter = true,
                        },
                    },
                },
            },
        };
        var includePaths = new List<string> { "comments" };

        // Act
        var (mainFilters, includeFilters) = IncludeFilterParser.SeparateIncludeFilters(
            filters,
            includePaths
        );

        // Assert
        Assert.NotNull(mainFilters);
        Assert.Single(mainFilters.Filters);
        Assert.Equal("title", mainFilters.Filters[0].Field);

        Assert.Single(includeFilters);
        Assert.Equal("comments", includeFilters[0].RelationshipPath);
        // The OR group should be used directly (not wrapped)
        Assert.Equal(LogicalOperator.Or, includeFilters[0].FilterGroup.LogicalOperator);
        Assert.Equal(2, includeFilters[0].FilterGroup.Filters.Count);
        Assert.All(includeFilters[0].FilterGroup.Filters, f => Assert.Equal("status", f.Field));
    }

    [Fact]
    public void SeparateIncludeFilters_WithDeepNestedIncludeFilterUsingLeafName_SeparatesCorrectly()
    {
        // Arrange - This tests the scenario: include=cve,cve.cvecomments&filter[cvecomments][companyCode][eq]=AA
        var filters = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new()
                {
                    Field = "cvecomments.companyCode",
                    Operator = FilterOperator.Eq,
                    Value = "AA",
                    IsIncludeFilter = true, // Bracket syntax: filter[cvecomments][companyCode][eq]=AA
                },
            },
        };
        var includePaths = new List<string> { "cve", "cve.cvecomments" };

        // Act
        var (mainFilters, includeFilters) = IncludeFilterParser.SeparateIncludeFilters(
            filters,
            includePaths
        );

        // Assert
        Assert.Null(mainFilters); // No main filters expected
        Assert.Single(includeFilters);
        Assert.Equal("cve.cvecomments", includeFilters[0].RelationshipPath);
        Assert.Single(includeFilters[0].FilterGroup.Filters);
        Assert.Equal("companyCode", includeFilters[0].FilterGroup.Filters[0].Field);
        Assert.Equal("AA", includeFilters[0].FilterGroup.Filters[0].Value);
    }

    [Fact]
    public void SeparateIncludeFilters_WithDeepNestedIncludeFilterUsingKebabCase_SeparatesCorrectly()
    {
        // Arrange - Similar test with kebab-case include path
        var filters = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new()
                {
                    Field = "cveComments.companyCode",
                    Operator = FilterOperator.Eq,
                    Value = "AA",
                    IsIncludeFilter = true, // Bracket syntax: filter[cveComments][companyCode][eq]=AA
                },
            },
        };
        var includePaths = new List<string> { "cve", "cve.cve-comments" };

        // Act
        var (mainFilters, includeFilters) = IncludeFilterParser.SeparateIncludeFilters(
            filters,
            includePaths
        );

        // Assert
        Assert.Null(mainFilters);
        Assert.Single(includeFilters);
        // The relationship path is returned as the matched normalized path
        Assert.Equal("cve.cveComments", includeFilters[0].RelationshipPath);
        Assert.Single(includeFilters[0].FilterGroup.Filters);
        Assert.Equal("companyCode", includeFilters[0].FilterGroup.Filters[0].Field);
    }

    [Fact]
    public void SeparateIncludeFilters_WithMultipleDeepNestedFilters_SeparatesCorrectly()
    {
        // Arrange - Multiple filters on deep nested includes
        var filters = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new()
                {
                    Field = "author.name",
                    Operator = FilterOperator.Eq,
                    Value = "John",
                    IsIncludeFilter = true, // Bracket syntax
                },
                new()
                {
                    Field = "comments.status",
                    Operator = FilterOperator.Eq,
                    Value = "approved",
                    IsIncludeFilter = true, // Bracket syntax
                },
            },
        };
        var includePaths = new List<string> { "posts.author", "posts.comments" };

        // Act
        var (mainFilters, includeFilters) = IncludeFilterParser.SeparateIncludeFilters(
            filters,
            includePaths
        );

        // Assert
        Assert.Null(mainFilters);
        Assert.Equal(2, includeFilters.Count);

        var authorFilter = includeFilters.First(f => f.RelationshipPath == "posts.author");
        Assert.Equal("posts.author", authorFilter.RelationshipPath);
        Assert.Single(authorFilter.FilterGroup.Filters);
        Assert.Equal("name", authorFilter.FilterGroup.Filters[0].Field);
        Assert.Equal("John", authorFilter.FilterGroup.Filters[0].Value);

        var commentFilter = includeFilters.First(f => f.RelationshipPath == "posts.comments");
        Assert.Equal("posts.comments", commentFilter.RelationshipPath);
        Assert.Single(commentFilter.FilterGroup.Filters);
        Assert.Equal("status", commentFilter.FilterGroup.Filters[0].Field);
        Assert.Equal("approved", commentFilter.FilterGroup.Filters[0].Value);
    }
}

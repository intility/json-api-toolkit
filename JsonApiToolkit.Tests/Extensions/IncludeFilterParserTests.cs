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
                    Field = "comments.status",
                    Operator = FilterOperator.Eq,
                    Value = "approved",
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
        Assert.Equal("status", includeFilters[0].FieldPath);
        Assert.Equal("approved", includeFilters[0].Filter.Value);
    }

    [Fact]
    public void SeparateIncludeFilters_WithKebabCaseInclude_HandlesCorrectly()
    {
        // Arrange
        var filters = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new()
                {
                    Field = "cveComments.companyCode",
                    Operator = FilterOperator.Eq,
                    Value = "AA",
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
        Assert.Equal("companyCode", includeFilters[0].FieldPath);
    }

    [Fact]
    public void SeparateIncludeFilters_WithNestedIncludeFilter_SeparatesCorrectly()
    {
        // Arrange
        var filters = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new()
                {
                    Field = "comments.author.department",
                    Operator = FilterOperator.Eq,
                    Value = "Security",
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
        Assert.Equal("department", includeFilters[0].FieldPath);
    }

    [Fact]
    public void SeparateIncludeFilters_WithComplexOrFilter_HandlesCorrectly()
    {
        // Arrange
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
                },
                new()
                {
                    Field = "comments.companyCode",
                    Operator = FilterOperator.IsNull,
                    Value = "true",
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
        Assert.Equal(2, includeFilters.Count);
        Assert.All(includeFilters, f => Assert.Equal("comments", f.RelationshipPath));
        Assert.All(includeFilters, f => Assert.Equal("companyCode", f.FieldPath));
    }

    [Fact]
    public void SeparateIncludeFilters_WithFilterOnNonIncludedRelationship_ReturnsAsMainFilter()
    {
        // Arrange
        var filters = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new()
                {
                    Field = "comments.status",
                    Operator = FilterOperator.Eq,
                    Value = "approved",
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
        // When the relationship is not included, the filter should be treated as a main filter with dot notation
        Assert.NotNull(mainFilters);
        Assert.Single(mainFilters.Filters);
        Assert.Equal("comments.status", mainFilters.Filters[0].Field);
        Assert.Empty(includeFilters);
    }

    [Fact]
    public void SeparateIncludeFilters_WithTooManyOrConditions_ThrowsException()
    {
        // Arrange
        var filters = new FilterGroup
        {
            LogicalOperator = LogicalOperator.Or,
            Filters = new List<FilterParameter>(),
        };

        // Add 11 OR conditions (exceeds limit of 10)
        for (int i = 0; i < 11; i++)
        {
            filters.Filters.Add(
                new FilterParameter
                {
                    Field = "comments.status",
                    Operator = FilterOperator.Eq,
                    Value = $"value{i}",
                }
            );
        }

        var includePaths = new List<string> { "comments" };

        // Act & Assert
        var exception = Assert.Throws<JsonApiBadRequestException>(
            () => IncludeFilterParser.SeparateIncludeFilters(filters, includePaths)
        );

        Assert.Contains("Too many OR conditions", exception.Message);
    }

    [Fact]
    public void SeparateIncludeFilters_WithTooDeepNesting_ThrowsException()
    {
        // Arrange
        var filters = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new()
                {
                    Field = "a.b.c.d.e",
                    Operator = FilterOperator.Eq,
                    Value = "test",
                }, // 5 levels deep
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
        // Arrange
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
        Assert.Equal("approved", includeFilters[0].FieldPath);
    }

    [Fact]
    public void SeparateIncludeFilters_WithNestedGroups_HandlesCorrectly()
    {
        // Arrange
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
                        },
                        new()
                        {
                            Field = "comments.status",
                            Operator = FilterOperator.Eq,
                            Value = "pending",
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

        Assert.Equal(2, includeFilters.Count);
        Assert.All(includeFilters, f => Assert.Equal("comments", f.RelationshipPath));
        Assert.All(includeFilters, f => Assert.Equal("status", f.FieldPath));
    }
}

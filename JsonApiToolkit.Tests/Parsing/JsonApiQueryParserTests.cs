using JsonApiToolkit.Helpers;
using JsonApiToolkit.Models.Querying;
using JsonApiToolkit.Models.Querying.Filtering;
using JsonApiToolkit.Parsing;
using Microsoft.AspNetCore.Http;

namespace JsonApiToolkit.Tests.Parsing;

public class JsonApiFilterParserTests
{
    [Fact]
    public void ParseComplexFilter_WithDotNotation_CreatesPrimaryFilter()
    {
        // Arrange - Dot notation: filter[rel.field][op]=value
        var group = new FilterGroup();
        string key = "filter[vulnerability.severity][eq]";
        string value = "Critical";

        // Act
        JsonApiFilterParser.ParseComplexFilter(key, value, group);

        // Assert
        Assert.Single(group.Filters);
        var filter = group.Filters[0];
        Assert.Equal("vulnerability.severity", filter.Field);
        Assert.Equal(FilterOperator.Eq, filter.Operator);
        Assert.Equal("Critical", filter.Value);
        Assert.False(filter.IsIncludeFilter); // Dot notation = primary filter
    }

    [Fact]
    public void ParseComplexFilter_WithBracketSyntax_CreatesIncludeFilter()
    {
        // Arrange - Bracket syntax: filter[rel][field][op]=value
        var group = new FilterGroup();
        string key = "filter[vulnerability][severity][eq]";
        string value = "Critical";

        // Act
        JsonApiFilterParser.ParseComplexFilter(key, value, group);

        // Assert
        Assert.Single(group.Filters);
        var filter = group.Filters[0];
        Assert.Equal("vulnerability.severity", filter.Field); // Combined for downstream
        Assert.Equal(FilterOperator.Eq, filter.Operator);
        Assert.Equal("Critical", filter.Value);
        Assert.True(filter.IsIncludeFilter); // Bracket syntax = include filter
    }

    [Fact]
    public void ParseComplexFilter_WithSimpleFilter_CreatesPrimaryFilter()
    {
        // Arrange - Simple filter: filter[field][op]=value
        var group = new FilterGroup();
        string key = "filter[status][eq]";
        string value = "Active";

        // Act
        JsonApiFilterParser.ParseComplexFilter(key, value, group);

        // Assert
        Assert.Single(group.Filters);
        var filter = group.Filters[0];
        Assert.Equal("status", filter.Field);
        Assert.Equal(FilterOperator.Eq, filter.Operator);
        Assert.Equal("Active", filter.Value);
        Assert.False(filter.IsIncludeFilter);
    }

    [Fact]
    public void ParseComplexFilter_WithAllOperators_ParsesCorrectly()
    {
        var testCases = new[]
        {
            ("eq", FilterOperator.Eq),
            ("ne", FilterOperator.Ne),
            ("gt", FilterOperator.Gt),
            ("ge", FilterOperator.Ge),
            ("lt", FilterOperator.Lt),
            ("le", FilterOperator.Le),
            ("like", FilterOperator.Like),
            ("in", FilterOperator.In),
            ("nin", FilterOperator.Nin),
            ("isnull", FilterOperator.IsNull),
            ("isnotnull", FilterOperator.IsNotNull),
        };

        foreach (var (opStr, expectedOp) in testCases)
        {
            var group = new FilterGroup();
            JsonApiFilterParser.ParseComplexFilter($"filter[field][{opStr}]", "value", group);

            Assert.Single(group.Filters);
            Assert.Equal(expectedOp, group.Filters[0].Operator);
        }
    }

    [Fact]
    public void ParseComplexFilter_WithNestedBracketSyntax_CreatesIncludeFilter()
    {
        // Arrange - Nested bracket syntax: filter[rel][nestedField][op]=value
        var group = new FilterGroup();
        string key = "filter[comments][author.name][eq]";
        string value = "John";

        // Act
        JsonApiFilterParser.ParseComplexFilter(key, value, group);

        // Assert
        Assert.Single(group.Filters);
        var filter = group.Filters[0];
        Assert.Equal("comments.author.name", filter.Field);
        Assert.Equal(FilterOperator.Eq, filter.Operator);
        Assert.True(filter.IsIncludeFilter);
    }
}

public class JsonApiQueryParserTests
{
    [Fact]
    public void Parse_WithPagination_ReturnsPaginationParameters()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.QueryString = new QueryString("?page[number]=2&page[size]=25");

        QueryParameters parameters = JsonApiQueryParser.Parse(httpContext.Request);

        Assert.NotNull(parameters.Pagination);
        Assert.Equal(2, parameters.Pagination.Number);
        Assert.Equal(25, parameters.Pagination.Size);
    }

    [Fact]
    public void Parse_WithInvalidPagination_UsesDefaultValues()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.QueryString = new QueryString("?page[number]=-5&page[size]=1000");

        QueryParameters parameters = JsonApiQueryParser.Parse(httpContext.Request);

        Assert.NotNull(parameters.Pagination);
        Assert.Equal(1, parameters.Pagination.Number);
        Assert.Equal(100, parameters.Pagination.Size);
    }

    [Fact]
    public void Parse_WithFilters_ReturnsFilterParameters()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.QueryString = new QueryString("?filter[name]=Test&filter[age][gt]=18");

        httpContext.Request.Query = new QueryCollection(
            new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                ["filter[name]"] = "Test",
                ["filter[age][gt]"] = "18",
            }
        );

        QueryParameters parameters = JsonApiQueryParser.Parse(httpContext.Request);

        Assert.NotNull(parameters.Filter);
        Assert.Equal(2, parameters.Filter.Filters.Count);

        FilterParameter? nameFilter = parameters.Filter.Filters.FirstOrDefault(f =>
            f.Field == "name"
        );
        Assert.NotNull(nameFilter);
        Assert.Equal(FilterOperator.Eq, nameFilter.Operator);
        Assert.Equal("Test", nameFilter.Value);

        FilterParameter? ageFilter = parameters.Filter.Filters.FirstOrDefault(f =>
            f.Field == "age"
        );
        Assert.NotNull(ageFilter);
        Assert.Equal(FilterOperator.Gt, ageFilter.Operator);
        Assert.Equal("18", ageFilter.Value);
    }

    [Fact]
    public void Parse_WithSort_ReturnsSortParameters()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.QueryString = new QueryString("?sort=name,-age");

        httpContext.Request.Query = new QueryCollection(
            new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                ["sort"] = "name,-age",
            }
        );

        QueryParameters parameters = JsonApiQueryParser.Parse(httpContext.Request);

        Assert.NotNull(parameters.Sort);
        Assert.Equal(2, parameters.Sort.Count);

        Assert.Equal("name", parameters.Sort[0].Field);
        Assert.False(parameters.Sort[0].IsDescending);

        Assert.Equal("age", parameters.Sort[1].Field);
        Assert.True(parameters.Sort[1].IsDescending);
    }

    [Fact]
    public void Parse_WithDotNotationFilter_CreatesPrimaryFilter()
    {
        // Dot notation: filter[rel.field][op]=value creates primary filter
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Query = new QueryCollection(
            new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                ["filter[vulnerability.severity][eq]"] = "Critical",
            }
        );

        QueryParameters parameters = JsonApiQueryParser.Parse(httpContext.Request);

        Assert.NotNull(parameters.Filter);
        Assert.Single(parameters.Filter.Filters);

        var filter = parameters.Filter.Filters[0];
        Assert.Equal("vulnerability.severity", filter.Field);
        Assert.Equal(FilterOperator.Eq, filter.Operator);
        Assert.Equal("Critical", filter.Value);
        Assert.False(filter.IsIncludeFilter); // Primary filter
    }

    [Fact]
    public void Parse_WithBracketSyntaxFilter_CreatesIncludeFilter()
    {
        // Bracket syntax: filter[rel][field][op]=value creates include filter
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Query = new QueryCollection(
            new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                ["filter[vulnerability][severity][eq]"] = "Critical",
            }
        );

        QueryParameters parameters = JsonApiQueryParser.Parse(httpContext.Request);

        Assert.NotNull(parameters.Filter);
        Assert.Single(parameters.Filter.Filters);

        var filter = parameters.Filter.Filters[0];
        Assert.Equal("vulnerability.severity", filter.Field);
        Assert.Equal(FilterOperator.Eq, filter.Operator);
        Assert.Equal("Critical", filter.Value);
        Assert.True(filter.IsIncludeFilter); // Include filter
    }

    [Fact]
    public void Parse_WithMixedFilterSyntax_CorrectlyIdentifiesFilterTypes()
    {
        // Mix of primary and include filters
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Query = new QueryCollection(
            new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                ["filter[status][eq]"] = "Active", // Primary filter (simple)
                ["filter[vulnerability.severity][eq]"] = "Critical", // Primary filter (dot notation)
                ["filter[comments][status][eq]"] = "approved", // Include filter (bracket syntax)
            }
        );

        QueryParameters parameters = JsonApiQueryParser.Parse(httpContext.Request);

        Assert.NotNull(parameters.Filter);
        Assert.Equal(3, parameters.Filter.Filters.Count);

        var statusFilter = parameters.Filter.Filters.First(f => f.Field == "status");
        Assert.False(statusFilter.IsIncludeFilter);

        var vulnFilter = parameters.Filter.Filters.First(f => f.Field == "vulnerability.severity");
        Assert.False(vulnFilter.IsIncludeFilter);

        var commentsFilter = parameters.Filter.Filters.First(f => f.Field == "comments.status");
        Assert.True(commentsFilter.IsIncludeFilter);
    }

    [Fact]
    public void Parse_WithOrChainDotNotation_CreatesPrimaryFilters()
    {
        // OR chain with dot notation: filter[or][0][rel.field][op]=value
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Query = new QueryCollection(
            new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                ["filter[or][0][vulnerability.severity][eq]"] = "Critical",
                ["filter[or][1][vulnerability.severity][eq]"] = "High",
            }
        );

        QueryParameters parameters = JsonApiQueryParser.Parse(httpContext.Request);

        Assert.NotNull(parameters.Filter);
        Assert.Single(parameters.Filter.Groups); // One OR group

        var orGroup = parameters.Filter.Groups[0];
        Assert.Equal(LogicalOperator.Or, orGroup.LogicalOperator);
        Assert.Equal(2, orGroup.Filters.Count);

        // Both should be primary filters (dot notation)
        Assert.All(orGroup.Filters, f =>
        {
            Assert.Equal("vulnerability.severity", f.Field);
            Assert.False(f.IsIncludeFilter);
        });
    }

    [Fact]
    public void Parse_WithOrChainBracketSyntax_CreatesIncludeFilters()
    {
        // OR chain with bracket syntax: filter[or][0][rel][field][op]=value
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Query = new QueryCollection(
            new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                ["filter[or][0][vulnerability][severity][eq]"] = "Critical",
                ["filter[or][1][vulnerability][severity][eq]"] = "High",
            }
        );

        QueryParameters parameters = JsonApiQueryParser.Parse(httpContext.Request);

        Assert.NotNull(parameters.Filter);
        Assert.Single(parameters.Filter.Groups);

        var orGroup = parameters.Filter.Groups[0];
        Assert.Equal(LogicalOperator.Or, orGroup.LogicalOperator);
        Assert.Equal(2, orGroup.Filters.Count);

        // Both should be include filters (bracket syntax)
        Assert.All(orGroup.Filters, f =>
        {
            Assert.Equal("vulnerability.severity", f.Field);
            Assert.True(f.IsIncludeFilter);
        });
    }

    [Fact]
    public void Parse_WithOrChainMixedSyntax_CorrectlyIdentifiesFilterTypes()
    {
        // Mix of primary and include filters in OR chain
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Query = new QueryCollection(
            new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                ["filter[or][0][vulnerability.severity][eq]"] = "Critical", // Primary (dot)
                ["filter[or][1][comments][status][eq]"] = "approved", // Include (bracket)
            }
        );

        QueryParameters parameters = JsonApiQueryParser.Parse(httpContext.Request);

        Assert.NotNull(parameters.Filter);
        Assert.Single(parameters.Filter.Groups);

        var orGroup = parameters.Filter.Groups[0];
        Assert.Equal(2, orGroup.Filters.Count);

        var primaryFilter = orGroup.Filters.First(f => f.Field == "vulnerability.severity");
        Assert.False(primaryFilter.IsIncludeFilter);

        var includeFilter = orGroup.Filters.First(f => f.Field == "comments.status");
        Assert.True(includeFilter.IsIncludeFilter);
    }
}

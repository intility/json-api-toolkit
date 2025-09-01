using JsonApiToolkit.Helpers;
using JsonApiToolkit.Models.Querying;
using JsonApiToolkit.Models.Querying.Filtering;
using JsonApiToolkit.Parsing;
using Microsoft.AspNetCore.Http;

namespace JsonApiToolkit.Tests.Parsing;

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
}

using JsonApiToolkit.Configuration;
using JsonApiToolkit.Models.Errors;
using JsonApiToolkit.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace JsonApiToolkit.Tests.Services;

public class JsonApiQueryParserServiceTests
{
    private static JsonApiQueryParserService CreateService(JsonApiOptions? options = null)
    {
        options ??= new JsonApiOptions();
        return new JsonApiQueryParserService(
            NullLogger<JsonApiQueryParserService>.Instance,
            Options.Create(options)
        );
    }

    private static HttpRequest CreateRequest(Dictionary<string, StringValues> query)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Query = new QueryCollection(query);
        return httpContext.Request;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // MaxFilters enforcement
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_WithFiltersBelowLimit_ReturnsParameters()
    {
        var service = CreateService(new JsonApiOptions { MaxFilters = 5 });
        var request = CreateRequest(
            new Dictionary<string, StringValues>
            {
                ["filter[name]"] = "test",
                ["filter[age]"] = "25",
            }
        );

        var result = service.Parse(request);

        Assert.NotNull(result.Filter);
        Assert.Equal(2, result.Filter.Filters.Count);
    }

    [Fact]
    public void Parse_WithFiltersExceedingLimit_ThrowsBadRequest()
    {
        var service = CreateService(new JsonApiOptions { MaxFilters = 2 });
        var request = CreateRequest(
            new Dictionary<string, StringValues>
            {
                ["filter[a]"] = "1",
                ["filter[b]"] = "2",
                ["filter[c]"] = "3",
            }
        );

        var exception = Assert.Throws<JsonApiBadRequestException>(() => service.Parse(request));

        Assert.Contains("3 filters", exception.Message);
        Assert.Contains("maximum allowed is 2", exception.Message);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // MaxFilterGroups enforcement
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_WithOrGroupsExceedingLimit_ThrowsBadRequest()
    {
        var service = CreateService(new JsonApiOptions { MaxFilterGroups = 1 });
        var request = CreateRequest(
            new Dictionary<string, StringValues>
            {
                ["filter[or][0][name][eq]"] = "John",
                ["filter[not][0][deleted][eq]"] = "true",
            }
        );

        var exception = Assert.Throws<JsonApiBadRequestException>(() => service.Parse(request));

        Assert.Contains("filter groups", exception.Message);
        Assert.Contains("maximum allowed is 1", exception.Message);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // MaxFilterValueLength enforcement
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_WithFilterValueExceedingLength_ThrowsBadRequest()
    {
        var service = CreateService(new JsonApiOptions { MaxFilterValueLength = 10 });
        var request = CreateRequest(
            new Dictionary<string, StringValues>
            {
                ["filter[search]"] = "this is a very long search value",
            }
        );

        var exception = Assert.Throws<JsonApiBadRequestException>(() => service.Parse(request));

        Assert.Contains("search", exception.Message);
        Assert.Contains("maximum allowed is 10", exception.Message);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // MaxIncludeDepth enforcement
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_WithIncludesBelowDepthLimit_ReturnsParameters()
    {
        var service = CreateService(new JsonApiOptions { MaxIncludeDepth = 3 });
        var request = CreateRequest(
            new Dictionary<string, StringValues> { ["include"] = "author,author.posts,comments" }
        );

        var result = service.Parse(request);

        Assert.NotNull(result.Include);
        Assert.Equal(3, result.Include.Count);
    }

    [Fact]
    public void Parse_WithIncludesExceedingDepthLimit_ThrowsBadRequest()
    {
        var service = CreateService(new JsonApiOptions { MaxIncludeDepth = 2 });
        var request = CreateRequest(
            new Dictionary<string, StringValues> { ["include"] = "author.posts.comments" }
        );

        var exception = Assert.Throws<JsonApiBadRequestException>(() => service.Parse(request));

        Assert.Contains("author.posts.comments", exception.Message);
        Assert.Contains("depth 3", exception.Message);
        Assert.Contains("maximum allowed is 2", exception.Message);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Pagination with configured limits
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_WithPageSizeExceedingMax_ClampsToConfiguredMax()
    {
        var service = CreateService(new JsonApiOptions { MaxPageSize = 50 });
        var request = CreateRequest(
            new Dictionary<string, StringValues> { ["page[number]"] = "1", ["page[size]"] = "1000" }
        );

        var result = service.Parse(request);

        Assert.NotNull(result.Pagination);
        Assert.Equal(50, result.Pagination.Size); // Clamped to configured max
    }

    [Fact]
    public void Parse_WithoutPageSize_UsesConfiguredDefault()
    {
        var service = CreateService(new JsonApiOptions { DefaultPageSize = 25 });
        var request = CreateRequest(
            new Dictionary<string, StringValues> { ["page[number]"] = "1" }
        );

        var result = service.Parse(request);

        Assert.NotNull(result.Pagination);
        Assert.Equal(25, result.Pagination.Size); // Uses configured default
    }

    [Fact]
    public void Parse_WithCustomMaxPageSize_AllowsLargerValues()
    {
        var service = CreateService(new JsonApiOptions { MaxPageSize = 500 });
        var request = CreateRequest(
            new Dictionary<string, StringValues> { ["page[number]"] = "1", ["page[size]"] = "300" }
        );

        var result = service.Parse(request);

        Assert.NotNull(result.Pagination);
        Assert.Equal(300, result.Pagination.Size);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Default limits
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_WithDefaultOptions_UsesDefaultPageSize()
    {
        var service = CreateService(); // Default options
        var request = CreateRequest(
            new Dictionary<string, StringValues> { ["page[number]"] = "2" }
        );

        var result = service.Parse(request);

        Assert.NotNull(result.Pagination);
        Assert.Equal(10, result.Pagination.Size); // Default is 10
    }

    [Fact]
    public void Parse_WithDefaultOptions_ClampsToDefaultMaxPageSize()
    {
        var service = CreateService(); // Default options (MaxPageSize = 100)
        var request = CreateRequest(
            new Dictionary<string, StringValues> { ["page[number]"] = "1", ["page[size]"] = "500" }
        );

        var result = service.Parse(request);

        Assert.NotNull(result.Pagination);
        Assert.Equal(100, result.Pagination.Size); // Clamped to default max (100)
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Combined limits
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_WithMultipleLimitsExceeded_ThrowsOnFirstViolation()
    {
        // When multiple limits are exceeded, the first one checked should throw
        var service = CreateService(new JsonApiOptions { MaxFilters = 1, MaxIncludeDepth = 1 });
        var request = CreateRequest(
            new Dictionary<string, StringValues>
            {
                ["filter[a]"] = "1",
                ["filter[b]"] = "2", // Exceeds MaxFilters
                ["include"] = "author.posts", // Exceeds MaxIncludeDepth
            }
        );

        // Include depth is validated during parsing (before post-parse filter count check)
        var exception = Assert.Throws<JsonApiBadRequestException>(() => service.Parse(request));
        Assert.Contains("Include path", exception.Message);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Sparse Fieldsets
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_WithValidFields_ParsesFieldsThroughService()
    {
        var service = CreateService();
        var request = CreateRequest(
            new Dictionary<string, StringValues>
            {
                ["fields[articles]"] = "title,content",
                ["fields[authors]"] = "name",
            }
        );

        var result = service.Parse(request);

        Assert.NotNull(result.Fields);
        Assert.Equal(2, result.Fields.Count);
        Assert.Equal(2, result.Fields["articles"].Count);
        Assert.Single(result.Fields["authors"]);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Disabled limits (int.MaxValue)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_WithDisabledFilterLimit_AllowsManyFilters()
    {
        var service = CreateService(new JsonApiOptions { MaxFilters = int.MaxValue });
        var filters = new Dictionary<string, StringValues>();
        for (int i = 0; i < 100; i++)
        {
            filters[$"filter[field{i}]"] = $"value{i}";
        }
        var request = CreateRequest(filters);

        var result = service.Parse(request);

        Assert.NotNull(result.Filter);
        Assert.Equal(100, result.Filter.Filters.Count);
    }

    [Fact]
    public void Parse_WithDisabledIncludeDepth_AllowsDeeplyNestedIncludes()
    {
        var service = CreateService(new JsonApiOptions { MaxIncludeDepth = int.MaxValue });
        var request = CreateRequest(
            new Dictionary<string, StringValues>
            {
                ["include"] = "a.b.c.d.e.f.g.h.i.j", // depth 10
            }
        );

        var result = service.Parse(request);

        Assert.NotNull(result.Include);
        Assert.Single(result.Include);
    }
}

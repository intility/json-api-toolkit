using JsonApiToolkit.Configuration;
using JsonApiToolkit.Models.Errors;
using JsonApiToolkit.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace JsonApiToolkit.Tests.Parsing;

public class StrictPaginationTests
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
    // Default behavior (StrictPagination = false)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Default_PageNumberZero_ClampedToOne()
    {
        var service = CreateService();
        var request = CreateRequest(
            new Dictionary<string, StringValues> { ["page[number]"] = "0" }
        );

        var result = service.Parse(request);

        Assert.NotNull(result.Pagination);
        Assert.Equal(1, result.Pagination.Number);
    }

    [Fact]
    public void Default_PageSizeExceedsMax_ClampedToMax()
    {
        var service = CreateService(new JsonApiOptions { MaxPageSize = 50 });
        var request = CreateRequest(
            new Dictionary<string, StringValues> { ["page[size]"] = "200" }
        );

        var result = service.Parse(request);

        Assert.NotNull(result.Pagination);
        Assert.Equal(50, result.Pagination.Size);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Strict mode (StrictPagination = true)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Strict_PageNumberZero_Throws400()
    {
        var service = CreateService(new JsonApiOptions { StrictPagination = true });
        var request = CreateRequest(
            new Dictionary<string, StringValues> { ["page[number]"] = "0" }
        );

        var ex = Assert.Throws<JsonApiBadRequestException>(() => service.Parse(request));
        Assert.Contains("Invalid page number", ex.Message);
    }

    [Fact]
    public void Strict_PageNumberNegative_Throws400()
    {
        var service = CreateService(new JsonApiOptions { StrictPagination = true });
        var request = CreateRequest(
            new Dictionary<string, StringValues> { ["page[number]"] = "-5" }
        );

        var ex = Assert.Throws<JsonApiBadRequestException>(() => service.Parse(request));
        Assert.Contains("Invalid page number", ex.Message);
    }

    [Fact]
    public void Strict_PageSizeZero_Throws400()
    {
        var service = CreateService(new JsonApiOptions { StrictPagination = true });
        var request = CreateRequest(new Dictionary<string, StringValues> { ["page[size]"] = "0" });

        var ex = Assert.Throws<JsonApiBadRequestException>(() => service.Parse(request));
        Assert.Contains("Invalid page size", ex.Message);
    }

    [Fact]
    public void Strict_PageSizeNegative_Throws400()
    {
        var service = CreateService(new JsonApiOptions { StrictPagination = true });
        var request = CreateRequest(
            new Dictionary<string, StringValues> { ["page[size]"] = "-10" }
        );

        var ex = Assert.Throws<JsonApiBadRequestException>(() => service.Parse(request));
        Assert.Contains("Invalid page size", ex.Message);
    }

    [Fact]
    public void Strict_PageSizeExceedsMax_Throws400()
    {
        var service = CreateService(
            new JsonApiOptions { StrictPagination = true, MaxPageSize = 50 }
        );
        var request = CreateRequest(
            new Dictionary<string, StringValues> { ["page[size]"] = "100" }
        );

        var ex = Assert.Throws<JsonApiBadRequestException>(() => service.Parse(request));
        Assert.Contains("exceeds maximum", ex.Message);
    }

    [Fact]
    public void Strict_ValidParameters_Works()
    {
        var service = CreateService(new JsonApiOptions { StrictPagination = true });
        var request = CreateRequest(
            new Dictionary<string, StringValues> { ["page[number]"] = "3", ["page[size]"] = "25" }
        );

        var result = service.Parse(request);

        Assert.NotNull(result.Pagination);
        Assert.Equal(3, result.Pagination.Number);
        Assert.Equal(25, result.Pagination.Size);
    }

    [Fact]
    public void Strict_NonParseablePageNumber_DefaultsToOne()
    {
        var service = CreateService(new JsonApiOptions { StrictPagination = true });
        var request = CreateRequest(
            new Dictionary<string, StringValues> { ["page[number]"] = "abc" }
        );

        // Non-parseable values are silently defaulted, not rejected
        var result = service.Parse(request);

        Assert.NotNull(result.Pagination);
        Assert.Equal(1, result.Pagination.Number);
    }

    [Fact]
    public void Strict_PageSizeAtMax_Works()
    {
        var service = CreateService(
            new JsonApiOptions { StrictPagination = true, MaxPageSize = 50 }
        );
        var request = CreateRequest(new Dictionary<string, StringValues> { ["page[size]"] = "50" });

        var result = service.Parse(request);

        Assert.NotNull(result.Pagination);
        Assert.Equal(50, result.Pagination.Size);
    }
}

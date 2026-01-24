using JsonApiToolkit.Attributes;
using JsonApiToolkit.Models.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;

namespace JsonApiToolkit.Tests.Attributes;

public class AllowedIncludesFilterPathTests
{
    private ActionExecutingContext CreateContext(
        Dictionary<string, StringValues>? queryParams = null,
        string? actionName = null
    )
    {
        var httpContext = new DefaultHttpContext();
        var services = new ServiceCollection();
        services.AddSingleton<ILogger<AllowedIncludesAttribute>>(
            Mock.Of<ILogger<AllowedIncludesAttribute>>()
        );
        httpContext.RequestServices = services.BuildServiceProvider();

        if (queryParams != null && queryParams.Count > 0)
        {
            httpContext.Request.Query = new QueryCollection(queryParams);
        }

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor { DisplayName = actionName ?? "TestAction" }
        );

        return new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            controller: new object()
        );
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Filter path validation - basic cases
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void OnActionExecuting_FilterOnAllowedRelationship_AllowsRequest()
    {
        var attribute = new AllowedIncludesAttribute("author", "posts");
        var context = CreateContext(
            new Dictionary<string, StringValues> { ["filter[author.name][eq]"] = "John" }
        );

        attribute.OnActionExecuting(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public void OnActionExecuting_FilterOnForbiddenRelationship_ReturnsForbidden()
    {
        var attribute = new AllowedIncludesAttribute("author");
        var context = CreateContext(
            new Dictionary<string, StringValues> { ["filter[admin.password][like]"] = "%" }
        );

        attribute.OnActionExecuting(context);

        Assert.NotNull(context.Result);
        var objectResult = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(403, objectResult.StatusCode);

        var errorResponse = Assert.IsType<JsonApiErrorResponse>(objectResult.Value);
        Assert.Single(errorResponse.Errors);
        Assert.Equal("403", errorResponse.Errors[0].Status);
        Assert.Equal(JsonApiErrorCodes.FilterNotAllowed, errorResponse.Errors[0].Code);
        Assert.Contains("admin", errorResponse.Errors[0].Detail);
    }

    [Fact]
    public void OnActionExecuting_SimpleFilterNoRelationship_AllowsRequest()
    {
        // Filters without dots (e.g., filter[name]) should always be allowed
        var attribute = new AllowedIncludesAttribute("author");
        var context = CreateContext(
            new Dictionary<string, StringValues> { ["filter[name][eq]"] = "test" }
        );

        attribute.OnActionExecuting(context);

        Assert.Null(context.Result);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Security vulnerability test cases
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void OnActionExecuting_FilterOnSensitiveRelationship_Blocked()
    {
        // This is the security hole we're fixing:
        // filter[admin.password][like]=% should be blocked when admin is not allowed
        var attribute = new AllowedIncludesAttribute("profile", "posts");
        var context = CreateContext(
            new Dictionary<string, StringValues>
            {
                ["filter[admin.secretKey][like]"] = "%",
                ["include"] = "profile", // Valid include
            }
        );

        attribute.OnActionExecuting(context);

        Assert.NotNull(context.Result);
        var objectResult = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(403, objectResult.StatusCode);

        var errorResponse = Assert.IsType<JsonApiErrorResponse>(objectResult.Value);
        Assert.Contains("admin", errorResponse.Errors[0].Detail);
    }

    [Fact]
    public void OnActionExecuting_FilterOnNestedForbiddenPath_Blocked()
    {
        // filter[author.admin.password] should check that author.admin is allowed
        var attribute = new AllowedIncludesAttribute("author");
        var context = CreateContext(
            new Dictionary<string, StringValues>
            {
                ["filter[author.admin.password][eq]"] = "secret",
            }
        );

        attribute.OnActionExecuting(context);

        Assert.NotNull(context.Result);
        var objectResult = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(403, objectResult.StatusCode);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Wildcard pattern tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void OnActionExecuting_FilterMatchingWildcard_AllowsRequest()
    {
        var attribute = new AllowedIncludesAttribute("author.*");
        var context = CreateContext(
            new Dictionary<string, StringValues> { ["filter[author.posts.title][eq]"] = "test" }
        );

        attribute.OnActionExecuting(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public void OnActionExecuting_FilterNotMatchingWildcard_Blocked()
    {
        var attribute = new AllowedIncludesAttribute("author.*");
        var context = CreateContext(
            new Dictionary<string, StringValues>
            {
                ["filter[comments.author.name][eq]"] = "John", // comments not allowed
            }
        );

        attribute.OnActionExecuting(context);

        Assert.NotNull(context.Result);
        var objectResult = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(403, objectResult.StatusCode);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Combined include and filter validation
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void OnActionExecuting_ValidIncludeInvalidFilter_ReturnsForbidden()
    {
        var attribute = new AllowedIncludesAttribute("author", "posts");
        var context = CreateContext(
            new Dictionary<string, StringValues>
            {
                ["include"] = "author",
                ["filter[admin.password][eq]"] = "secret",
            }
        );

        attribute.OnActionExecuting(context);

        Assert.NotNull(context.Result);
        var objectResult = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(403, objectResult.StatusCode);

        var errorResponse = Assert.IsType<JsonApiErrorResponse>(objectResult.Value);
        // Should be filter error, not include error
        Assert.Equal(JsonApiErrorCodes.FilterNotAllowed, errorResponse.Errors[0].Code);
    }

    [Fact]
    public void OnActionExecuting_InvalidIncludeValidFilter_ReturnsForbiddenInclude()
    {
        var attribute = new AllowedIncludesAttribute("author", "posts");
        var context = CreateContext(
            new Dictionary<string, StringValues>
            {
                ["include"] = "admin", // Invalid
                ["filter[author.name][eq]"] = "John", // Valid
            }
        );

        attribute.OnActionExecuting(context);

        Assert.NotNull(context.Result);
        var objectResult = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(403, objectResult.StatusCode);

        var errorResponse = Assert.IsType<JsonApiErrorResponse>(objectResult.Value);
        // Include validation runs first, so should be include error
        Assert.Equal(JsonApiErrorCodes.IncludeNotAllowed, errorResponse.Errors[0].Code);
    }

    [Fact]
    public void OnActionExecuting_BothValidIncludeAndFilter_AllowsRequest()
    {
        var attribute = new AllowedIncludesAttribute("author", "posts");
        var context = CreateContext(
            new Dictionary<string, StringValues>
            {
                ["include"] = "author,posts",
                ["filter[author.name][eq]"] = "John",
                ["filter[posts.title][like]"] = "%test%",
            }
        );

        attribute.OnActionExecuting(context);

        Assert.Null(context.Result);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Multiple forbidden filters
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void OnActionExecuting_MultipleInvalidFilters_ReturnsAllInError()
    {
        var attribute = new AllowedIncludesAttribute("author");
        var context = CreateContext(
            new Dictionary<string, StringValues>
            {
                ["filter[admin.password][eq]"] = "secret",
                ["filter[secrets.key][eq]"] = "value",
            }
        );

        attribute.OnActionExecuting(context);

        Assert.NotNull(context.Result);
        var objectResult = Assert.IsType<ObjectResult>(context.Result);
        var errorResponse = Assert.IsType<JsonApiErrorResponse>(objectResult.Value);

        var meta = errorResponse.Errors[0].Meta;
        Assert.NotNull(meta);

        var forbiddenPaths = meta["forbiddenFilterPaths"] as List<string>;
        Assert.NotNull(forbiddenPaths);
        Assert.Equal(2, forbiddenPaths.Count);
        Assert.Contains("admin", forbiddenPaths);
        Assert.Contains("secrets", forbiddenPaths);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Edge cases
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void OnActionExecuting_EmptyAllowedIncludes_AllowsSimpleFilters()
    {
        // Empty allowed includes means no relationships allowed
        // But simple filters (no dot) should still work
        var attribute = new AllowedIncludesAttribute();
        var context = CreateContext(
            new Dictionary<string, StringValues> { ["filter[name][eq]"] = "test" }
        );

        attribute.OnActionExecuting(context);

        // No includes requested, simple filter allowed
        Assert.Null(context.Result);
    }

    [Fact]
    public void OnActionExecuting_NoFilters_AllowsRequest()
    {
        var attribute = new AllowedIncludesAttribute("author");
        var context = CreateContext(
            new Dictionary<string, StringValues> { ["sort"] = "-createdAt" }
        );

        attribute.OnActionExecuting(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public void OnActionExecuting_CaseInsensitiveFilterPath_AllowsRequest()
    {
        var attribute = new AllowedIncludesAttribute("Author");
        var context = CreateContext(
            new Dictionary<string, StringValues> { ["filter[author.Name][eq]"] = "John" }
        );

        attribute.OnActionExecuting(context);

        Assert.Null(context.Result);
    }
}

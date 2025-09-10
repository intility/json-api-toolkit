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

public class AllowedIncludesAttributeWildcardTests
{
    private ActionExecutingContext CreateContext(string? includeQueryParam = null)
    {
        var httpContext = new DefaultHttpContext();
        var services = new ServiceCollection();
        services.AddSingleton<ILogger<AllowedIncludesAttribute>>(
            Mock.Of<ILogger<AllowedIncludesAttribute>>()
        );
        httpContext.RequestServices = services.BuildServiceProvider();

        if (includeQueryParam != null)
        {
            httpContext.Request.QueryString = new QueryString($"?include={includeQueryParam}");
            var queryCollection = new Dictionary<string, StringValues>
            {
                { "include", includeQueryParam },
            };
            httpContext.Request.Query = new QueryCollection(queryCollection);
        }

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor { DisplayName = "TestAction" }
        );

        var context = new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            controller: new object()
        );

        return context;
    }

    [Fact]
    public void OnActionExecuting_TopLevelWildcard_AllowsTopLevel()
    {
        var attribute = new AllowedIncludesAttribute("*");
        var context = CreateContext("author");

        attribute.OnActionExecuting(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public void OnActionExecuting_TopLevelWildcard_ForbidsNested()
    {
        var attribute = new AllowedIncludesAttribute("*");
        var context = CreateContext("author.posts");

        attribute.OnActionExecuting(context);

        Assert.NotNull(context.Result);
        var objectResult = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(403, objectResult.StatusCode);
    }

    [Fact]
    public void OnActionExecuting_SingleLevelWildcard_AllowsPrefix()
    {
        var attribute = new AllowedIncludesAttribute("author.*");
        var context = CreateContext("author");

        attribute.OnActionExecuting(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public void OnActionExecuting_SingleLevelWildcard_AllowsSingleLevel()
    {
        var attribute = new AllowedIncludesAttribute("author.*");
        var context = CreateContext("author.posts");

        attribute.OnActionExecuting(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public void OnActionExecuting_SingleLevelWildcard_ForbidsDeeperNesting()
    {
        var attribute = new AllowedIncludesAttribute("author.*");
        var context = CreateContext("author.posts.comments");

        attribute.OnActionExecuting(context);

        Assert.NotNull(context.Result);
        var objectResult = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(403, objectResult.StatusCode);
    }

    [Fact]
    public void OnActionExecuting_MixedPatterns_AllowsMatching()
    {
        var attribute = new AllowedIncludesAttribute("author.*", "posts", "comments.replies");
        var context = CreateContext("author.name,posts,comments");

        attribute.OnActionExecuting(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public void OnActionExecuting_MixedPatterns_ForbidsNonMatching()
    {
        var attribute = new AllowedIncludesAttribute("author.*", "posts");
        var context = CreateContext("author.posts,tags");

        attribute.OnActionExecuting(context);

        Assert.NotNull(context.Result);
        var objectResult = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(403, objectResult.StatusCode);

        var errorResponse = Assert.IsType<JsonApiErrorResponse>(objectResult.Value);
        var errorWithMeta = errorResponse.Errors[0] as JsonApiErrorWithMeta;
        Assert.NotNull(errorWithMeta);

        var forbiddenIncludes = errorWithMeta.Meta?["forbiddenIncludes"] as List<string>;
        Assert.NotNull(forbiddenIncludes);
        Assert.Single(forbiddenIncludes);
        Assert.Contains("tags", forbiddenIncludes);
    }

    [Fact]
    public void OnActionExecuting_WildcardCaseInsensitive()
    {
        var attribute = new AllowedIncludesAttribute("Author.*");
        var context = CreateContext("author.POSTS");

        attribute.OnActionExecuting(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public void OnActionExecuting_RealWorldExample_CVE()
    {
        var attribute = new AllowedIncludesAttribute("epss", "vulncheckkevs", "cve.*");

        // Allowed includes
        var context1 = CreateContext("epss,vulncheckkevs");
        attribute.OnActionExecuting(context1);
        Assert.Null(context1.Result);

        // Allowed with wildcard
        var context2 = CreateContext("cve.description");
        attribute.OnActionExecuting(context2);
        Assert.Null(context2.Result);

        // Forbidden
        var context3 = CreateContext("vulnerabilities");
        attribute.OnActionExecuting(context3);
        Assert.NotNull(context3.Result);
        var objectResult = Assert.IsType<ObjectResult>(context3.Result);
        Assert.Equal(403, objectResult.StatusCode);
    }

    [Fact]
    public void OnActionExecuting_MultipleWildcards()
    {
        var attribute = new AllowedIncludesAttribute("author.*", "posts.*", "*");

        // Top level
        var context1 = CreateContext("tags");
        attribute.OnActionExecuting(context1);
        Assert.Null(context1.Result);

        // Author wildcard
        var context2 = CreateContext("author.profile");
        attribute.OnActionExecuting(context2);
        Assert.Null(context2.Result);

        // Posts wildcard
        var context3 = CreateContext("posts.comments");
        attribute.OnActionExecuting(context3);
        Assert.Null(context3.Result);

        // Forbidden nested under top-level
        var context4 = CreateContext("tags.items");
        attribute.OnActionExecuting(context4);
        Assert.NotNull(context4.Result);
    }
}

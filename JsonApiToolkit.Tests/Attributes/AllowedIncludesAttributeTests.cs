using JsonApiToolkit.Attributes;
using JsonApiToolkit.Models.Errors;
using JsonApiToolkit.Models.Querying;
using JsonApiToolkit.Parsing;
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

public class AllowedIncludesAttributeTests
{
    private ActionExecutingContext CreateContext(
        string? includeQueryParam = null,
        string? actionName = null
    )
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
            new ActionDescriptor { DisplayName = actionName ?? "TestAction" }
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
    public void Constructor_WithNullArray_TreatsAsEmpty()
    {
        var attribute = new AllowedIncludesAttribute(null!);
        Assert.Empty(attribute.AllowedIncludes);
    }

    [Fact]
    public void Constructor_WithEmptyArray_StoresEmpty()
    {
        var attribute = new AllowedIncludesAttribute();
        Assert.Empty(attribute.AllowedIncludes);
    }

    [Fact]
    public void Constructor_WithIncludes_StoresIncludes()
    {
        var attribute = new AllowedIncludesAttribute("author", "posts");
        Assert.Equal(2, attribute.AllowedIncludes.Length);
        Assert.Contains("author", attribute.AllowedIncludes);
        Assert.Contains("posts", attribute.AllowedIncludes);
    }

    [Fact]
    public void OnActionExecuting_NoIncludeParam_AllowsRequest()
    {
        var attribute = new AllowedIncludesAttribute("author");
        var context = CreateContext();

        attribute.OnActionExecuting(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public void OnActionExecuting_EmptyIncludeParam_AllowsRequest()
    {
        var attribute = new AllowedIncludesAttribute("author");
        var context = CreateContext("");

        attribute.OnActionExecuting(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public void OnActionExecuting_AllowedInclude_AllowsRequest()
    {
        var attribute = new AllowedIncludesAttribute("author", "posts");
        var context = CreateContext("author");

        attribute.OnActionExecuting(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public void OnActionExecuting_MultipleAllowedIncludes_AllowsRequest()
    {
        var attribute = new AllowedIncludesAttribute("author", "posts", "comments");
        var context = CreateContext("author,posts");

        attribute.OnActionExecuting(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public void OnActionExecuting_ForbiddenInclude_ReturnsForbidden()
    {
        var attribute = new AllowedIncludesAttribute("author");
        var context = CreateContext("posts");

        attribute.OnActionExecuting(context);

        Assert.NotNull(context.Result);
        var objectResult = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(403, objectResult.StatusCode);

        var errorResponse = Assert.IsType<JsonApiErrorResponse>(objectResult.Value);
        Assert.Single(errorResponse.Errors);
        Assert.Equal("403", errorResponse.Errors[0].Status);
        Assert.Contains("posts", errorResponse.Errors[0].Detail);
    }

    [Fact]
    public void OnActionExecuting_EmptyAllowedArray_ForbidsAllIncludes()
    {
        var attribute = new AllowedIncludesAttribute();
        var context = CreateContext("author");

        attribute.OnActionExecuting(context);

        Assert.NotNull(context.Result);
        var objectResult = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(403, objectResult.StatusCode);
    }

    [Fact]
    public void OnActionExecuting_CaseInsensitive_AllowsRequest()
    {
        var attribute = new AllowedIncludesAttribute("Author");
        var context = CreateContext("author");

        attribute.OnActionExecuting(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public void OnActionExecuting_CaseInsensitiveReverse_AllowsRequest()
    {
        var attribute = new AllowedIncludesAttribute("author");
        var context = CreateContext("Author");

        attribute.OnActionExecuting(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public void OnActionExecuting_PartialPath_AllowsRequest()
    {
        var attribute = new AllowedIncludesAttribute("author.posts");
        var context = CreateContext("author");

        attribute.OnActionExecuting(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public void OnActionExecuting_FullPath_AllowsRequest()
    {
        var attribute = new AllowedIncludesAttribute("author.posts");
        var context = CreateContext("author.posts");

        attribute.OnActionExecuting(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public void OnActionExecuting_ExtendedPath_ForbidsRequest()
    {
        var attribute = new AllowedIncludesAttribute("author.posts");
        var context = CreateContext("author.posts.comments");

        attribute.OnActionExecuting(context);

        Assert.NotNull(context.Result);
        var objectResult = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(403, objectResult.StatusCode);
    }

    [Fact]
    public void OnActionExecuting_JsonApiCreatedAction_SkipsValidation()
    {
        var attribute = new AllowedIncludesAttribute("author");
        var context = CreateContext("forbidden", "JsonApiCreated");

        attribute.OnActionExecuting(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public void OnActionExecuting_MultipleForbidden_ReturnsAllInError()
    {
        var attribute = new AllowedIncludesAttribute("author");
        var context = CreateContext("posts,comments,tags");

        attribute.OnActionExecuting(context);

        Assert.NotNull(context.Result);
        var objectResult = Assert.IsType<ObjectResult>(context.Result);
        var errorResponse = Assert.IsType<JsonApiErrorResponse>(objectResult.Value);

        var error = errorResponse.Errors[0];
        var meta = error.Meta;
        Assert.NotNull(meta);

        var forbiddenIncludes = meta["forbiddenIncludes"] as List<string>;
        Assert.NotNull(forbiddenIncludes);
        Assert.Equal(3, forbiddenIncludes.Count);
        Assert.Contains("posts", forbiddenIncludes);
        Assert.Contains("comments", forbiddenIncludes);
        Assert.Contains("tags", forbiddenIncludes);
    }

    [Fact]
    public void OnActionExecuting_ErrorMetadata_ContainsCorrectInfo()
    {
        var attribute = new AllowedIncludesAttribute("author", "posts");
        var context = CreateContext("author,forbidden");

        attribute.OnActionExecuting(context);

        Assert.NotNull(context.Result);
        var objectResult = Assert.IsType<ObjectResult>(context.Result);
        var errorResponse = Assert.IsType<JsonApiErrorResponse>(objectResult.Value);

        var error = errorResponse.Errors[0];
        var meta = error.Meta;
        Assert.NotNull(meta);

        var requestedIncludes = meta["requestedIncludes"] as List<string>;
        Assert.NotNull(requestedIncludes);
        Assert.Equal(2, requestedIncludes.Count);
        Assert.Contains("author", requestedIncludes);
        Assert.Contains("forbidden", requestedIncludes);

        var forbiddenIncludes = meta["forbiddenIncludes"] as List<string>;
        Assert.NotNull(forbiddenIncludes);
        Assert.Single(forbiddenIncludes);
        Assert.Contains("forbidden", forbiddenIncludes);

        var allowedIncludes = meta["allowedIncludes"] as string[];
        Assert.NotNull(allowedIncludes);
        Assert.Equal(2, allowedIncludes.Length);
        Assert.Contains("author", allowedIncludes);
        Assert.Contains("posts", allowedIncludes);
    }
}

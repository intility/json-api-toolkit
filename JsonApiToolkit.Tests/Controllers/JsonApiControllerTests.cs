using JsonApiToolkit.Controllers;
using JsonApiToolkit.Models.Documents;
using JsonApiToolkit.Models.Errors;
using JsonApiToolkit.Models.Resources;
using JsonApiToolkit.Services;
using JsonApiToolkit.Tests.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace JsonApiToolkit.Tests.Controllers;

public class TestJsonApiController : JsonApiController
{
    public IActionResult TestJsonApiOk(TestEntity entity)
    {
        return JsonApiOk(entity, "testEntities");
    }

    public IActionResult TestJsonApiOkCollection(List<TestEntity> entities)
    {
        return JsonApiOk(entities, "testEntities", null);
    }

    public IActionResult TestJsonApiCreated(TestEntity entity)
    {
        return JsonApiCreated(entity, "testEntities", entity.Id.ToString());
    }

    public IActionResult TestJsonApiNotFound()
    {
        return JsonApiNotFound("Test entity not found");
    }

    public IActionResult TestJsonApiBadRequest()
    {
        return JsonApiBadRequest("Invalid test entity data");
    }
}

public class JsonApiControllerTests
{
    private readonly TestJsonApiController _controller;

    public JsonApiControllerTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<IJsonApiQueryParser>(provider =>
        {
            var mock = new Mock<IJsonApiQueryParser>();
            mock.Setup(x => x.Parse(It.IsAny<Microsoft.AspNetCore.Http.HttpRequest>()))
                .Returns(
                    new JsonApiToolkit.Models.Querying.QueryParameters
                    {
                        Include = new List<string>(),
                    }
                );
            return mock.Object;
        });

        var serviceProvider = services.BuildServiceProvider();
        _controller = new TestJsonApiController();

        var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("api.example.com");
        httpContext.Request.Path = "/test-entities";

        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    [Fact]
    public void JsonApiOk_WithSingleEntity_ReturnsCorrectResponse()
    {
        var entity = new TestEntity
        {
            Id = 1,
            Name = "Test Entity",
            Description = "Test Description",
        };

        var result = _controller.TestJsonApiOk(entity);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var document = Assert.IsType<JsonApiDocument<ResourceObject>>(okResult.Value);

        Assert.NotNull(document.Data);
        Assert.Equal("1", document.Data.Id);
        Assert.Equal("testEntities", document.Data.Type);
        Assert.NotNull(document.Data.Attributes);
        Assert.Equal("Test Entity", document.Data.Attributes["name"]);
    }

    [Fact]
    public void JsonApiOk_WithCollection_ReturnsCorrectResponse()
    {
        var entities = new List<TestEntity>
        {
            new TestEntity { Id = 1, Name = "Entity 1" },
            new TestEntity { Id = 2, Name = "Entity 2" },
        };

        var result = _controller.TestJsonApiOkCollection(entities);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var document = Assert.IsType<JsonApiCollectionDocument<ResourceObject>>(okResult.Value);

        Assert.NotNull(document.Data);
        Assert.Equal(2, document.Data.Count());

        var firstResource = document.Data.First();
        Assert.Equal("1", firstResource.Id);
        Assert.Equal("testEntities", firstResource.Type);
    }

    [Fact]
    public void JsonApiCreated_ReturnsCorrectResponse()
    {
        var entity = new TestEntity { Id = 1, Name = "Test Entity" };

        var result = _controller.TestJsonApiCreated(entity);

        var createdResult = Assert.IsType<CreatedResult>(result);
        Assert.Equal("https://api.example.com/test-entities/1", createdResult.Location);

        var document = Assert.IsType<JsonApiDocument<ResourceObject>>(createdResult.Value);
        Assert.NotNull(document.Data);
        Assert.Equal("1", document.Data.Id);
    }

    [Fact]
    public void JsonApiNotFound_ReturnsCorrectResponse()
    {
        var result = _controller.TestJsonApiNotFound();

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var errorResponse = Assert.IsType<JsonApiErrorResponse>(notFoundResult.Value);

        Assert.Single(errorResponse.Errors);
        Assert.Equal("404", errorResponse.Errors[0].Status);
        Assert.Equal("Not Found", errorResponse.Errors[0].Title);
        Assert.Equal("Test entity not found", errorResponse.Errors[0].Detail);
    }

    [Fact]
    public void JsonApiBadRequest_ReturnsCorrectResponse()
    {
        var result = _controller.TestJsonApiBadRequest();

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errorResponse = Assert.IsType<JsonApiErrorResponse>(badRequestResult.Value);

        Assert.Single(errorResponse.Errors);
        Assert.Equal("400", errorResponse.Errors[0].Status);
        Assert.Equal("Bad Request", errorResponse.Errors[0].Title);
        Assert.Equal("Invalid test entity data", errorResponse.Errors[0].Detail);
    }
}

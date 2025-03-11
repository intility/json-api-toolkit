using JsonApiToolkit.Filters;
using JsonApiToolkit.Models.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace JsonApiToolkit.Tests.Filters;

public class JsonApiExceptionFilterTests
{
    private readonly Mock<ILogger<JsonApiExceptionFilter>> _loggerMock;
    private readonly Mock<IHostEnvironment> _environmentMock;
    private readonly JsonApiExceptionFilter _filter;

    public JsonApiExceptionFilterTests()
    {
        _loggerMock = new Mock<ILogger<JsonApiExceptionFilter>>();
        _environmentMock = new Mock<IHostEnvironment>();
        _filter = new JsonApiExceptionFilter(_loggerMock.Object, _environmentMock.Object);
    }

    [Fact]
    public void OnException_InDevelopment_ReturnsExceptionDetails()
    {
        // Arrange
        _environmentMock.Setup(e => e.EnvironmentName).Returns("Development");

        var exception = new InvalidOperationException("Test exception message");
        var context = CreateExceptionContext(exception);

        // Act
        _filter.OnException(context);

        // Assert
        Assert.True(context.ExceptionHandled);
        var objectResult = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(500, objectResult.StatusCode);

        var errorResponse = Assert.IsType<JsonApiErrorResponse>(objectResult.Value);
        Assert.Single(errorResponse.Errors);
        Assert.Equal("500", errorResponse.Errors[0].Status);
        Assert.Equal("Internal Server Error", errorResponse.Errors[0].Title);
        Assert.Equal("Test exception message", errorResponse.Errors[0].Detail);
    }

    [Fact]
    public void OnException_InProduction_ReturnsGenericMessage()
    {
        // Arrange
        _environmentMock.Setup(e => e.EnvironmentName).Returns("Production");

        var exception = new InvalidOperationException("Test exception message");
        var context = CreateExceptionContext(exception);

        // Act
        _filter.OnException(context);

        // Assert
        Assert.True(context.ExceptionHandled);
        var objectResult = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(500, objectResult.StatusCode);

        var errorResponse = Assert.IsType<JsonApiErrorResponse>(objectResult.Value);
        Assert.Single(errorResponse.Errors);
        Assert.Equal("500", errorResponse.Errors[0].Status);
        Assert.Equal("Internal Server Error", errorResponse.Errors[0].Title);
        Assert.Equal(
            "An error occurred while processing your request.",
            errorResponse.Errors[0].Detail
        );
    }

    private static ExceptionContext CreateExceptionContext(Exception exception)
    {
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

        return new ExceptionContext(actionContext, new List<IFilterMetadata>())
        {
            Exception = exception,
        };
    }
}

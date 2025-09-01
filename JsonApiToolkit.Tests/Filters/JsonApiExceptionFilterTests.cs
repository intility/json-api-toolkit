using JsonApiToolkit.Filters;
using JsonApiToolkit.Models.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Moq;

namespace JsonApiToolkit.Tests.Filters;

public class JsonApiExceptionFilterTests
{
    private readonly Mock<ILogger<JsonApiExceptionFilter>> _mockLogger;
    private readonly JsonApiExceptionFilter _filter;

    public JsonApiExceptionFilterTests()
    {
        _mockLogger = new Mock<ILogger<JsonApiExceptionFilter>>();
        _filter = new JsonApiExceptionFilter(_mockLogger.Object);
    }

    private ExceptionContext CreateExceptionContext(Exception exception)
    {
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new Microsoft.AspNetCore.Routing.RouteData(), new ActionDescriptor());
        
        return new ExceptionContext(actionContext, [])
        {
            Exception = exception
        };
    }

    [Fact]
    public void OnException_WithJsonApiBadRequestException_Returns400()
    {
        var exception = new JsonApiBadRequestException("Invalid input data");
        var context = CreateExceptionContext(exception);

        _filter.OnException(context);

        Assert.True(context.ExceptionHandled);
        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(400, result.StatusCode);
        var errorResponse = Assert.IsType<JsonApiErrorResponse>(result.Value);
        
        Assert.Single(errorResponse.Errors);
        Assert.Equal("400", errorResponse.Errors[0].Status);
        Assert.Equal("Bad Request", errorResponse.Errors[0].Title);
        Assert.Equal("Invalid input data", errorResponse.Errors[0].Detail);
    }

    [Fact]
    public void OnException_WithJsonApiNotFoundException_Returns404()
    {
        var exception = new JsonApiNotFoundException("Resource not found");
        var context = CreateExceptionContext(exception);

        _filter.OnException(context);

        Assert.True(context.ExceptionHandled);
        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(404, result.StatusCode);
        var errorResponse = Assert.IsType<JsonApiErrorResponse>(result.Value);
        
        Assert.Single(errorResponse.Errors);
        Assert.Equal("404", errorResponse.Errors[0].Status);
        Assert.Equal("Not Found", errorResponse.Errors[0].Title);
        Assert.Equal("Resource not found", errorResponse.Errors[0].Detail);
    }

    [Fact]
    public void OnException_WithJsonApiConflictException_Returns409()
    {
        var exception = new JsonApiConflictException("Resource already exists");
        var context = CreateExceptionContext(exception);

        _filter.OnException(context);

        Assert.True(context.ExceptionHandled);
        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(409, result.StatusCode);
        var errorResponse = Assert.IsType<JsonApiErrorResponse>(result.Value);
        
        Assert.Single(errorResponse.Errors);
        Assert.Equal("409", errorResponse.Errors[0].Status);
        Assert.Equal("Conflict", errorResponse.Errors[0].Title);
        Assert.Equal("Resource already exists", errorResponse.Errors[0].Detail);
    }

    [Fact]
    public void OnException_WithJsonApiUnauthorizedException_Returns401()
    {
        var exception = new JsonApiUnauthorizedException("Authentication required");
        var context = CreateExceptionContext(exception);

        _filter.OnException(context);

        Assert.True(context.ExceptionHandled);
        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(401, result.StatusCode);
        var errorResponse = Assert.IsType<JsonApiErrorResponse>(result.Value);
        
        Assert.Single(errorResponse.Errors);
        Assert.Equal("401", errorResponse.Errors[0].Status);
        Assert.Equal("Unauthorized", errorResponse.Errors[0].Title);
        Assert.Equal("Authentication required", errorResponse.Errors[0].Detail);
    }

    [Fact]
    public void OnException_WithJsonApiForbiddenException_Returns403()
    {
        var exception = new JsonApiForbiddenException("Access denied");
        var context = CreateExceptionContext(exception);

        _filter.OnException(context);

        Assert.True(context.ExceptionHandled);
        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(403, result.StatusCode);
        
        var errorResponse = Assert.IsType<JsonApiErrorResponse>(result.Value);
        Assert.Single(errorResponse.Errors);
        Assert.Equal("403", errorResponse.Errors[0].Status);
        Assert.Equal("Forbidden", errorResponse.Errors[0].Title);
        Assert.Equal("Access denied", errorResponse.Errors[0].Detail);
    }

    [Fact]
    public void OnException_WithJsonApiTooManyRequestsException_Returns429()
    {
        var exception = new JsonApiTooManyRequestsException("Rate limit exceeded");
        var context = CreateExceptionContext(exception);

        _filter.OnException(context);

        Assert.True(context.ExceptionHandled);
        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(429, result.StatusCode);
        
        var errorResponse = Assert.IsType<JsonApiErrorResponse>(result.Value);
        Assert.Single(errorResponse.Errors);
        Assert.Equal("429", errorResponse.Errors[0].Status);
        Assert.Equal("Too Many Requests", errorResponse.Errors[0].Title);
        Assert.Equal("Rate limit exceeded", errorResponse.Errors[0].Detail);
    }

    [Fact]
    public void OnException_WithUnhandledException_Returns500()
    {
        var exception = new InvalidOperationException("Something went wrong");
        var context = CreateExceptionContext(exception);

        _filter.OnException(context);

        Assert.True(context.ExceptionHandled);
        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(500, result.StatusCode);
        
        var errorResponse = Assert.IsType<JsonApiErrorResponse>(result.Value);
        Assert.Single(errorResponse.Errors);
        Assert.Equal("500", errorResponse.Errors[0].Status);
        Assert.Equal("Internal Server Error", errorResponse.Errors[0].Title);
        Assert.Equal("An error occurred while processing your request.", errorResponse.Errors[0].Detail);
    }

    [Fact]
    public void OnException_WithHandledException_LogsWithoutStackTrace()
    {
        var exception = new JsonApiNotFoundException("Resource not found");
        var context = CreateExceptionContext(exception);

        _filter.OnException(context);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("JsonApiNotFoundException")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void OnException_WithUnhandledException_LogsWithStackTrace()
    {
        var exception = new InvalidOperationException("Something went wrong");
        var context = CreateExceptionContext(exception);

        _filter.OnException(context);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("An unhandled exception occurred")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}

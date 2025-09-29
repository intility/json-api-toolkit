using System.Net;
using System.Text.Json;
using JsonApiToolkit.Attributes;
using JsonApiToolkit.Controllers;
using JsonApiToolkit.Extensions;
using JsonApiToolkit.Models.Errors;
using JsonApiToolkit.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JsonApiToolkit.Tests.Integration;

public class AllowedIncludesIntegrationTests : IDisposable
{
    private readonly IHost _host;
    private readonly HttpClient _client;

    public AllowedIncludesIntegrationTests()
    {
        _host = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddDbContext<TestDbContext>(options =>
                            options.UseInMemoryDatabase("TestDb")
                        );
                        services.AddControllers();
                        services.AddJsonApiToolkit();
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapControllers();
                        });
                    });
            })
            .Build();

        _host.Start();
        _client = _host.GetTestClient();
    }

    [Fact]
    public async Task GetWithAllowedInclude_ReturnsOkAsync()
    {
        var response = await _client.GetAsync("/api/test/with-allowed?include=author");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetWithForbiddenInclude_ReturnsForbiddenAsync()
    {
        var response = await _client.GetAsync("/api/test/with-allowed?include=forbidden");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<JsonApiErrorResponse>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        Assert.NotNull(errorResponse);
        Assert.NotEmpty(errorResponse.Errors);
        Assert.Equal("403", errorResponse.Errors[0].Status);
    }

    [Fact]
    public async Task GetWithWildcard_AllowsMatchingPatternAsync()
    {
        var response = await _client.GetAsync("/api/test/with-wildcard?include=author.posts");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetWithWildcard_ForbidsDeeperNestingAsync()
    {
        var response = await _client.GetAsync(
            "/api/test/with-wildcard?include=author.posts.comments"
        );

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetWithoutAttribute_AllowsAnyIncludeAsync()
    {
        var response = await _client.GetAsync(
            "/api/test/without-attribute?include=anything.deeply.nested"
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetWithEmptyAttribute_ForbidsAllIncludesAsync()
    {
        var response = await _client.GetAsync("/api/test/with-empty?include=author");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ForbiddenInclude_ContainsMetaInformationAsync()
    {
        var response = await _client.GetAsync("/api/test/with-allowed?include=forbidden,author");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();

        // Verify meta information is present
        Assert.Contains("\"meta\":", content);
        Assert.Contains("requestedIncludes", content);
        Assert.Contains("forbiddenIncludes", content);
        Assert.Contains("allowedIncludes", content);
        Assert.Contains("forbidden", content);
        Assert.Contains("author", content);
    }

    [Fact]
    public async Task EmptyAllowedIncludes_ShowsCorrectErrorDetailAsync()
    {
        var response = await _client.GetAsync("/api/test/with-empty?include=something");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();

        // Error should mention the actual requested include, not empty string
        Assert.Contains("something", content);
        Assert.DoesNotContain("''", content);
    }

    public void Dispose()
    {
        _client?.Dispose();
        _host?.Dispose();
    }
}

// Test controller for integration tests
[ApiController]
[Route("api/test")]
public class TestIntegrationController : JsonApiController
{
    public TestIntegrationController(
        ILogger<JsonApiController> logger,
        IJsonApiQueryParser queryParser
    )
        : base(logger, queryParser) { }

    [HttpGet("with-allowed")]
    [AllowedIncludes("author", "posts")]
    public IActionResult GetWithAllowed()
    {
        return Ok(new { data = new { type = "test", id = "1" } });
    }

    [HttpGet("with-wildcard")]
    [AllowedIncludes("author.*", "posts")]
    public IActionResult GetWithWildcard()
    {
        return Ok(new { data = new { type = "test", id = "1" } });
    }

    [HttpGet("without-attribute")]
    public IActionResult GetWithoutAttribute()
    {
        return Ok(new { data = new { type = "test", id = "1" } });
    }

    [HttpGet("with-empty")]
    [AllowedIncludes()]
    public IActionResult GetWithEmpty()
    {
        return Ok(new { data = new { type = "test", id = "1" } });
    }
}

// Test DbContext for integration tests
public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options)
        : base(options) { }

    public DbSet<TestEntity> TestEntities { get; set; }
}

// Test entity
public class TestEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

using System.Net;
using System.Text.Json;
using JsonApiToolkit.Extensions;
using JsonApiToolkit.Models.Errors;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace JsonApiToolkit.Tests.Integration;

/// <summary>
/// Integration tests for StrictQueryValidation: invalid query parameters return
/// 400 with a descriptive code instead of being silently ignored (or 500 for
/// unconvertible values). Re-uses the QueryTest fixtures from JsonApiQueryAsyncTests.
/// </summary>
public class StrictQueryValidationIntegrationTests : IDisposable
{
    private readonly IHost _host;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public StrictQueryValidationIntegrationTests()
    {
        var databaseName = $"StrictQueryValidationTestDb_{Guid.NewGuid()}";

        _host = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddDbContext<QueryTestDbContext>(options =>
                            options.UseInMemoryDatabase(databaseName)
                        );
                        services.AddControllers();
                        services.AddJsonApiToolkit(options =>
                        {
                            options.StrictQueryValidation = true;
                        });
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(endpoints => endpoints.MapControllers());

                        using var scope = app.ApplicationServices.CreateScope();
                        var context =
                            scope.ServiceProvider.GetRequiredService<QueryTestDbContext>();
                        SeedData(context);
                    });
            })
            .Build();

        _host.Start();
        _client = _host.GetTestClient();
    }

    private static void SeedData(QueryTestDbContext context)
    {
        var author = new QueryTestAuthor { Id = 1, Name = "Alice" };
        context.Authors.Add(author);

        for (int i = 1; i <= 5; i++)
        {
            context.Articles.Add(
                new QueryTestArticle
                {
                    Id = i,
                    Title = $"Article {i}",
                    Content = $"Content {i}",
                    CreatedAt = new DateTime(2024, 1, i),
                    IsPublished = i % 2 == 1,
                    ViewCount = i * 10,
                    AuthorId = 1,
                }
            );
        }
        context.SaveChanges();
    }

    private async Task<(HttpStatusCode Status, JsonApiError? Error)> GetErrorAsync(string url)
    {
        var response = await _client.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();
        var doc = JsonSerializer.Deserialize<JsonApiErrorResponse>(content, _jsonOptions);
        return (response.StatusCode, doc?.Errors?.FirstOrDefault());
    }

    [Fact]
    public async Task AndGroup_Returns400UnsupportedFilterGroupAsync()
    {
        var (status, error) = await GetErrorAsync("/api/articles?filter[and][0][isPublished]=true");

        Assert.Equal(HttpStatusCode.BadRequest, status);
        Assert.Equal(JsonApiErrorCodes.UnsupportedFilterGroup, error?.Code);
    }

    [Fact]
    public async Task NestedGroup_Returns400UnsupportedFilterGroupAsync()
    {
        var (status, error) = await GetErrorAsync(
            "/api/articles?filter[or][0][title]=A&filter[or][1][and][0][title][eq]=B"
        );

        Assert.Equal(HttpStatusCode.BadRequest, status);
        Assert.Equal(JsonApiErrorCodes.UnsupportedFilterGroup, error?.Code);
    }

    [Fact]
    public async Task NestedGroupWithoutOperator_Returns400UnsupportedFilterGroupAsync()
    {
        var (status, error) = await GetErrorAsync(
            "/api/articles?filter[or][0][title]=A&filter[or][1][and][0][title]=B"
        );

        Assert.Equal(HttpStatusCode.BadRequest, status);
        Assert.Equal(JsonApiErrorCodes.UnsupportedFilterGroup, error?.Code);
    }

    [Fact]
    public async Task UnknownFilterField_Returns400InvalidFilterFieldAsync()
    {
        var (status, error) = await GetErrorAsync("/api/articles?filter[bogus]=x");

        Assert.Equal(HttpStatusCode.BadRequest, status);
        Assert.Equal(JsonApiErrorCodes.InvalidFilterField, error?.Code);
    }

    [Fact]
    public async Task UnconvertibleFilterValue_Returns400InsteadOf500Async()
    {
        // "isnull" as a DateTime value: the classic 2-arg builder wart
        var (status, error) = await GetErrorAsync("/api/articles?filter[createdAt]=isnull");

        Assert.Equal(HttpStatusCode.BadRequest, status);
        Assert.Equal(JsonApiErrorCodes.InvalidFilterValue, error?.Code);
    }

    [Fact]
    public async Task UnknownFilterOperator_Returns400InvalidFilterOperatorAsync()
    {
        var (status, error) = await GetErrorAsync("/api/articles?filter[title][contains]=x");

        Assert.Equal(HttpStatusCode.BadRequest, status);
        Assert.Equal(JsonApiErrorCodes.InvalidFilterOperator, error?.Code);
    }

    [Fact]
    public async Task UnknownSortField_Returns400InvalidSortFieldAsync()
    {
        var (status, error) = await GetErrorAsync("/api/articles?sort=bogus");

        Assert.Equal(HttpStatusCode.BadRequest, status);
        Assert.Equal(JsonApiErrorCodes.InvalidSortField, error?.Code);
    }

    [Fact]
    public async Task DotPathSort_Returns400InvalidSortFieldAsync()
    {
        var (status, error) = await GetErrorAsync("/api/articles?sort=author.name");

        Assert.Equal(HttpStatusCode.BadRequest, status);
        Assert.Equal(JsonApiErrorCodes.InvalidSortField, error?.Code);
    }

    [Fact]
    public async Task IncludeFilterWithoutInclude_Returns400FilterNotAllowedAsync()
    {
        var (status, error) = await GetErrorAsync("/api/articles?filter[author][name][eq]=Alice");

        Assert.Equal(HttpStatusCode.BadRequest, status);
        Assert.Equal(JsonApiErrorCodes.FilterNotAllowed, error?.Code);
    }

    [Fact]
    public async Task MalformedFilterKey_Returns400ValidationFailedAsync()
    {
        var (status, error) = await GetErrorAsync("/api/articles?filter[x=1");

        Assert.Equal(HttpStatusCode.BadRequest, status);
        Assert.Equal(JsonApiErrorCodes.ValidationFailed, error?.Code);
    }

    [Fact]
    public async Task ValidQuery_StillReturns200Async()
    {
        var response = await _client.GetAsync(
            "/api/articles?filter[isPublished]=true&filter[viewCount][gt]=10"
                + "&filter[or][0][title]=Article 3&filter[or][1][title]=Article 5"
                + "&sort=-viewCount&include=author&page[number]=1&page[size]=2"
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task IncludeFilterWithMatchingInclude_Returns200Async()
    {
        var response = await _client.GetAsync(
            "/api/articles?filter[author][name][eq]=Alice&include=author"
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    public void Dispose()
    {
        _client.Dispose();
        _host.Dispose();
        GC.SuppressFinalize(this);
    }
}

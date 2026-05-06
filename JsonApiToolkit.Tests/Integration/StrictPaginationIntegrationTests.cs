using System.Net;
using System.Text.Json;
using JsonApiToolkit.Extensions;
using JsonApiToolkit.Models.Documents;
using JsonApiToolkit.Models.Errors;
using JsonApiToolkit.Models.Resources;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace JsonApiToolkit.Tests.Integration;

/// <summary>
/// Integration tests for runtime strict-pagination behavior (page &gt; totalPages → 404).
/// Re-uses the QueryTestArticle/QueryTestDbContext fixtures from JsonApiQueryAsyncTests.
/// </summary>
public class StrictPaginationIntegrationTests : IDisposable
{
    private readonly IHost _host;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public StrictPaginationIntegrationTests()
    {
        var databaseName = $"StrictPaginationTestDb_{Guid.NewGuid()}";

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
                            options.StrictPagination = true;
                        });
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(endpoints => endpoints.MapControllers());

                        using var scope = app.ApplicationServices.CreateScope();
                        var context =
                            scope.ServiceProvider.GetRequiredService<QueryTestDbContext>();
                        SeedFiveArticles(context);
                    });
            })
            .Build();

        _host.Start();
        _client = _host.GetTestClient();
    }

    private static void SeedFiveArticles(QueryTestDbContext context)
    {
        for (int i = 1; i <= 5; i++)
        {
            context.Articles.Add(
                new QueryTestArticle
                {
                    Id = i,
                    Title = $"Article {i}",
                    Content = $"Content {i}",
                    CreatedAt = new DateTime(2024, 1, i),
                    IsPublished = true,
                    ViewCount = i * 10,
                }
            );
        }
        context.SaveChanges();
    }

    [Fact]
    public async Task PageBeyondTotal_Returns404Async()
    {
        // 5 articles, page size 2 → 3 total pages. Page 100 must 404.
        var response = await _client.GetAsync("/api/articles?page[number]=100&page[size]=2");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PageBeyondTotal_ErrorBodyHasMetaAsync()
    {
        var response = await _client.GetAsync("/api/articles?page[number]=10&page[size]=2");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var doc = JsonSerializer.Deserialize<JsonApiErrorResponse>(content, _jsonOptions);

        Assert.NotNull(doc?.Errors);
        var error = Assert.Single(doc.Errors);
        Assert.Equal("404", error.Status);
        Assert.Equal(JsonApiErrorCodes.InvalidPageNumber, error.Code);
        Assert.Equal("page[number]", error.Source?.Parameter);
        Assert.NotNull(error.Meta);
        Assert.Equal(10, GetIntFromMeta(error.Meta, "value"));
        Assert.Equal(3, GetIntFromMeta(error.Meta, "totalPages"));
        Assert.Equal(5, GetIntFromMeta(error.Meta, "totalResources"));
    }

    [Fact]
    public async Task LastPage_Returns200Async()
    {
        // Exactly the last page must succeed (boundary check: > not >=).
        var response = await _client.GetAsync("/api/articles?page[number]=3&page[size]=2&sort=id");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var doc = JsonSerializer.Deserialize<JsonApiCollectionDocument<ResourceObject>>(
            content,
            _jsonOptions
        );

        Assert.NotNull(doc?.Data);
        Assert.Single(doc.Data);
        Assert.Equal("5", doc.Data.First().Id);
    }

    [Fact]
    public async Task EmptyResultWithPaging_DoesNotReturn404Async()
    {
        // Filter that matches no rows. With totalCount=0, strict mode must not 404 page=2 —
        // there are no pages to be wrong about.
        var response = await _client.GetAsync(
            "/api/articles?filter[title]=NoSuchArticle&page[number]=2&page[size]=10"
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var doc = JsonSerializer.Deserialize<JsonApiCollectionDocument<ResourceObject>>(
            content,
            _jsonOptions
        );

        Assert.NotNull(doc?.Data);
        Assert.Empty(doc.Data);
    }

    private static int GetIntFromMeta(Dictionary<string, object> meta, string key)
    {
        Assert.True(meta.TryGetValue(key, out var raw), $"Missing meta key '{key}'");
        return raw switch
        {
            JsonElement e => e.GetInt32(),
            int i => i,
            long l => (int)l,
            _ => Convert.ToInt32(raw),
        };
    }

    public void Dispose()
    {
        _client.Dispose();
        _host.Dispose();
        GC.SuppressFinalize(this);
    }
}

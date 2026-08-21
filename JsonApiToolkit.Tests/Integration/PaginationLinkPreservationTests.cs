using System.Text.Json;
using JsonApiToolkit.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace JsonApiToolkit.Tests.Integration;

/// <summary>
/// Integration tests for PreserveQueryInPaginationLinks: first/last/prev/next
/// keep the request's query string with only the page parameters replaced.
/// Re-uses the QueryTest fixtures from JsonApiQueryAsyncTests.
/// </summary>
public class PaginationLinkPreservationTests : IDisposable
{
    private readonly IHost _host;
    private readonly HttpClient _client;

    public PaginationLinkPreservationTests()
    {
        var databaseName = $"PaginationLinksTestDb_{Guid.NewGuid()}";

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
                            options.PreserveQueryInPaginationLinks = true;
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
        for (int i = 1; i <= 6; i++)
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

    private async Task<Dictionary<string, string?>> GetLinksAsync(string url)
    {
        var response = await _client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc
            .RootElement.GetProperty("links")
            .EnumerateObject()
            .ToDictionary(p => p.Name, p => p.Value.GetString());
    }

    [Fact]
    public async Task Links_PreserveFilterAndSort_ReplaceOnlyPageParamsAsync()
    {
        var links = await GetLinksAsync(
            "/api/articles?filter[isPublished]=true&sort=-viewCount&page[number]=2&page[size]=2"
        );

        // Keys are re-encoded by the link builder, so brackets appear as %5B/%5D
        Assert.Equal(
            "http://localhost/api/articles"
                + "?filter%5BisPublished%5D=true&sort=-viewCount&page%5Bnumber%5D=3&page%5Bsize%5D=2",
            links["next"]
        );
        Assert.Contains("filter%5BisPublished%5D=true", links["prev"]);
        Assert.Contains("page%5Bnumber%5D=1", links["prev"]);
        Assert.Contains("page%5Bnumber%5D=1", links["first"]);
        Assert.Contains("page%5Bnumber%5D=3", links["last"]);
    }

    [Fact]
    public async Task Links_WithOnlyPageParams_ContainJustPageParamsAsync()
    {
        var links = await GetLinksAsync("/api/articles?page[number]=1&page[size]=2");

        Assert.Equal(
            "http://localhost/api/articles?page%5Bnumber%5D=2&page%5Bsize%5D=2",
            links["next"]
        );
        Assert.False(links.ContainsKey("prev"));
    }

    [Fact]
    public async Task NextLink_IsFollowable_AndKeepsFilterAppliedAsync()
    {
        var links = await GetLinksAsync(
            "/api/articles?filter[viewCount][gt]=20&page[number]=1&page[size]=2"
        );

        // 4 matches (30..60), page size 2 -> next is page 2 with the filter intact
        var next = new Uri(links["next"]!);
        var response = await _client.GetAsync(next.PathAndQuery);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var pagination = doc.RootElement.GetProperty("meta").GetProperty("pagination");
        Assert.Equal(4, pagination.GetProperty("totalResources").GetInt32());
        Assert.Equal(2, pagination.GetProperty("currentPage").GetInt32());
    }

    public void Dispose()
    {
        _client.Dispose();
        _host.Dispose();
        GC.SuppressFinalize(this);
    }
}

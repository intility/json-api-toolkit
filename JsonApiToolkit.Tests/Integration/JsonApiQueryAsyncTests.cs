using System.Net;
using System.Text.Json;
using JsonApiToolkit.Controllers;
using JsonApiToolkit.Extensions;
using JsonApiToolkit.Models.Documents;
using JsonApiToolkit.Models.Resources;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace JsonApiToolkit.Tests.Integration;

/// <summary>
/// Integration tests for JsonApiQueryAsync covering filtering, sorting, pagination, and includes.
/// </summary>
public class JsonApiQueryAsyncTests : IDisposable
{
    private readonly IHost _host;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public JsonApiQueryAsyncTests()
    {
        var databaseName = $"QueryTestDb_{Guid.NewGuid()}";

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
                        services.AddJsonApiToolkit();
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapControllers();
                        });

                        // Seed test data
                        using var scope = app.ApplicationServices.CreateScope();
                        var context =
                            scope.ServiceProvider.GetRequiredService<QueryTestDbContext>();
                        SeedTestData(context);
                    });
            })
            .Build();

        _host.Start();
        _client = _host.GetTestClient();
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    private static void SeedTestData(QueryTestDbContext context)
    {
        var author1 = new QueryTestAuthor
        {
            Id = 1,
            Name = "Alice",
            Email = "alice@test.com",
        };
        var author2 = new QueryTestAuthor
        {
            Id = 2,
            Name = "Bob",
            Email = "bob@test.com",
        };
        var author3 = new QueryTestAuthor
        {
            Id = 3,
            Name = "Charlie",
            Email = "charlie@test.com",
        };

        context.Authors.AddRange(author1, author2, author3);

        var articles = new List<QueryTestArticle>
        {
            new()
            {
                Id = 1,
                Title = "First Article",
                Content = "Content A",
                CreatedAt = new DateTime(2024, 1, 1),
                IsPublished = true,
                ViewCount = 100,
                Author = author1,
            },
            new()
            {
                Id = 2,
                Title = "Second Article",
                Content = "Content B",
                CreatedAt = new DateTime(2024, 2, 1),
                IsPublished = true,
                ViewCount = 50,
                Author = author1,
            },
            new()
            {
                Id = 3,
                Title = "Third Article",
                Content = "Content C",
                CreatedAt = new DateTime(2024, 3, 1),
                IsPublished = false,
                ViewCount = 25,
                Author = author2,
            },
            new()
            {
                Id = 4,
                Title = "Fourth Article",
                Content = "Another Content",
                CreatedAt = new DateTime(2024, 4, 1),
                IsPublished = true,
                ViewCount = 200,
                Author = author2,
            },
            new()
            {
                Id = 5,
                Title = "Fifth Article",
                Content = "More Content",
                CreatedAt = new DateTime(2024, 5, 1),
                IsPublished = true,
                ViewCount = 75,
                Author = author3,
            },
        };

        context.Articles.AddRange(articles);

        // Add comments to articles
        context.Comments.AddRange(
            new QueryTestComment
            {
                Id = 1,
                Text = "Great article!",
                ArticleId = 1,
            },
            new QueryTestComment
            {
                Id = 2,
                Text = "Very helpful",
                ArticleId = 1,
            },
            new QueryTestComment
            {
                Id = 3,
                Text = "Nice work",
                ArticleId = 2,
            }
        );

        context.SaveChanges();
    }

    #region Basic Query Tests

    [Fact]
    public async Task GetArticles_WithNoParameters_ReturnsAllArticlesAsync()
    {
        var response = await _client.GetAsync("/api/articles");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonApiCollectionDocument<ResourceObject>>(
            content,
            _jsonOptions
        );

        Assert.NotNull(document?.Data);
        Assert.Equal(5, document.Data.Count());
    }

    [Fact]
    public async Task GetArticles_ReturnsCorrectResourceTypeAsync()
    {
        var response = await _client.GetAsync("/api/articles");

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonApiCollectionDocument<ResourceObject>>(
            content,
            _jsonOptions
        );

        Assert.NotNull(document?.Data);
        Assert.All(document.Data, resource => Assert.Equal("articles", resource.Type));
    }

    #endregion

    #region Filtering Tests

    [Fact]
    public async Task GetArticles_FilterByEquality_ReturnsMatchingArticlesAsync()
    {
        var response = await _client.GetAsync("/api/articles?filter[title]=First Article");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonApiCollectionDocument<ResourceObject>>(
            content,
            _jsonOptions
        );

        Assert.NotNull(document?.Data);
        var article = Assert.Single(document.Data);
        Assert.Equal("1", article.Id);
    }

    [Fact]
    public async Task GetArticles_FilterByBoolean_ReturnsMatchingArticlesAsync()
    {
        var response = await _client.GetAsync("/api/articles?filter[isPublished]=true");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonApiCollectionDocument<ResourceObject>>(
            content,
            _jsonOptions
        );

        Assert.NotNull(document?.Data);
        Assert.Equal(4, document.Data.Count()); // 4 published articles
    }

    [Fact]
    public async Task GetArticles_FilterWithGreaterThan_ReturnsMatchingArticlesAsync()
    {
        var response = await _client.GetAsync("/api/articles?filter[viewCount][gt]=75");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonApiCollectionDocument<ResourceObject>>(
            content,
            _jsonOptions
        );

        Assert.NotNull(document?.Data);
        Assert.Equal(2, document.Data.Count()); // viewCount > 75 (100 and 200)
    }

    [Fact]
    public async Task GetArticles_FilterWithLessThan_ReturnsMatchingArticlesAsync()
    {
        var response = await _client.GetAsync("/api/articles?filter[viewCount][lt]=75");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonApiCollectionDocument<ResourceObject>>(
            content,
            _jsonOptions
        );

        Assert.NotNull(document?.Data);
        Assert.Equal(2, document.Data.Count()); // viewCount < 75 (50 and 25)
    }

    [Fact]
    public async Task GetArticles_FilterWithLike_ReturnsMatchingArticlesAsync()
    {
        var response = await _client.GetAsync("/api/articles?filter[content][like]=Content");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonApiCollectionDocument<ResourceObject>>(
            content,
            _jsonOptions
        );

        Assert.NotNull(document?.Data);
        Assert.Equal(5, document.Data.Count()); // All have "Content" in content field
    }

    [Fact]
    public async Task GetArticles_FilterWithIn_ReturnsMatchingArticlesAsync()
    {
        var response = await _client.GetAsync("/api/articles?filter[id][in]=1,3,5");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonApiCollectionDocument<ResourceObject>>(
            content,
            _jsonOptions
        );

        Assert.NotNull(document?.Data);
        Assert.Equal(3, document.Data.Count());
        var ids = document.Data.Select(r => r.Id).ToList();
        Assert.Contains("1", ids);
        Assert.Contains("3", ids);
        Assert.Contains("5", ids);
    }

    [Fact]
    public async Task GetArticles_MultipleFilters_AppliesAllFiltersAsync()
    {
        var response = await _client.GetAsync(
            "/api/articles?filter[isPublished]=true&filter[viewCount][gt]=50"
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonApiCollectionDocument<ResourceObject>>(
            content,
            _jsonOptions
        );

        Assert.NotNull(document?.Data);
        Assert.Equal(3, document.Data.Count()); // Published AND viewCount > 50 (100, 200, 75)
    }

    [Fact]
    public async Task GetArticles_FilterWithNoMatch_ReturnsEmptyCollectionAsync()
    {
        var response = await _client.GetAsync("/api/articles?filter[title]=Nonexistent");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonApiCollectionDocument<ResourceObject>>(
            content,
            _jsonOptions
        );

        Assert.NotNull(document?.Data);
        Assert.Empty(document.Data);
    }

    [Fact]
    public async Task GetArticles_FilterByInvalidField_SkipsFilterAsync()
    {
        // Invalid filter fields are skipped gracefully
        var response = await _client.GetAsync("/api/articles?filter[nonexistent]=value");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonApiCollectionDocument<ResourceObject>>(
            content,
            _jsonOptions
        );

        Assert.NotNull(document?.Data);
        Assert.Equal(5, document.Data.Count()); // Returns all since invalid filter is skipped
    }

    #endregion

    #region Sorting Tests

    [Fact]
    public async Task GetArticles_SortAscending_ReturnsSortedArticlesAsync()
    {
        var response = await _client.GetAsync("/api/articles?sort=viewCount");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonApiCollectionDocument<ResourceObject>>(
            content,
            _jsonOptions
        );

        Assert.NotNull(document?.Data);
        var ids = document.Data.Select(r => r.Id).ToList();
        Assert.Equal("3", ids[0]); // viewCount: 25
        Assert.Equal("2", ids[1]); // viewCount: 50
        Assert.Equal("5", ids[2]); // viewCount: 75
        Assert.Equal("1", ids[3]); // viewCount: 100
        Assert.Equal("4", ids[4]); // viewCount: 200
    }

    [Fact]
    public async Task GetArticles_SortDescending_ReturnsSortedArticlesAsync()
    {
        var response = await _client.GetAsync("/api/articles?sort=-viewCount");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonApiCollectionDocument<ResourceObject>>(
            content,
            _jsonOptions
        );

        Assert.NotNull(document?.Data);
        var ids = document.Data.Select(r => r.Id).ToList();
        Assert.Equal("4", ids[0]); // viewCount: 200
        Assert.Equal("1", ids[1]); // viewCount: 100
    }

    [Fact]
    public async Task GetArticles_SortByTitle_SortsByStringAsync()
    {
        var response = await _client.GetAsync("/api/articles?sort=title");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonApiCollectionDocument<ResourceObject>>(
            content,
            _jsonOptions
        );

        Assert.NotNull(document?.Data);
        var titles = document.Data.Select(r => r.Attributes?["title"]?.ToString()).ToList();
        Assert.Equal("Fifth Article", titles[0]);
        Assert.Equal("First Article", titles[1]);
    }

    [Fact]
    public async Task GetArticles_MultiFieldSort_SortsCorrectlyAsync()
    {
        var response = await _client.GetAsync("/api/articles?sort=isPublished,-viewCount");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonApiCollectionDocument<ResourceObject>>(
            content,
            _jsonOptions
        );

        Assert.NotNull(document?.Data);
        var ids = document.Data.Select(r => r.Id).ToList();
        // First: isPublished=false (id 3), then isPublished=true ordered by viewCount desc
        Assert.Equal("3", ids[0]); // isPublished: false
    }

    [Fact]
    public async Task GetArticles_SortByInvalidField_SkipsInvalidSortAsync()
    {
        var response = await _client.GetAsync("/api/articles?sort=nonexistent");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonApiCollectionDocument<ResourceObject>>(
            content,
            _jsonOptions
        );

        Assert.NotNull(document?.Data);
        Assert.Equal(5, document.Data.Count()); // Returns all, sort is skipped
    }

    #endregion

    #region Pagination Tests

    [Fact]
    public async Task GetArticles_WithPagination_ReturnsCorrectPageAsync()
    {
        var response = await _client.GetAsync("/api/articles?page[number]=1&page[size]=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonApiCollectionDocument<ResourceObject>>(
            content,
            _jsonOptions
        );

        Assert.NotNull(document?.Data);
        Assert.Equal(2, document.Data.Count());
    }

    [Fact]
    public async Task GetArticles_WithPagination_ReturnsMetadataAsync()
    {
        var response = await _client.GetAsync("/api/articles?page[number]=1&page[size]=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonApiCollectionDocument<ResourceObject>>(
            content,
            _jsonOptions
        );

        Assert.NotNull(document?.Meta);
        Assert.Equal(5, GetPaginationValue<int>(document.Meta, "totalResources"));
        Assert.Equal(3, GetPaginationValue<int>(document.Meta, "totalPages"));
        Assert.Equal(1, GetPaginationValue<int>(document.Meta, "currentPage"));
        Assert.Equal(2, GetPaginationValue<int>(document.Meta, "pageSize"));
    }

    private static T GetPaginationValue<T>(Dictionary<string, object> meta, string key)
    {
        if (
            meta.TryGetValue("pagination", out var pagination)
            && pagination is JsonElement paginationElement
        )
        {
            if (paginationElement.TryGetProperty(key, out var property))
            {
                if (typeof(T) == typeof(int))
                    return (T)(object)property.GetInt32();
                if (typeof(T) == typeof(string))
                    return (T)(object)property.GetString()!;
            }
        }

        throw new InvalidOperationException(
            $"Pagination key '{key}' not found. Meta: {JsonSerializer.Serialize(meta)}"
        );
    }

    [Fact]
    public async Task GetArticles_SecondPage_ReturnsCorrectItemsAsync()
    {
        // First get first page with sorting to ensure deterministic order
        var response = await _client.GetAsync("/api/articles?page[number]=2&page[size]=2&sort=id");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonApiCollectionDocument<ResourceObject>>(
            content,
            _jsonOptions
        );

        Assert.NotNull(document?.Data);
        Assert.Equal(2, document.Data.Count());
        var ids = document.Data.Select(r => r.Id).ToList();
        Assert.Equal("3", ids[0]); // Second page with page size 2, sorted by id
        Assert.Equal("4", ids[1]);
    }

    [Fact]
    public async Task GetArticles_LastPage_ReturnsRemainingItemsAsync()
    {
        var response = await _client.GetAsync("/api/articles?page[number]=3&page[size]=2&sort=id");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonApiCollectionDocument<ResourceObject>>(
            content,
            _jsonOptions
        );

        Assert.NotNull(document?.Data);
        Assert.Single(document.Data); // Last page with only 1 remaining item
        Assert.Equal("5", document.Data.First().Id);
    }

    [Fact]
    public async Task GetArticles_PageBeyondTotal_ClampsToLastPageAsync()
    {
        // The library clamps page numbers to valid ranges
        // Page 100 with 5 items and page size 10 = 1 total page, so page is clamped to 1
        var response = await _client.GetAsync("/api/articles?page[number]=100&page[size]=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonApiCollectionDocument<ResourceObject>>(
            content,
            _jsonOptions
        );

        Assert.NotNull(document?.Data);
        Assert.Equal(5, document.Data.Count()); // Returns all data (clamped to page 1)
    }

    [Fact]
    public async Task GetArticles_PaginationWithFilter_FiltersFirstThenPaginatesAsync()
    {
        // Filter to 4 published articles, then paginate
        var response = await _client.GetAsync(
            "/api/articles?filter[isPublished]=true&page[number]=1&page[size]=2&sort=id"
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonApiCollectionDocument<ResourceObject>>(
            content,
            _jsonOptions
        );

        Assert.NotNull(document?.Data);
        Assert.Equal(2, document.Data.Count());
        Assert.NotNull(document.Meta);
        Assert.Equal(4, GetPaginationValue<int>(document.Meta, "totalResources")); // Total filtered count
        Assert.Equal(2, GetPaginationValue<int>(document.Meta, "totalPages"));
    }

    #endregion

    #region Include Tests

    [Fact]
    public async Task GetArticles_WithInclude_ReturnsIncludedResourcesAsync()
    {
        var response = await _client.GetAsync("/api/articles?include=author");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonApiCollectionDocument<ResourceObject>>(
            content,
            _jsonOptions
        );

        Assert.NotNull(document?.Included);
        Assert.NotEmpty(document.Included);
        Assert.Contains(document.Included, r => r.Type == "queryTestAuthor");
    }

    [Fact]
    public async Task GetArticles_WithInclude_IncludesRelationshipLinksAsync()
    {
        var response = await _client.GetAsync("/api/articles?include=author");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonApiCollectionDocument<ResourceObject>>(
            content,
            _jsonOptions
        );

        Assert.NotNull(document?.Data);
        var firstArticle = document.Data.First();
        Assert.NotNull(firstArticle.Relationships);
        Assert.Contains("author", firstArticle.Relationships.Keys);
    }

    [Fact]
    public async Task GetArticles_WithMultipleIncludes_ReturnsAllIncludedAsync()
    {
        var response = await _client.GetAsync("/api/articles?include=author,comments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonApiCollectionDocument<ResourceObject>>(
            content,
            _jsonOptions
        );

        Assert.NotNull(document?.Included);
        Assert.Contains(document.Included, r => r.Type == "queryTestAuthor");
        Assert.Contains(document.Included, r => r.Type == "queryTestComment");
    }

    [Fact]
    public async Task GetArticles_WithInvalidInclude_ReturnsServerErrorAsync()
    {
        // Invalid includes cause an error (EF Core can't include non-existent properties)
        var response = await _client.GetAsync("/api/articles?include=nonexistent");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    #endregion

    #region Combined Operations Tests

    [Fact]
    public async Task GetArticles_FilterSortPaginate_AppliesAllOperationsAsync()
    {
        var response = await _client.GetAsync(
            "/api/articles?filter[isPublished]=true&sort=-viewCount&page[number]=1&page[size]=2"
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonApiCollectionDocument<ResourceObject>>(
            content,
            _jsonOptions
        );

        Assert.NotNull(document?.Data);
        Assert.Equal(2, document.Data.Count());

        // Should be filtered (published only), sorted by viewCount desc, then paginated
        var ids = document.Data.Select(r => r.Id).ToList();
        Assert.Equal("4", ids[0]); // viewCount: 200 (highest among published)
        Assert.Equal("1", ids[1]); // viewCount: 100

        Assert.NotNull(document.Meta);
        Assert.Equal(4, GetPaginationValue<int>(document.Meta, "totalResources"));
    }

    [Fact]
    public async Task GetArticles_FullQueryWithIncludes_ReturnsCompleteResponseAsync()
    {
        var response = await _client.GetAsync(
            "/api/articles?filter[isPublished]=true&sort=title&page[number]=1&page[size]=3&include=author"
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonApiCollectionDocument<ResourceObject>>(
            content,
            _jsonOptions
        );

        // Verify data
        Assert.NotNull(document?.Data);
        Assert.Equal(3, document.Data.Count());

        // Verify sorting (by title ascending)
        var titles = document.Data.Select(r => r.Attributes?["title"]?.ToString()).ToList();
        Assert.Equal("Fifth Article", titles[0]);
        Assert.Equal("First Article", titles[1]);
        Assert.Equal("Fourth Article", titles[2]);

        // Verify pagination metadata
        Assert.NotNull(document.Meta);
        Assert.Equal(4, GetPaginationValue<int>(document.Meta, "totalResources"));
        Assert.Equal(2, GetPaginationValue<int>(document.Meta, "totalPages"));

        // Verify includes
        Assert.NotNull(document.Included);
        Assert.NotEmpty(document.Included);
    }

    #endregion

    #region Response Format Tests

    [Fact]
    public async Task GetArticles_ResponseContentType_IsJsonApiAsync()
    {
        var response = await _client.GetAsync("/api/articles");

        Assert.Equal("application/vnd.api+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetArticles_ResponseStructure_FollowsJsonApiSpecAsync()
    {
        var response = await _client.GetAsync("/api/articles");

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonApiCollectionDocument<ResourceObject>>(
            content,
            _jsonOptions
        );

        Assert.NotNull(document);
        Assert.NotNull(document.Data);

        var firstResource = document.Data.First();
        Assert.NotNull(firstResource.Id);
        Assert.NotNull(firstResource.Type);
        Assert.NotNull(firstResource.Attributes);
    }

    [Fact]
    public async Task GetArticles_ResourceAttributes_ContainsExpectedFieldsAsync()
    {
        var response = await _client.GetAsync("/api/articles");

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonApiCollectionDocument<ResourceObject>>(
            content,
            _jsonOptions
        );

        Assert.NotNull(document?.Data);
        var firstResource = document.Data.First();

        Assert.NotNull(firstResource.Attributes);
        Assert.Contains("title", firstResource.Attributes.Keys);
        Assert.Contains("content", firstResource.Attributes.Keys);
        Assert.Contains("isPublished", firstResource.Attributes.Keys);
        Assert.Contains("viewCount", firstResource.Attributes.Keys);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task GetArticles_EmptyQueryString_ReturnsAllAsync()
    {
        var response = await _client.GetAsync("/api/articles?");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonApiCollectionDocument<ResourceObject>>(
            content,
            _jsonOptions
        );

        Assert.NotNull(document?.Data);
        Assert.Equal(5, document.Data.Count());
    }

    [Fact]
    public async Task GetArticles_EmptyFilterValue_HandlesGracefullyAsync()
    {
        var response = await _client.GetAsync("/api/articles?filter[title]=");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonApiCollectionDocument<ResourceObject>>(
            content,
            _jsonOptions
        );

        Assert.NotNull(document?.Data);
        // Empty filter value filters for empty string, which returns no matches
        Assert.Empty(document.Data);
    }

    [Fact]
    public async Task GetArticles_PageSizeLargerThanDataset_ReturnsAllAsync()
    {
        var response = await _client.GetAsync("/api/articles?page[number]=1&page[size]=100");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonApiCollectionDocument<ResourceObject>>(
            content,
            _jsonOptions
        );

        Assert.NotNull(document?.Data);
        Assert.Equal(5, document.Data.Count());
        Assert.NotNull(document.Meta);
        Assert.Equal(1, GetPaginationValue<int>(document.Meta, "totalPages"));
    }

    #endregion

    public void Dispose()
    {
        _client?.Dispose();
        _host?.Dispose();
    }
}

#region Test Entities and DbContext

public class QueryTestArticle
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsPublished { get; set; }
    public int ViewCount { get; set; }

    public int? AuthorId { get; set; }
    public QueryTestAuthor? Author { get; set; }

    public List<QueryTestComment> Comments { get; set; } = new();
}

public class QueryTestAuthor
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public List<QueryTestArticle> Articles { get; set; } = new();
}

public class QueryTestComment
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;

    public int ArticleId { get; set; }
    public QueryTestArticle? Article { get; set; }
}

public class QueryTestDbContext : DbContext
{
    public DbSet<QueryTestArticle> Articles { get; set; } = null!;
    public DbSet<QueryTestAuthor> Authors { get; set; } = null!;
    public DbSet<QueryTestComment> Comments { get; set; } = null!;

    public QueryTestDbContext(DbContextOptions<QueryTestDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<QueryTestArticle>().HasOne(a => a.Author).WithMany(au => au.Articles);

        modelBuilder
            .Entity<QueryTestComment>()
            .HasOne(c => c.Article)
            .WithMany(a => a.Comments)
            .HasForeignKey(c => c.ArticleId);
    }
}

#endregion

#region Test Controller

[ApiController]
[Route("api/articles")]
public class QueryTestArticlesController : JsonApiController
{
    private readonly QueryTestDbContext _context;

    public QueryTestArticlesController(QueryTestDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetArticles()
    {
        return await JsonApiQueryAsync(_context.Articles, "articles");
    }
}

#endregion

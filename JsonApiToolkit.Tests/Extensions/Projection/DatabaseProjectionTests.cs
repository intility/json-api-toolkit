using System.Net;
using System.Reflection;
using System.Text.Json;
using JsonApiToolkit.Controllers;
using JsonApiToolkit.Extensions;
using JsonApiToolkit.Extensions.Projection;
using JsonApiToolkit.Mapping;
using JsonApiToolkit.Models.Documents;
using JsonApiToolkit.Models.Resources;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace JsonApiToolkit.Tests.Extensions.Projection;

// ---------------------------------------------------------------------------
// Unit tests
// ---------------------------------------------------------------------------

public class DynamicTypeBuilderTests
{
    [Fact]
    public void Build_WithScalarProperties_CreatesTypeWithCorrectPropertyTypes()
    {
        var props = new[]
        {
            ("Id", typeof(int)),
            ("Name", typeof(string)),
            ("Count", typeof(long)),
        };

        Type projType = DynamicTypeBuilder.Build(props);

        Assert.NotNull(projType);
        Assert.NotNull(projType.GetProperty("Id"));
        Assert.Equal(typeof(int), projType.GetProperty("Id")!.PropertyType);
        Assert.Equal(typeof(string), projType.GetProperty("Name")!.PropertyType);
        Assert.Equal(typeof(long), projType.GetProperty("Count")!.PropertyType);
    }

    [Fact]
    public void Build_GeneratedProperties_HavePublicGetterAndSetter()
    {
        Type projType = DynamicTypeBuilder.Build([("Title", typeof(string))]);

        PropertyInfo? prop = projType.GetProperty("Title");
        Assert.NotNull(prop);
        Assert.NotNull(prop!.GetGetMethod());
        Assert.NotNull(prop.GetSetMethod());
    }

    [Fact]
    public void Build_GetterAndSetter_RoundTripValueCorrectly()
    {
        Type projType = DynamicTypeBuilder.Build([("Name", typeof(string))]);

        object instance = Activator.CreateInstance(projType)!;
        projType.GetProperty("Name")!.SetValue(instance, "hello");
        object? value = projType.GetProperty("Name")!.GetValue(instance);

        Assert.Equal("hello", value);
    }

    [Fact]
    public void Build_MultipleCallsWithSameProps_ReturnDistinctTypes()
    {
        var props = new[] { ("Id", typeof(int)), ("Name", typeof(string)) };

        Type t1 = DynamicTypeBuilder.Build(props);
        Type t2 = DynamicTypeBuilder.Build(props);

        // Each call produces a new type (caching is ProjectionTypeCache's job)
        Assert.NotSame(t1, t2);
    }

    [Fact]
    public void Build_WithNullableValueType_CreatesPropertyWithCorrectType()
    {
        Type projType = DynamicTypeBuilder.Build([("PublishedAt", typeof(DateTime?))]);

        PropertyInfo? prop = projType.GetProperty("PublishedAt");
        Assert.Equal(typeof(DateTime?), prop!.PropertyType);
    }
}

public class ProjectionPropertySelectorTests
{
    [Fact]
    public void Determine_AlwaysIncludesIdProperty()
    {
        var selected = ProjectionPropertySelector.Determine(
            typeof(ProjSelectorArticle),
            ["title"],
            []
        );

        Assert.Contains(selected, p => p.Name == "Id");
    }

    [Fact]
    public void Determine_IncludesRequestedAttributeFields()
    {
        var selected = ProjectionPropertySelector.Determine(
            typeof(ProjSelectorArticle),
            ["title"],
            []
        );

        Assert.Contains(selected, p => p.Name == "Title");
    }

    [Fact]
    public void Determine_ExcludesUnrequestedAttributeFields()
    {
        var selected = ProjectionPropertySelector.Determine(
            typeof(ProjSelectorArticle),
            ["title"],
            []
        );

        Assert.DoesNotContain(selected, p => p.Name == "Summary");
    }

    [Fact]
    public void Determine_IncludesNavigationPropertyForActiveInclude()
    {
        var selected = ProjectionPropertySelector.Determine(
            typeof(ProjSelectorArticle),
            ["title"],
            ["Author"]
        );

        Assert.Contains(selected, p => p.Name == "Author");
    }

    [Fact]
    public void Determine_ExcludesNavigationPropertyNotInIncludes()
    {
        var selected = ProjectionPropertySelector.Determine(
            typeof(ProjSelectorArticle),
            ["title"],
            []
        );

        Assert.DoesNotContain(selected, p => p.Name == "Author");
    }

    [Fact]
    public void Determine_HandlesNestedIncludePath_OnlyTopSegment()
    {
        // "Author.Articles" should include the "Author" navigation on ProjSelectorArticle
        var selected = ProjectionPropertySelector.Determine(
            typeof(ProjSelectorArticle),
            ["title"],
            ["Author.Articles"]
        );

        Assert.Contains(selected, p => p.Name == "Author");
    }
}

public class ProjectionTypeCacheTests
{
    [Fact]
    public void GetOrCreate_SameProperties_ReturnsSameProjectionType()
    {
        var props = typeof(ProjSelectorArticle)
            .GetProperties()
            .Where(p => p.Name is "Id" or "Title")
            .ToList();

        var (type1, _) = ProjectionTypeCache.GetOrCreate(typeof(ProjSelectorArticle), props);
        var (type2, _) = ProjectionTypeCache.GetOrCreate(typeof(ProjSelectorArticle), props);

        Assert.Same(type1, type2);
    }

    [Fact]
    public void GetOrCreate_DifferentPropertySets_ReturnDifferentTypes()
    {
        var propsA = typeof(ProjSelectorArticle)
            .GetProperties()
            .Where(p => p.Name == "Id")
            .ToList();
        var propsB = typeof(ProjSelectorArticle)
            .GetProperties()
            .Where(p => p.Name is "Id" or "Title")
            .ToList();

        var (typeA, _) = ProjectionTypeCache.GetOrCreate(typeof(ProjSelectorArticle), propsA);
        var (typeB, _) = ProjectionTypeCache.GetOrCreate(typeof(ProjSelectorArticle), propsB);

        Assert.NotSame(typeA, typeB);
    }

    [Fact]
    public void GetOrCreate_ReturnsExpressionWithCorrectSourceType()
    {
        var props = typeof(ProjSelectorArticle)
            .GetProperties()
            .Where(p => p.Name is "Id" or "Title")
            .ToList();

        var (_, expr) = ProjectionTypeCache.GetOrCreate(typeof(ProjSelectorArticle), props);

        Assert.Equal(typeof(ProjSelectorArticle), expr.Parameters[0].Type);
    }
}

// Shared test entities for unit tests
public class ProjSelectorArticle
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public ProjSelectorAuthor? Author { get; set; }
}

public class ProjSelectorAuthor
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<ProjSelectorArticle> Articles { get; set; } = new();
}

// ---------------------------------------------------------------------------
// Integration tests
// ---------------------------------------------------------------------------

public class DatabaseProjectionIntegrationTests : IDisposable
{
    private readonly IHost _host;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public DatabaseProjectionIntegrationTests()
    {
        var databaseName = $"ProjTestDb_{Guid.NewGuid()}";

        _host = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddDbContext<ProjTestDbContext>(options =>
                            options.UseInMemoryDatabase(databaseName)
                        );
                        services.AddControllers();
                        services.AddJsonApiToolkit(options =>
                        {
                            options.EnableDatabaseProjection = true;
                        });
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(endpoints => endpoints.MapControllers());

                        using var scope = app.ApplicationServices.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<ProjTestDbContext>();
                        SeedData(db);
                    });
            })
            .Build();

        _host.Start();
        _client = _host.GetTestClient();
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    private static void SeedData(ProjTestDbContext db)
    {
        var author = new ProjTestAuthor
        {
            Id = 1,
            Name = "Alice",
            Email = "alice@test.com",
        };
        db.Authors.Add(author);

        db.Articles.AddRange(
            new ProjTestArticle
            {
                Id = 1,
                Title = "First",
                Body = "Body A",
                ViewCount = 10,
                Author = author,
            },
            new ProjTestArticle
            {
                Id = 2,
                Title = "Second",
                Body = "Body B",
                ViewCount = 20,
                Author = author,
            }
        );

        db.SaveChanges();
    }

    [Fact]
    public async Task Projection_OnlyRequestedFieldsAppearInAttributes()
    {
        var response = await _client.GetAsync("/api/proj-articles?fields[articles]=title");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var doc = await Deserialize(response);
        var attrs = doc!.Data!.First().Attributes!;

        Assert.True(attrs.ContainsKey("title"));
        Assert.False(attrs.ContainsKey("body"));
        Assert.False(attrs.ContainsKey("viewCount"));
    }

    [Fact]
    public async Task Projection_IdAlwaysPresentRegardlessOfFields()
    {
        var response = await _client.GetAsync("/api/proj-articles?fields[articles]=title");

        var doc = await Deserialize(response);
        Assert.All(doc!.Data!, r => Assert.NotNull(r.Id));
    }

    [Fact]
    public async Task Projection_MultipleRequestedFields_AllPresent()
    {
        var response = await _client.GetAsync(
            "/api/proj-articles?fields[articles]=title,viewCount"
        );

        var doc = await Deserialize(response);
        var attrs = doc!.Data!.First().Attributes!;

        Assert.True(attrs.ContainsKey("title"));
        Assert.True(attrs.ContainsKey("viewCount"));
        Assert.False(attrs.ContainsKey("body"));
    }

    [Fact]
    public async Task Projection_WithInclude_IncludedDataPresent()
    {
        var response = await _client.GetAsync(
            "/api/proj-articles?fields[articles]=title&include=author"
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var doc = await Deserialize(response);
        Assert.NotNull(doc!.Included);
        Assert.NotEmpty(doc.Included!);
    }

    [Fact]
    public async Task Projection_WithPagination_ReturnsCorrectPage()
    {
        var response = await _client.GetAsync(
            "/api/proj-articles?fields[articles]=title&page[number]=1&page[size]=1"
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var doc = await Deserialize(response);
        Assert.Single(doc!.Data!);
    }

    [Fact]
    public async Task Projection_WithFilter_FiltersAppliedCorrectly()
    {
        var response = await _client.GetAsync(
            "/api/proj-articles?fields[articles]=title&filter[title]=First"
        );

        var doc = await Deserialize(response);
        Assert.Single(doc!.Data!);
        Assert.Equal("First", doc.Data!.First().Attributes!["title"].ToString());
    }

    [Fact]
    public async Task Projection_DisabledWhenNoFieldsSpecified_ReturnsAllAttributes()
    {
        // No fields param: projection should not run; all attributes returned
        var response = await _client.GetAsync("/api/proj-articles");

        var doc = await Deserialize(response);
        var attrs = doc!.Data!.First().Attributes!;

        Assert.True(attrs.ContainsKey("title"));
        Assert.True(attrs.ContainsKey("body"));
        Assert.True(attrs.ContainsKey("viewCount"));
    }

    [Fact]
    public async Task Projection_WithIncludeAndFields_SkipsProjectionAndReturnsIncludedData()
    {
        // EF Core silently drops all .Include() calls when .Select() is applied.
        // Projection must be skipped whenever any includes are active, otherwise navigation
        // properties would be null on a real relational database.
        var response = await _client.GetAsync(
            "/api/proj-articles?fields[articles]=title&include=author"
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var doc = await Deserialize(response);
        // Author should be included (nested include path starts with "author")
        Assert.NotNull(doc!.Included);
        Assert.NotEmpty(doc.Included!);
    }

    [Fact]
    public async Task Projection_ReturnsCorrectResourceCount()
    {
        var response = await _client.GetAsync("/api/proj-articles?fields[articles]=title");

        var doc = await Deserialize(response);
        Assert.Equal(2, doc!.Data!.Count());
    }

    private async Task<JsonApiCollectionDocument<ResourceObject>?> Deserialize(
        HttpResponseMessage response
    )
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<JsonApiCollectionDocument<ResourceObject>>(
            content,
            _jsonOptions
        );
    }

    public void Dispose()
    {
        _client.Dispose();
        _host.Dispose();
    }
}

// ---------------------------------------------------------------------------
// Test entities, DbContext, and controller for integration tests
// ---------------------------------------------------------------------------

public class ProjTestArticle
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public int ViewCount { get; set; }

    public int? AuthorId { get; set; }
    public ProjTestAuthor? Author { get; set; }
}

public class ProjTestAuthor
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public List<ProjTestArticle> Articles { get; set; } = new();
}

public class ProjTestDbContext : DbContext
{
    public DbSet<ProjTestArticle> Articles { get; set; } = null!;
    public DbSet<ProjTestAuthor> Authors { get; set; } = null!;

    public ProjTestDbContext(DbContextOptions<ProjTestDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProjTestArticle>().HasOne(a => a.Author).WithMany(au => au.Articles);
    }
}

[ApiController]
[Route("api/proj-articles")]
public class ProjTestArticlesController : JsonApiController
{
    private readonly ProjTestDbContext _db;

    public ProjTestArticlesController(ProjTestDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public Task<IActionResult> GetAsync() => JsonApiQueryAsync(_db.Articles, "articles");
}

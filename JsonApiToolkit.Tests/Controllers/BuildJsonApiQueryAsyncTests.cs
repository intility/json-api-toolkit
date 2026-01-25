using System.Net;
using System.Text.Json;
using JsonApiToolkit.Controllers;
using JsonApiToolkit.Extensions;
using JsonApiToolkit.Models.Querying;
using JsonApiToolkit.Services;
using JsonApiToolkit.Tests.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace JsonApiToolkit.Tests.Controllers;

/// <summary>
/// Tests for BuildJsonApiQueryAsync method.
/// </summary>
public class BuildJsonApiQueryAsyncTests : IDisposable
{
    private readonly IHost _host;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public BuildJsonApiQueryAsyncTests()
    {
        var databaseName = $"BuildQueryTestDb_{Guid.NewGuid()}";

        _host = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddDbContext<BuildQueryTestDbContext>(options =>
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
                            scope.ServiceProvider.GetRequiredService<BuildQueryTestDbContext>();
                        SeedTestData(context);
                    });
            })
            .Build();

        _host.Start();
        _client = _host.GetTestClient();
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    private static void SeedTestData(BuildQueryTestDbContext context)
    {
        var category1 = new BuildQueryCategory { Id = 1, Name = "Technology" };
        var category2 = new BuildQueryCategory { Id = 2, Name = "Science" };

        context.Categories.AddRange(category1, category2);

        var products = new List<BuildQueryProduct>
        {
            new()
            {
                Id = 1,
                Name = "Laptop",
                Price = 999.99m,
                InStock = true,
                Category = category1,
            },
            new()
            {
                Id = 2,
                Name = "Phone",
                Price = 599.99m,
                InStock = true,
                Category = category1,
            },
            new()
            {
                Id = 3,
                Name = "Microscope",
                Price = 1499.99m,
                InStock = false,
                Category = category2,
            },
            new()
            {
                Id = 4,
                Name = "Telescope",
                Price = 799.99m,
                InStock = true,
                Category = category2,
            },
            new()
            {
                Id = 5,
                Name = "Tablet",
                Price = 449.99m,
                InStock = true,
                Category = category1,
            },
        };

        context.Products.AddRange(products);
        context.SaveChanges();
    }

    #region Basic Query Tests

    [Fact]
    public async Task BuildJsonApiQueryAsync_WithNoParameters_ReturnsAllItemsAsync()
    {
        var response = await _client.GetAsync("/api/build-query/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<BuildQueryTestResponse>(content, _jsonOptions);

        Assert.NotNull(result);
        Assert.Equal(5, result.Count);
        Assert.Equal(5, result.TotalCount);
        Assert.False(result.HasPagination);
    }

    [Fact]
    public async Task BuildJsonApiQueryAsync_ReturnsCorrectTotalCount()
    {
        var response = await _client.GetAsync("/api/build-query/products?filter[inStock]=true");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<BuildQueryTestResponse>(content, _jsonOptions);

        Assert.NotNull(result);
        Assert.Equal(4, result.TotalCount); // 4 products in stock
        Assert.Equal(4, result.Count);
    }

    #endregion

    #region Filter Tests

    [Fact]
    public async Task BuildJsonApiQueryAsync_WithFilter_AppliesFilterCorrectly()
    {
        var response = await _client.GetAsync("/api/build-query/products?filter[name]=Laptop");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<BuildQueryTestResponse>(content, _jsonOptions);

        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        Assert.Equal(1, result.TotalCount);
        Assert.Contains("Laptop", result.ProductNames);
    }

    [Fact]
    public async Task BuildJsonApiQueryAsync_WithOperatorFilter_AppliesCorrectly()
    {
        var response = await _client.GetAsync("/api/build-query/products?filter[price][gt]=700");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<BuildQueryTestResponse>(content, _jsonOptions);

        Assert.NotNull(result);
        Assert.Equal(3, result.Count); // Laptop (999.99), Microscope (1499.99), Telescope (799.99)
    }

    [Fact]
    public async Task BuildJsonApiQueryAsync_WithMultipleFilters_AppliesAllFilters()
    {
        var response = await _client.GetAsync(
            "/api/build-query/products?filter[inStock]=true&filter[price][lt]=600"
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<BuildQueryTestResponse>(content, _jsonOptions);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count); // Phone (599.99) and Tablet (449.99), both in stock
    }

    #endregion

    #region Include Tests

    [Fact]
    public async Task BuildJsonApiQueryAsync_WithInclude_LoadsRelatedEntities()
    {
        var response = await _client.GetAsync("/api/build-query/products?include=category");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<BuildQueryTestResponse>(content, _jsonOptions);

        Assert.NotNull(result);
        Assert.True(result.HasIncludes);
        Assert.Equal(5, result.Count);
    }

    [Fact]
    public async Task BuildJsonApiQueryAsync_WithFilteredInclude_AppliesCorrectly()
    {
        var response = await _client.GetAsync(
            "/api/build-query/products?filter[category.name]=Technology&include=category"
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<BuildQueryTestResponse>(content, _jsonOptions);

        Assert.NotNull(result);
        Assert.Equal(3, result.Count); // Laptop, Phone, Tablet
        Assert.Equal(3, result.TotalCount);
    }

    #endregion

    #region Sorting Tests

    [Fact]
    public async Task BuildJsonApiQueryAsync_WithSort_AppliesSorting()
    {
        var response = await _client.GetAsync("/api/build-query/products?sort=price");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<BuildQueryTestResponse>(content, _jsonOptions);

        Assert.NotNull(result);
        Assert.Equal(5, result.Count);
        // First should be cheapest (Tablet: 449.99)
        Assert.Equal("Tablet", result.ProductNames.First());
    }

    [Fact]
    public async Task BuildJsonApiQueryAsync_WithDescendingSort_AppliesCorrectly()
    {
        var response = await _client.GetAsync("/api/build-query/products?sort=-price");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<BuildQueryTestResponse>(content, _jsonOptions);

        Assert.NotNull(result);
        Assert.Equal(5, result.Count);
        // First should be most expensive (Microscope: 1499.99)
        Assert.Equal("Microscope", result.ProductNames.First());
    }

    #endregion

    #region No Pagination Tests

    [Fact]
    public async Task BuildJsonApiQueryAsync_DoesNotApplyPagination()
    {
        // Even with pagination parameters, BuildJsonApiQueryAsync returns all matching items
        // (pagination params are parsed but NOT applied to the query)
        var response = await _client.GetAsync(
            "/api/build-query/products?page[number]=1&page[size]=2"
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<BuildQueryTestResponse>(content, _jsonOptions);

        Assert.NotNull(result);
        Assert.Equal(5, result.Count); // All 5 returned, not limited to page size of 2
        Assert.Equal(5, result.TotalCount);
        // HasPagination is true because params were parsed, but query returns ALL items
        Assert.True(result.HasPagination);
    }

    #endregion

    #region IncludeCount Parameter Tests

    [Fact]
    public async Task BuildJsonApiQueryAsync_WithIncludeCountFalse_ReturnsZeroTotalCount()
    {
        var response = await _client.GetAsync("/api/build-query/products-no-count");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<BuildQueryTestResponse>(content, _jsonOptions);

        Assert.NotNull(result);
        Assert.Equal(5, result.Count); // Actual count from query execution
        Assert.Equal(0, result.TotalCount); // TotalCount should be 0 when includeCount=false
    }

    #endregion

    #region Combined Operations Tests

    [Fact]
    public async Task BuildJsonApiQueryAsync_WithFilterSortInclude_AppliesAllOperations()
    {
        var response = await _client.GetAsync(
            "/api/build-query/products?filter[inStock]=true&sort=-price&include=category"
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<BuildQueryTestResponse>(content, _jsonOptions);

        Assert.NotNull(result);
        Assert.Equal(4, result.Count); // 4 in stock
        Assert.Equal(4, result.TotalCount);
        Assert.True(result.HasIncludes);
        // First should be most expensive in-stock item (Laptop: 999.99)
        Assert.Equal("Laptop", result.ProductNames.First());
    }

    [Fact]
    public async Task BuildJsonApiQueryAsync_ReturnsQueryParametersInResult()
    {
        var response = await _client.GetAsync(
            "/api/build-query/products?filter[inStock]=true&sort=name"
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<BuildQueryTestResponse>(content, _jsonOptions);

        Assert.NotNull(result);
        Assert.True(result.HasFilter);
        Assert.True(result.HasSort);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task BuildJsonApiQueryAsync_WithEmptyQueryString_ReturnsAllItems()
    {
        var response = await _client.GetAsync("/api/build-query/products?");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<BuildQueryTestResponse>(content, _jsonOptions);

        Assert.NotNull(result);
        Assert.Equal(5, result.Count);
    }

    [Fact]
    public async Task BuildJsonApiQueryAsync_WithNoMatchingFilter_ReturnsEmpty()
    {
        var response = await _client.GetAsync(
            "/api/build-query/products?filter[name]=NonexistentProduct"
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<BuildQueryTestResponse>(content, _jsonOptions);

        Assert.NotNull(result);
        Assert.Equal(0, result.Count);
        Assert.Equal(0, result.TotalCount);
    }

    #endregion

    public void Dispose()
    {
        _client?.Dispose();
        _host?.Dispose();
    }
}

#region Unit Tests

/// <summary>
/// Unit tests for BuildJsonApiQueryAsync using mocked dependencies.
/// </summary>
public class BuildJsonApiQueryAsyncUnitTests
{
    [Fact]
    public void JsonApiQueryResult_RequiredProperties_AreSet()
    {
        var products = new List<TestEntity>().AsQueryable();
        var parameters = new QueryParameters();

        var result = new JsonApiQueryResult<TestEntity>
        {
            Query = products,
            Parameters = parameters,
            TotalCount = 10,
        };

        Assert.Same(products, result.Query);
        Assert.Same(parameters, result.Parameters);
        Assert.Equal(10, result.TotalCount);
    }

    [Fact]
    public void JsonApiQueryResult_DefaultTotalCount_IsZero()
    {
        var products = new List<TestEntity>().AsQueryable();
        var parameters = new QueryParameters();

        var result = new JsonApiQueryResult<TestEntity>
        {
            Query = products,
            Parameters = parameters,
        };

        Assert.Equal(0, result.TotalCount);
    }
}

#endregion

#region Test Models and Infrastructure

public class BuildQueryTestResponse
{
    public int Count { get; set; }
    public int TotalCount { get; set; }
    public bool HasPagination { get; set; }
    public bool HasFilter { get; set; }
    public bool HasSort { get; set; }
    public bool HasIncludes { get; set; }
    public List<string> ProductNames { get; set; } = new();
}

public class BuildQueryProduct
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool InStock { get; set; }

    public int? CategoryId { get; set; }
    public BuildQueryCategory? Category { get; set; }
}

public class BuildQueryCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<BuildQueryProduct> Products { get; set; } = new();
}

public class BuildQueryTestDbContext : DbContext
{
    public DbSet<BuildQueryProduct> Products { get; set; } = null!;
    public DbSet<BuildQueryCategory> Categories { get; set; } = null!;

    public BuildQueryTestDbContext(DbContextOptions<BuildQueryTestDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .Entity<BuildQueryProduct>()
            .HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId);
    }
}

#endregion

#region Test Controller

[ApiController]
[Route("api/build-query")]
public class BuildQueryTestController : JsonApiController
{
    private readonly BuildQueryTestDbContext _context;

    public BuildQueryTestController(BuildQueryTestDbContext context)
    {
        _context = context;
    }

    [HttpGet("products")]
    public async Task<IActionResult> GetProducts()
    {
        var result = await BuildJsonApiQueryAsync(_context.Products, "products");

        var products = await result.Query.ToListAsync();

        return Ok(
            new BuildQueryTestResponse
            {
                Count = products.Count,
                TotalCount = result.TotalCount,
                HasPagination = result.Parameters.Pagination != null,
                HasFilter = result.Parameters.Filter != null,
                HasSort = result.Parameters.Sort?.Count > 0,
                HasIncludes = result.Parameters.Include?.Count > 0,
                ProductNames = products.Select(p => p.Name).ToList(),
            }
        );
    }

    [HttpGet("products-no-count")]
    public async Task<IActionResult> GetProductsNoCount()
    {
        var result = await BuildJsonApiQueryAsync(
            _context.Products,
            "products",
            includeCount: false
        );

        var products = await result.Query.ToListAsync();

        return Ok(
            new BuildQueryTestResponse
            {
                Count = products.Count,
                TotalCount = result.TotalCount,
                HasPagination = result.Parameters.Pagination != null,
                HasFilter = result.Parameters.Filter != null,
                HasSort = result.Parameters.Sort?.Count > 0,
                HasIncludes = result.Parameters.Include?.Count > 0,
                ProductNames = products.Select(p => p.Name).ToList(),
            }
        );
    }
}

#endregion

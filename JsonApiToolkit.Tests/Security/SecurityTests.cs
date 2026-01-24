using System.Net;
using System.Text.Json;
using JsonApiToolkit.Configuration;
using JsonApiToolkit.Controllers;
using JsonApiToolkit.Extensions;
using JsonApiToolkit.Models.Documents;
using JsonApiToolkit.Models.Errors;
using JsonApiToolkit.Models.Querying;
using JsonApiToolkit.Models.Querying.Filtering;
using JsonApiToolkit.Models.Resources;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace JsonApiToolkit.Tests.Security;

/// <summary>
/// Security tests for DoS protection, query limits, and bypass attempts.
/// </summary>
public class SecurityTests : IDisposable
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    #region Filter Count Limit Tests

    [Fact]
    public void Validate_With100Filters_ExceedsDefaultLimit_ThrowsBadRequest()
    {
        var options = new JsonApiOptions(); // Default MaxFilters = 50
        var filters = Enumerable
            .Range(1, 100)
            .Select(i => new FilterParameter { Field = $"field{i}", Value = $"value{i}" })
            .ToList();

        var parameters = new QueryParameters { Filter = new FilterGroup { Filters = filters } };

        var exception = Assert.Throws<JsonApiBadRequestException>(() =>
            QueryComplexityAnalyzer.Validate(parameters, options)
        );

        Assert.Contains("100 filters", exception.Message);
        Assert.Contains("maximum allowed is 50", exception.Message);
        Assert.Equal(JsonApiErrorCodes.QueryTooComplex, exception.Code);
    }

    [Fact]
    public void Validate_WithManyFiltersAcrossNestedGroups_CountsAllFilters()
    {
        var options = new JsonApiOptions { MaxFilters = 10 };

        // Create structure with 15 total filters spread across nested groups
        var parameters = new QueryParameters
        {
            Filter = new FilterGroup
            {
                Filters = Enumerable
                    .Range(1, 5)
                    .Select(i => new FilterParameter { Field = $"root{i}", Value = "v" })
                    .ToList(),
                Groups =
                [
                    new FilterGroup
                    {
                        LogicalOperator = LogicalOperator.Or,
                        Filters = Enumerable
                            .Range(1, 5)
                            .Select(i => new FilterParameter { Field = $"or{i}", Value = "v" })
                            .ToList(),
                    },
                    new FilterGroup
                    {
                        LogicalOperator = LogicalOperator.Not,
                        Filters = Enumerable
                            .Range(1, 5)
                            .Select(i => new FilterParameter { Field = $"not{i}", Value = "v" })
                            .ToList(),
                    },
                ],
            },
        };

        var exception = Assert.Throws<JsonApiBadRequestException>(() =>
            QueryComplexityAnalyzer.Validate(parameters, options)
        );

        Assert.Contains("15 filters", exception.Message);
    }

    [Fact]
    public void Validate_WithFiltersAtExactLimit_DoesNotThrow()
    {
        var options = new JsonApiOptions { MaxFilters = 50 };
        var filters = Enumerable
            .Range(1, 50)
            .Select(i => new FilterParameter { Field = $"field{i}", Value = $"value{i}" })
            .ToList();

        var parameters = new QueryParameters { Filter = new FilterGroup { Filters = filters } };

        var exception = Record.Exception(() =>
            QueryComplexityAnalyzer.Validate(parameters, options)
        );

        Assert.Null(exception);
    }

    #endregion

    #region Filter Group Limit Tests

    [Fact]
    public void Validate_With15FilterGroups_ExceedsDefaultLimit_ThrowsBadRequest()
    {
        var options = new JsonApiOptions(); // Default MaxFilterGroups = 10

        var parameters = new QueryParameters
        {
            Filter = new FilterGroup
            {
                Groups = Enumerable
                    .Range(1, 15)
                    .Select(_ => new FilterGroup
                    {
                        LogicalOperator = LogicalOperator.Or,
                        Filters = [new FilterParameter { Field = "f", Value = "v" }],
                    })
                    .ToList(),
            },
        };

        var exception = Assert.Throws<JsonApiBadRequestException>(() =>
            QueryComplexityAnalyzer.Validate(parameters, options)
        );

        Assert.Contains("15 filter groups", exception.Message);
        Assert.Contains("maximum allowed is 10", exception.Message);
    }

    [Fact]
    public void Validate_WithDeeplyNestedGroups_CountsTotalGroups()
    {
        var options = new JsonApiOptions { MaxFilterGroups = 5 };

        // Create 8 groups through nesting: 2 at level 1, each with 2, each with 1
        var parameters = new QueryParameters
        {
            Filter = new FilterGroup
            {
                Groups =
                [
                    new FilterGroup
                    {
                        Groups =
                        [
                            new FilterGroup
                            {
                                Filters = [new FilterParameter { Field = "a", Value = "1" }],
                            },
                            new FilterGroup
                            {
                                Filters = [new FilterParameter { Field = "b", Value = "2" }],
                            },
                        ],
                    },
                    new FilterGroup
                    {
                        Groups =
                        [
                            new FilterGroup
                            {
                                Filters = [new FilterParameter { Field = "c", Value = "3" }],
                            },
                            new FilterGroup
                            {
                                Filters = [new FilterParameter { Field = "d", Value = "4" }],
                            },
                        ],
                    },
                ],
            },
        };

        var exception = Assert.Throws<JsonApiBadRequestException>(() =>
            QueryComplexityAnalyzer.Validate(parameters, options)
        );

        Assert.Contains("filter groups", exception.Message);
    }

    #endregion

    #region Filter Depth Limit Tests

    [Fact]
    public void Validate_WithDepth5_ExceedsDefaultLimit_ThrowsBadRequest()
    {
        var options = new JsonApiOptions(); // Default MaxFilterDepth = 3

        var parameters = new QueryParameters
        {
            Filter = new FilterGroup
            {
                Groups =
                [
                    new FilterGroup
                    {
                        Groups =
                        [
                            new FilterGroup
                            {
                                Groups =
                                [
                                    new FilterGroup
                                    {
                                        Groups =
                                        [
                                            new FilterGroup
                                            {
                                                Filters =
                                                [
                                                    new FilterParameter
                                                    {
                                                        Field = "deep",
                                                        Value = "value",
                                                    },
                                                ],
                                            },
                                        ],
                                    },
                                ],
                            },
                        ],
                    },
                ],
            },
        };

        var exception = Assert.Throws<JsonApiBadRequestException>(() =>
            QueryComplexityAnalyzer.Validate(parameters, options)
        );

        Assert.Contains("nesting depth", exception.Message);
        Assert.Contains("JsonApiOptions.MaxFilterDepth", exception.Message);
    }

    [Fact]
    public void Validate_WithDepthAtLimit_DoesNotThrow()
    {
        var options = new JsonApiOptions { MaxFilterDepth = 3 };

        // Depth 3: root -> level1 -> level2
        var parameters = new QueryParameters
        {
            Filter = new FilterGroup
            {
                Groups =
                [
                    new FilterGroup
                    {
                        Groups =
                        [
                            new FilterGroup
                            {
                                Filters =
                                [
                                    new FilterParameter { Field = "level2", Value = "value" },
                                ],
                            },
                        ],
                    },
                ],
            },
        };

        var exception = Record.Exception(() =>
            QueryComplexityAnalyzer.Validate(parameters, options)
        );

        Assert.Null(exception);
    }

    #endregion

    #region Filter Value Length Limit Tests

    [Fact]
    public void Validate_WithFilterValue1500Chars_ExceedsDefaultLimit_ThrowsBadRequest()
    {
        var options = new JsonApiOptions(); // Default MaxFilterValueLength = 1000
        var longValue = new string('x', 1500);

        var parameters = new QueryParameters
        {
            Filter = new FilterGroup
            {
                Filters = [new FilterParameter { Field = "search", Value = longValue }],
            },
        };

        var exception = Assert.Throws<JsonApiBadRequestException>(() =>
            QueryComplexityAnalyzer.Validate(parameters, options)
        );

        Assert.Contains("1500 characters", exception.Message);
        Assert.Contains("maximum allowed is 1000", exception.Message);
        Assert.Contains("search", exception.Message);
    }

    [Fact]
    public void Validate_WithVeryLongFilterValue_InNestedGroup_ThrowsBadRequest()
    {
        var options = new JsonApiOptions { MaxFilterValueLength = 100 };
        var longValue = new string('a', 500);

        var parameters = new QueryParameters
        {
            Filter = new FilterGroup
            {
                Groups =
                [
                    new FilterGroup
                    {
                        LogicalOperator = LogicalOperator.Or,
                        Filters = [new FilterParameter { Field = "nested", Value = longValue }],
                    },
                ],
            },
        };

        var exception = Assert.Throws<JsonApiBadRequestException>(() =>
            QueryComplexityAnalyzer.Validate(parameters, options)
        );

        Assert.Contains("nested", exception.Message);
    }

    [Fact]
    public void Validate_WithFilterValueAtExactLimit_DoesNotThrow()
    {
        var options = new JsonApiOptions { MaxFilterValueLength = 100 };
        var exactValue = new string('x', 100);

        var parameters = new QueryParameters
        {
            Filter = new FilterGroup
            {
                Filters = [new FilterParameter { Field = "search", Value = exactValue }],
            },
        };

        var exception = Record.Exception(() =>
            QueryComplexityAnalyzer.Validate(parameters, options)
        );

        Assert.Null(exception);
    }

    #endregion

    #region Include Depth Limit Tests

    [Fact]
    public void Validate_WithIncludeDepth5_ExceedsDefaultLimit_ThrowsBadRequest()
    {
        var options = new JsonApiOptions(); // Default MaxIncludeDepth = 3

        var parameters = new QueryParameters
        {
            Include = ["author.posts.comments.likes.user"], // depth 5
        };

        var exception = Assert.Throws<JsonApiBadRequestException>(() =>
            QueryComplexityAnalyzer.Validate(parameters, options)
        );

        Assert.Contains("author.posts.comments.likes.user", exception.Message);
        Assert.Contains("depth 5", exception.Message);
        Assert.Contains("maximum allowed is 3", exception.Message);
        Assert.Equal(JsonApiErrorCodes.IncludeDepthExceeded, exception.Code);
    }

    [Fact]
    public void Validate_WithManyShallowIncludes_DoesNotThrow()
    {
        var options = new JsonApiOptions();

        // Many includes but all shallow (depth 1)
        var parameters = new QueryParameters
        {
            Include = Enumerable.Range(1, 50).Select(i => $"relation{i}").ToList(),
        };

        var exception = Record.Exception(() =>
            QueryComplexityAnalyzer.Validate(parameters, options)
        );

        Assert.Null(exception);
    }

    [Fact]
    public void Validate_WithIncludeAtExactDepthLimit_DoesNotThrow()
    {
        var options = new JsonApiOptions { MaxIncludeDepth = 3 };

        var parameters = new QueryParameters
        {
            Include = ["author.posts.comments"], // depth 3
        };

        var exception = Record.Exception(() =>
            QueryComplexityAnalyzer.Validate(parameters, options)
        );

        Assert.Null(exception);
    }

    [Fact]
    public void Validate_WithMultipleIncludesSomeExceedingLimit_ThrowsOnFirst()
    {
        var options = new JsonApiOptions { MaxIncludeDepth = 2 };

        var parameters = new QueryParameters
        {
            Include =
            [
                "author", // depth 1 - ok
                "comments.user", // depth 2 - ok
                "tags.category.parent", // depth 3 - exceeds
            ],
        };

        var exception = Assert.Throws<JsonApiBadRequestException>(() =>
            QueryComplexityAnalyzer.Validate(parameters, options)
        );

        Assert.Contains("tags.category.parent", exception.Message);
    }

    #endregion

    #region Custom Limit Configuration Tests

    [Fact]
    public void Validate_WithCustomHighLimits_AllowsComplexQueries()
    {
        var options = new JsonApiOptions
        {
            MaxFilters = 200,
            MaxFilterGroups = 50,
            MaxFilterDepth = 10,
            MaxFilterValueLength = 5000,
            MaxIncludeDepth = 10,
        };

        var filters = Enumerable
            .Range(1, 150)
            .Select(i => new FilterParameter { Field = $"field{i}", Value = $"value{i}" })
            .ToList();

        var parameters = new QueryParameters
        {
            Filter = new FilterGroup { Filters = filters },
            Include = ["a.b.c.d.e.f.g"], // depth 7
        };

        var exception = Record.Exception(() =>
            QueryComplexityAnalyzer.Validate(parameters, options)
        );

        Assert.Null(exception);
    }

    [Fact]
    public void Validate_WithLimitsDisabled_AllowsAnything()
    {
        var options = new JsonApiOptions
        {
            MaxFilters = int.MaxValue,
            MaxFilterGroups = int.MaxValue,
            MaxFilterDepth = int.MaxValue,
            MaxFilterValueLength = int.MaxValue,
            MaxIncludeDepth = int.MaxValue,
        };

        var filters = Enumerable
            .Range(1, 1000)
            .Select(i => new FilterParameter
            {
                Field = $"field{i}",
                Value = new string('x', 10000),
            })
            .ToList();

        var parameters = new QueryParameters
        {
            Filter = new FilterGroup { Filters = filters },
            Include = ["a.b.c.d.e.f.g.h.i.j.k.l.m.n.o.p"], // depth 16
        };

        var exception = Record.Exception(() =>
            QueryComplexityAnalyzer.Validate(parameters, options)
        );

        Assert.Null(exception);
    }

    [Fact]
    public void Validate_WithVeryStrictLimits_RejectsNormalQueries()
    {
        var options = new JsonApiOptions
        {
            MaxFilters = 1,
            MaxFilterGroups = 0,
            MaxFilterDepth = 1,
            MaxIncludeDepth = 1,
        };

        var parameters = new QueryParameters
        {
            Filter = new FilterGroup
            {
                Filters =
                [
                    new FilterParameter { Field = "a", Value = "1" },
                    new FilterParameter { Field = "b", Value = "2" },
                ],
            },
        };

        var exception = Assert.Throws<JsonApiBadRequestException>(() =>
            QueryComplexityAnalyzer.Validate(parameters, options)
        );

        Assert.Contains("2 filters", exception.Message);
        Assert.Contains("maximum allowed is 1", exception.Message);
    }

    #endregion

    #region Page Size Clamping Tests

    [Fact]
    public async Task Integration_PageSizeExceedsMax_IsClampedToMaxAsync()
    {
        using var host = CreateTestHost(options =>
        {
            options.MaxPageSize = 10;
        });

        host.Start();
        var client = host.GetTestClient();

        // Request page size of 100, but max is 10
        var response = await client.GetAsync("/api/items?page[size]=100");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonApiCollectionDocument<ResourceObject>>(
            content,
            _jsonOptions
        );

        Assert.NotNull(document?.Data);
        // Should be clamped to 10 (or less if fewer items exist)
        Assert.True(document.Data.Count() <= 10);
    }

    [Fact]
    public async Task Integration_DefaultPageSize_IsAppliedAsync()
    {
        using var host = CreateTestHost(options =>
        {
            options.DefaultPageSize = 5;
            options.MaxPageSize = 100;
        });

        host.Start();
        var client = host.GetTestClient();

        // No page size specified, should use default
        var response = await client.GetAsync("/api/items?page[number]=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonApiCollectionDocument<ResourceObject>>(
            content,
            _jsonOptions
        );

        Assert.NotNull(document?.Data);
        Assert.True(document.Data.Count() <= 5);
    }

    #endregion

    #region Integration Tests - Full HTTP Pipeline

    [Fact]
    public async Task Integration_QueryWithTooManyFilters_Returns400Async()
    {
        using var host = CreateTestHost(options =>
        {
            options.MaxFilters = 5;
        });

        host.Start();
        var client = host.GetTestClient();

        // Build query string with 10 filters
        var filterParams = string.Join(
            "&",
            Enumerable.Range(1, 10).Select(i => $"filter[field{i}]=value{i}")
        );

        var response = await client.GetAsync($"/api/items?{filterParams}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("filters", content.ToLower());
    }

    [Fact]
    public async Task Integration_QueryWithDeepInclude_Returns400Async()
    {
        using var host = CreateTestHost(options =>
        {
            options.MaxIncludeDepth = 2;
        });

        host.Start();
        var client = host.GetTestClient();

        var response = await client.GetAsync(
            "/api/items?include=parent.grandparent.greatgrandparent"
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("depth", content.ToLower());
    }

    [Fact]
    public async Task Integration_ValidComplexQuery_Returns200Async()
    {
        using var host = CreateTestHost();

        host.Start();
        var client = host.GetTestClient();

        // Complex but within limits
        var response = await client.GetAsync(
            "/api/items?filter[name][like]=test&filter[isActive]=true&sort=-createdAt&page[number]=1&page[size]=10"
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region Stress Tests

    [Fact]
    public void StressTest_CountingManyFilters_PerformsReasonably()
    {
        var options = new JsonApiOptions { MaxFilters = 10000 };

        // Create deeply nested structure with many filters
        var filters = Enumerable
            .Range(1, 5000)
            .Select(i => new FilterParameter { Field = $"field{i}", Value = $"value{i}" })
            .ToList();

        var parameters = new QueryParameters { Filter = new FilterGroup { Filters = filters } };

        // Should complete without timeout and not throw (under limit)
        var exception = Record.Exception(() =>
            QueryComplexityAnalyzer.Validate(parameters, options)
        );

        Assert.Null(exception);
    }

    [Fact]
    public void StressTest_DeeplyNestedGroups_DoesNotCauseStackOverflow()
    {
        var options = new JsonApiOptions { MaxFilterDepth = 100, MaxFilterGroups = 100 };

        // Create 50 levels of nesting
        FilterGroup deepGroup = new()
        {
            Filters = [new FilterParameter { Field = "leaf", Value = "value" }],
        };

        for (int i = 0; i < 50; i++)
        {
            deepGroup = new FilterGroup { Groups = [deepGroup] };
        }

        var parameters = new QueryParameters { Filter = deepGroup };

        // Should complete without stack overflow
        var exception = Record.Exception(() =>
            QueryComplexityAnalyzer.Validate(parameters, options)
        );

        Assert.Null(exception);
    }

    #endregion

    #region Error Response Structure Tests

    [Fact]
    public void ErrorResponse_ContainsRequiredMetadata()
    {
        var options = new JsonApiOptions { MaxFilters = 5 };

        var parameters = new QueryParameters
        {
            Filter = new FilterGroup
            {
                Filters = Enumerable
                    .Range(1, 10)
                    .Select(i => new FilterParameter { Field = $"f{i}", Value = "v" })
                    .ToList(),
            },
        };

        var exception = Assert.Throws<JsonApiBadRequestException>(() =>
            QueryComplexityAnalyzer.Validate(parameters, options)
        );

        // Verify error has all required metadata
        Assert.Equal(JsonApiErrorCodes.QueryTooComplex, exception.Code);
        Assert.NotNull(exception.ErrorSource);
        Assert.Equal("filter", exception.ErrorSource.Parameter);
        Assert.NotNull(exception.Meta);
        Assert.Contains("limit", exception.Meta.Keys);
        Assert.Contains("actual", exception.Meta.Keys);
        Assert.Contains("configKey", exception.Meta.Keys);
    }

    [Fact]
    public void ErrorResponse_IncludeDepthExceeded_ContainsIncludePath()
    {
        var options = new JsonApiOptions { MaxIncludeDepth = 2 };

        var parameters = new QueryParameters { Include = ["author.posts.comments.likes"] };

        var exception = Assert.Throws<JsonApiBadRequestException>(() =>
            QueryComplexityAnalyzer.Validate(parameters, options)
        );

        Assert.Equal(JsonApiErrorCodes.IncludeDepthExceeded, exception.Code);
        Assert.NotNull(exception.Meta);
        Assert.Equal("author.posts.comments.likes", exception.Meta["includePath"]);
        Assert.Equal(4, exception.Meta["depth"]);
    }

    #endregion

    #region Test Infrastructure

    private static IHost CreateTestHost(Action<JsonApiOptions>? configureOptions = null)
    {
        var databaseName = $"SecurityTestDb_{Guid.NewGuid()}";

        return new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddDbContext<SecurityTestDbContext>(options =>
                            options.UseInMemoryDatabase(databaseName)
                        );
                        services.AddControllers();
                        services.AddJsonApiToolkit(configureOptions ?? (_ => { }));
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
                            scope.ServiceProvider.GetRequiredService<SecurityTestDbContext>();
                        SeedTestData(context);
                    });
            })
            .Build();
    }

    private static void SeedTestData(SecurityTestDbContext context)
    {
        var items = Enumerable
            .Range(1, 50)
            .Select(i => new SecurityTestItem
            {
                Id = i,
                Name = $"Item {i}",
                IsActive = i % 2 == 0,
                CreatedAt = DateTime.Now.AddDays(-i),
            })
            .ToList();

        context.Items.AddRange(items);
        context.SaveChanges();
    }

    public void Dispose()
    {
        // Cleanup handled by individual test hosts
    }

    #endregion
}

#region Test Entities and Infrastructure

public class SecurityTestItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public SecurityTestItem? Parent { get; set; }
    public int? ParentId { get; set; }
}

public class SecurityTestDbContext : DbContext
{
    public DbSet<SecurityTestItem> Items { get; set; } = null!;

    public SecurityTestDbContext(DbContextOptions<SecurityTestDbContext> options)
        : base(options) { }
}

[ApiController]
[Route("api/items")]
public class SecurityTestItemsController : JsonApiController
{
    private readonly SecurityTestDbContext _context;

    public SecurityTestItemsController(SecurityTestDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetItems()
    {
        return await JsonApiQueryAsync(_context.Items, "items");
    }
}

#endregion

# Contributing Guide

This guide covers development practices, testing patterns, and contribution workflow for JsonApiToolkit.

## Development Setup

### Prerequisites

- .NET 9.0 SDK
- Git
- An IDE (VS Code, Rider, or Visual Studio)

### Building

```bash
# Restore dependencies
dotnet restore

# Build
dotnet build --configuration Release

# Run tests
dotnet test --configuration Release

# Format code
dotnet csharpier format .
```

## Testing Patterns

### Test Organization

Tests are organized by component in the `JsonApiToolkit.Tests` project:

```
JsonApiToolkit.Tests/
├── Configuration/          # JsonApiOptions, QueryComplexityAnalyzer
├── Controllers/            # JsonApiController behavior
├── Extensions/
│   ├── Filtering/          # FilterHandler, FilterExpressionBuilder
│   ├── Pagination/         # PaginationHandler
│   ├── Sorting/            # SortingHandler
│   └── QueryHelpersTests.cs
├── Filters/                # JsonApiExceptionFilter
├── Integration/            # Full HTTP pipeline tests
├── Mapping/                # JsonApiMapper, InclusionMapper, EntityMapper
├── Models/                 # Test entities and model tests
├── Parsing/                # JsonApiQueryParser
├── Security/               # DoS protection, bypass attempts
└── Validation/             # IncludeValidator, AllowedIncludes
```

### Test Naming Convention

Follow the pattern: `MethodName_Scenario_ExpectedBehavior`

```csharp
// Good examples
[Fact]
public void ApplyPagination_WithPageSizeZero_ClampsToOne() { }

[Fact]
public void ConvertToPropertyType_WithInvalidGuid_ThrowsFormatException() { }

[Fact]
public async Task GetArticles_FilterSortPaginate_AppliesAllOperationsAsync() { }
```

### Test Categories

#### 1. Unit Tests

Test individual methods in isolation:

```csharp
[Fact]
public void CountFilters_WithNestedGroups_CountsAllFilters()
{
    var group = new FilterGroup
    {
        Filters = [new() { Field = "a", Value = "1" }],
        Groups = [new FilterGroup { Filters = [new() { Field = "b", Value = "2" }] }],
    };

    int count = QueryComplexityAnalyzer.CountFilters(group);

    Assert.Equal(2, count);
}
```

#### 2. Integration Tests

Test the full HTTP pipeline with TestServer:

```csharp
[Fact]
public async Task GetArticles_WithPagination_ReturnsCorrectPageAsync()
{
    var response = await _client.GetAsync("/api/articles?page[number]=1&page[size]=2");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var document = JsonSerializer.Deserialize<JsonApiCollectionDocument<ResourceObject>>(
        await response.Content.ReadAsStringAsync(),
        _jsonOptions
    );

    Assert.Equal(2, document?.Data?.Count());
}
```

#### 3. Boundary Tests

Test edge cases and limits:

```csharp
[Theory]
[InlineData(0, 1)]      // Zero clamps to 1
[InlineData(-1, 1)]     // Negative clamps to 1
[InlineData(int.MaxValue, 100)] // Exceeds max, clamps to max
public void ApplyPagination_WithBoundaryPageSize_ClampsCorrectly(
    int inputSize,
    int expectedSize)
{
    // Test implementation
}
```

#### 4. Error Condition Tests

Test that errors are handled correctly:

```csharp
[Fact]
public void ConvertToPropertyType_WithInvalidInt_ThrowsFormatException()
{
    var exception = Assert.Throws<FormatException>(() =>
        QueryHelpers.ConvertToPropertyType("not-a-number", typeof(int))
    );

    Assert.Contains("Failed to convert filter value", exception.Message);
}
```

### Test Infrastructure

#### In-Memory Database

Use EF Core in-memory database for integration tests:

```csharp
public class TestDbContext : DbContext
{
    public DbSet<TestEntity> Entities { get; set; } = null!;

    public TestDbContext(DbContextOptions<TestDbContext> options)
        : base(options) { }
}

// In test setup
services.AddDbContext<TestDbContext>(options =>
    options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
);
```

#### Test Server Setup

```csharp
_host = new HostBuilder()
    .ConfigureWebHost(webBuilder =>
    {
        webBuilder
            .UseTestServer()
            .ConfigureServices(services =>
            {
                services.AddDbContext<TestDbContext>(options =>
                    options.UseInMemoryDatabase(databaseName)
                );
                services.AddControllers();
                services.AddJsonApiToolkit();
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints => endpoints.MapControllers());

                // Seed data
                using var scope = app.ApplicationServices.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
                SeedTestData(context);
            });
    })
    .Build();
```

### Coverage Requirements

Before merging to main:

- All public methods must have tests
- All filter operators must have positive and negative tests
- All query handlers must have boundary tests
- Integration tests must cover the full query pipeline

## Code Style

### Formatting

All code is formatted with CSharpier. Run before committing:

```bash
dotnet csharpier format .
```

CI will fail if code is not formatted.

### Naming Conventions

| Element | Convention | Example |
|---------|-----------|---------|
| Classes | PascalCase | `FilterHandler` |
| Interfaces | IPascalCase | `IFilterHandler` |
| Methods | PascalCase | `ApplyPagination` |
| Parameters | camelCase | `queryParameters` |
| Private fields | _camelCase | `_logger` |
| Constants | PascalCase | `DefaultPageSize` |

### Documentation

- Public APIs must have XML documentation
- Include `<summary>`, `<param>`, and `<returns>` where applicable
- Keep comments concise and meaningful

## Git Workflow

### Branching

Use conventional commit prefixes for branch names:

| Type | Branch | Example |
|------|--------|---------|
| Bug fix | `fix/` | `fix/pagination-zero-divide` |
| Feature | `feat/` | `feat/sparse-fieldsets` |
| Tests | `test/` | `test/security-tests` |
| Docs | `docs/` | `docs/contributing-guide` |
| Refactor | `refactor/` | `refactor/di-interfaces` |

### Commit Messages

Follow conventional commits:

```bash
# Bug fixes
git commit -m "fix: prevent division by zero in pagination"

# Features
git commit -m "feat: add sparse fieldsets support"

# Tests
git commit -m "test: add security tests for DoS protection"

# Breaking changes
git commit -m "feat!: require DI in JsonApiController"
```

### Pull Requests

1. Create a branch from `main`
2. Make changes with conventional commits
3. Ensure all tests pass: `dotnet test`
4. Ensure code is formatted: `dotnet csharpier check .`
5. Create PR with descriptive title and bullet-point description
6. Wait for CI checks to pass
7. Squash merge to main

## Release Process

Releases are managed by release-please. When PRs are merged to main:

1. release-please creates/updates a Release PR
2. The Release PR accumulates changes based on conventional commits
3. When ready, merge the Release PR
4. This triggers a GitHub Release and NuGet publish

### Version Bumps

| Commit Prefix | Version Bump |
|---------------|--------------|
| `fix:` | Patch (1.0.x) |
| `feat:` | Minor (1.x.0) |
| `feat!:` or `fix!:` | Major (x.0.0) |
| `docs:`, `test:`, `refactor:` | No bump |

## Questions?

Open an issue on GitHub: https://github.com/Intility/JsonApiToolkit/issues

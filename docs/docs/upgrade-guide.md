# Upgrade Guide

This document tracks all breaking changes, new features, and migration steps for each version of JsonApiToolkit.

**Current Version:** 1.2.5

---

## .NET 10 Upgrade (November 2025)

**Target Version:** 2.3.0 or 3.0.0 (TBD)

**Timeline:** After .NET 10 GA release (November 2025)

**Changes:**
- [ ] Update target framework from `net9.0` to `net10.0`
- [ ] Update Microsoft.* dependencies to .NET 10 versions
- [ ] Review and adopt new C# 14 language features where beneficial
- [ ] Update GitHub Actions to use `dotnet-version: 10.x`
- [ ] Update documentation prerequisites

**Breaking Changes:**
- Minimum runtime requirement changes from .NET 9 to .NET 10
- Applications must upgrade to .NET 10 to use new package versions

**Migration:**
1. Update your project to target .NET 10
2. Update JsonApiToolkit package to the new version

**Multi-targeting consideration:**
If there's demand, we may multi-target `net9.0;net10.0` for one release cycle to ease migration.

---

## Version 2.x (Upcoming - Breaking Changes)

### v2.2.0 - Database Projection

**Release Date:** TBD

**New Features:**
- [ ] Database-level projection - only fetch requested columns from database
- [ ] Opt-in via `EnableDatabaseProjection` option
- [ ] Massive performance improvement for entities with JSON columns

**Configuration:**
```csharp
services.AddJsonApiToolkit(options => {
    options.EnableDatabaseProjection = true;
});
```

**Breaking Changes:** None (opt-in feature)

**Migration:** None required

---

### v2.1.0 - Sparse Fieldsets

**Release Date:** TBD

**New Features:**
- [ ] `fields[type]` query parameter support (JSON:API sparse fieldsets)
- [ ] Reduces response payload size by returning only requested attributes
- [ ] Works with included resources: `fields[author]=name,email`

**Usage:**
```
GET /articles?fields[articles]=title,body&include=author&fields[author]=name
```

**Response:**
```json
{
  "data": {
    "type": "articles",
    "id": "1",
    "attributes": {
      "title": "Hello World",
      "body": "..."
    }
  },
  "included": [{
    "type": "author",
    "id": "5",
    "attributes": {
      "name": "John Doe"
    }
  }]
}
```

**Breaking Changes:** None

**Migration:** None required

---

### v2.0.0 - Architecture Refactor (MAJOR BREAKING CHANGE)

**Release Date:** TBD

**New Features:**
- [ ] Full dependency injection support
- [ ] All core components now injectable and mockable
- [ ] Interfaces for all handlers (`IFilterHandler`, `ISortHandler`, etc.)
- [ ] Extended `JsonApiOptions` (introduced in v1.4.0) with more configuration

**Breaking Changes:**

#### 1. Controller Constructor Signature Changed

**Before (v1.x):**
```csharp
public class BooksController : JsonApiController
{
    private readonly AppDbContext _db;

    public BooksController(AppDbContext db)
    {
        _db = db;
    }
}
```

**After (v2.0):**
```csharp
public class BooksController : JsonApiController
{
    private readonly AppDbContext _db;

    public BooksController(
        AppDbContext db,
        ILogger<BooksController> logger,
        IJsonApiMapper mapper,
        IJsonApiQueryParser queryParser,
        IOptions<JsonApiOptions> options)
        : base(logger, mapper, queryParser, options)
    {
        _db = db;
    }
}
```

#### 2. Static Extension Methods Changed

Most users won't notice this change - the base controller methods (`JsonApiQueryAsync`, `JsonApiOk`, etc.) continue to work as before. However, if you were calling static methods directly:

**Before (v1.x):**
```csharp
var document = JsonApiMapper.ToDocument(entity, "books");
```

**After (v2.0):**
```csharp
// Use the inherited Mapper property from JsonApiController
var document = Mapper.ToDocument(entity, "books");
```

**For advanced usage** - if you need direct handler access outside controller methods:
```csharp
public class BooksController : JsonApiController
{
    private readonly IFilterHandler _filterHandler;

    public BooksController(
        AppDbContext db,
        IFilterHandler filterHandler,  // Inject directly if needed
        ILogger<BooksController> logger,
        IJsonApiMapper mapper,
        IJsonApiQueryParser queryParser,
        IOptions<JsonApiOptions> options)
        : base(logger, mapper, queryParser, options)
    {
        _db = db;
        _filterHandler = filterHandler;
    }
}
```

#### 3. New Service Registrations

All services are now automatically registered by `AddJsonApiToolkit()`, but if you were manually resolving services, the types have changed:

| Before (v1.x) | After (v2.0) |
|---------------|--------------|
| `JsonApiMapper` (static) | `IJsonApiMapper` (scoped) |
| `EntityMapper` (static) | `IEntityMapper` (scoped) |
| N/A | `IFilterHandler` (scoped) |
| N/A | `ISortHandler` (scoped) |
| N/A | `IPaginationHandler` (scoped) |
| N/A | `IIncludeHandler` (scoped) |

**Migration Steps:**

1. Update all controller constructors to accept required dependencies
2. Call `base(...)` with the new parameters
3. Replace static method calls with injected service calls
4. Update any manual service resolutions

**Compatibility Helper (Deprecated):**

For easier migration, v2.0 includes deprecated static wrappers that will be removed in v3.0:

```csharp
// These work but emit deprecation warnings
[Obsolete("Use IJsonApiMapper instead")]
public static class JsonApiMapper { ... }
```

---

## Version 1.x

### v1.5.0 - Test Coverage

**Release Date:** TBD

**Changes:**
- [ ] Comprehensive test suite added
- [ ] No API changes

**Breaking Changes:** None

---

### v1.4.0 - Security Hardening

**Release Date:** TBD

**New Features:**
- [ ] `JsonApiOptions` configuration class
- [ ] Configurable query complexity limits
- [ ] AllowedIncludes now validates filter paths (not just includes)

**Configuration:**
```csharp
services.AddJsonApiToolkit(options => {
    options.MaxFilters = 50;           // Default: 50
    options.MaxFilterGroups = 10;      // Default: 10
    options.MaxFilterDepth = 3;        // Default: 3
    options.MaxFilterValueLength = 1000; // Default: 1000
    options.MaxIncludeDepth = 4;       // Default: 4
    options.MaxPageSize = 100;         // Default: 100
    options.DefaultPageSize = 10;      // Default: 10
});
```

**Breaking Changes:**

#### 1. Query Complexity Limits Enforced

Queries exceeding limits now return 400 Bad Request:

```json
{
  "errors": [{
    "status": "400",
    "title": "Bad Request",
    "detail": "Query exceeds maximum filter count of 50"
  }]
}
```

**Migration:** If your application uses complex queries, increase limits via configuration.

#### 2. Filter Path Validation with AllowedIncludes

Dot-notation filters are now validated against `AllowedIncludes`:

**Before (v1.3):**
```csharp
[AllowedIncludes("profile")]
public async Task<IActionResult> GetUsers()
{
    // filter[admin.password][like]=% would work (security hole!)
}
```

**After (v1.4):**
```csharp
[AllowedIncludes("profile")]
public async Task<IActionResult> GetUsers()
{
    // filter[admin.password][like]=% returns 403 Forbidden
}
```

**Migration:** Add relationships to `AllowedIncludes` if you need to filter on them.

---

### v1.3.0 - Bug Fixes & Error Improvements

**Release Date:** TBD

**Bug Fixes:**
- [ ] Fixed exception swallowing in InclusionMapper (now properly logged)
- [ ] Fixed unsafe string parsing in filter parser
- [ ] Fixed potential division by zero in pagination
- [ ] Added defensive checks for reflection method lookups
- [ ] Removed dead code (`AddIncludedResourcesRecursive`)

**New Features:**
- [ ] `JsonApiErrorCodes` - Standard error codes for consistent error identification
- [ ] `JsonApiErrors` - Factory methods for creating rich, well-structured errors

**Usage:**
```csharp
// Before - verbose, missing metadata
throw new JsonApiNotFoundException("Book not found");

// After - concise, consistent, includes metadata
throw JsonApiErrors.NotFound("books", id);
```

Produces:
```json
{
  "errors": [{
    "status": "404",
    "code": "RESOURCE_NOT_FOUND",
    "title": "Not Found",
    "detail": "Resource 'books' with id '123' not found",
    "meta": {
      "resourceType": "books",
      "id": "123"
    }
  }]
}
```

**Available Factories:**
| Factory | Status | Use Case |
|---------|--------|----------|
| `JsonApiErrors.NotFound(type, id)` | 404 | Resource not found |
| `JsonApiErrors.InvalidFilterValue(field, value, type)` | 400 | Type conversion failed |
| `JsonApiErrors.InvalidFilterField(field, entityType)` | 400 | Field doesn't exist |
| `JsonApiErrors.IncludeNotAllowed(include)` | 403 | Include blocked by AllowedIncludes |
| `JsonApiErrors.AlreadyExists(type, field, value)` | 409 | Duplicate key violation |
| `JsonApiErrors.ValidationFailed(field, message)` | 400 | Generic validation error |

**Breaking Changes:** None

**Migration:** None required (existing exception classes still work)

---

### v1.2.5 - Current Release

Current stable version.

---

## Deprecation Schedule

| Feature | Deprecated In | Removed In | Replacement |
|---------|---------------|------------|-------------|
| Static `JsonApiMapper` class | v2.0.0 | v3.0.0 | `IJsonApiMapper` service |
| Static `EntityMapper` class | v2.0.0 | v3.0.0 | `IEntityMapper` service |
| Static extension methods | v2.0.0 | v3.0.0 | Injected handler services |

---

## FAQ

### Q: Do I need to update all my controllers at once for v2.0?

No. The deprecated static methods will continue to work in v2.0 (with warnings). You can migrate controllers incrementally. However, they will be removed in v3.0.

### Q: Will sparse fieldsets slow down my API?

In v2.1, sparse fieldsets only filter at serialization time - the database still loads all columns. In v2.2 with projection enabled, the database query itself is optimized.

### Q: How do I know if my queries exceed the new limits?

Enable debug logging for `JsonApiToolkit` to see query complexity metrics:

```json
{
  "Logging": {
    "LogLevel": {
      "JsonApiToolkit": "Debug"
    }
  }
}
```

### Q: Can I disable the new security limits?

Yes, set them to high values:

```csharp
services.AddJsonApiToolkit(options => {
    options.MaxFilters = int.MaxValue;
    options.MaxFilterDepth = int.MaxValue;
    // Not recommended for production!
});
```

# Upgrade Guide

This document tracks all breaking changes, new features, and migration steps for each version of JsonApiToolkit.

**Current Version:** 1.6.0

---

## .NET 10 Upgrade (November 2025)

**Target Version:** 2.0.0

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

## Upcoming Features

### v1.8.0 - Database Projection

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

### v1.7.0 - Sparse Fieldsets

**Release Date:** February 2026

**New Features:**
- [x] `fields[type]` query parameter support (JSON:API sparse fieldsets)
- [x] Reduces response payload size by returning only requested attributes
- [x] Works with included resources: `fields[author]=name,email`

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

## Released Versions

### v1.6.0 - Query Builder API

**Release Date:** January 2026

**New Features:**
- [x] `BuildJsonApiQueryAsync<T>()` method for custom query execution
- [x] Returns processed `IQueryable<T>` with filters, includes, and sorting applied
- [x] Pagination is intentionally NOT applied - use for exports, aggregations, projections
- [x] Optional `includeCount` parameter to skip COUNT query for performance

**Usage:**

```csharp
[HttpGet("export")]
public async Task<IActionResult> ExportBooks()
{
    // Get processed query WITHOUT pagination
    var result = await BuildJsonApiQueryAsync(_context.Books, "books");

    // Execute however you need
    var books = await result.Query.ToListAsync();

    // result.TotalCount has the filtered count
    // result.Parameters has parsed query params
    return Ok(books);
}
```

**Use Cases:**
- CSV/Excel exports - need all matching records
- Aggregations - GROUP BY after filtering
- Custom projections - Select specific columns
- Streaming large datasets

**New Types:**
- `JsonApiQueryResult<T>` - Result class with `Query`, `Parameters`, and `TotalCount` properties

**Breaking Changes:** None (additive only)

**Migration:** None required

**Documentation:** See [Building Custom Queries](build-query.md)

---

### v1.5.0 - Test Coverage

**Release Date:** January 2026

**Changes:**
- [x] Comprehensive test suite with 570+ tests
- [x] Handler tests: SortingHandler, PaginationHandler, InclusionMapper, NestedPropertyNavigator
- [x] Integration tests: JsonApiQueryAsync full pipeline
- [x] Security tests: DoS protection, query limit enforcement
- [x] Type conversion tests: All 15+ supported filter types
- [x] Edge case tests: Circular references, boundary values, error conditions
- [x] No API changes

**Breaking Changes:** None

**What's Tested:**
| Category | Tests | Coverage |
|----------|-------|----------|
| Filtering | 50+ | All operators, nested properties, type conversions |
| Sorting | 20+ | Multi-field, invalid fields, direction |
| Pagination | 30+ | Boundary values (0, -1, MAX_INT), clamping |
| Includes | 40+ | Nested, circular references, deduplication |
| Security | 26 | DoS limits, bypass attempts, stress tests |
| Integration | 40+ | Full HTTP pipeline, combined operations |

---

### v1.4.0 - Security Hardening

**Release Date:** January 2026

**New Features:**
- [x] `JsonApiOptions` configuration class for query limits
- [x] Configurable query complexity limits (filters, groups, depth, page size)
- [x] AllowedIncludes now validates filter paths (not just includes)
- [x] Recursion depth guard for nested filter groups

**Configuration:**
```csharp
services.AddJsonApiToolkit(options => {
    options.MaxFilters = 50;           // Default: 50
    options.MaxFilterGroups = 10;      // Default: 10
    options.MaxFilterDepth = 3;        // Default: 3
    options.MaxFilterValueLength = 1000; // Default: 1000
    options.MaxIncludeDepth = 3;       // Default: 3
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
- [x] Fixed exception swallowing in InclusionMapper (dead code removed)
- [x] Fixed unsafe string parsing in filter parser
- [x] Fixed potential division by zero in pagination
- [x] Added defensive checks for reflection method lookups
- [x] Removed dead code (`AddIncludedResourcesRecursive`)

**New Features:**
- [x] `JsonApiErrorCodes` - Standard error codes for consistent error identification
- [x] `JsonApiErrors` - Factory methods for creating rich, well-structured errors

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

### v1.2.5

Previous stable version before refactoring began.

---

## FAQ

### Q: Will sparse fieldsets slow down my API?

In v1.6, sparse fieldsets only filter at serialization time - the database still loads all columns. In v1.7 with projection enabled, the database query itself is optimized.

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

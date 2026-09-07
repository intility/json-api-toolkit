# Security

Guidance for using JsonApiToolkit safely: restricting includes, configuring query complexity limits, and rejecting bad pagination.

> [!NOTE]
> To **report a vulnerability** in JsonApiToolkit itself, see the [Security Policy](https://github.com/intility/json-api-toolkit/blob/main/SECURITY.md). This page is about using the toolkit securely.

> [!NOTE]
> JsonApiToolkit does not perform authentication or authorization. It builds queries and serializes responses inside your controllers. Protecting endpoints with `[Authorize]`, policies, and per-resource ownership checks is the responsibility of the consuming application.

## `[AllowedIncludes]`

Without `[AllowedIncludes]`, every navigation property on your entities is includable via `?include=`. That can leak sensitive relationships and run expensive queries. The attribute restricts which relationships clients can request.

```csharp
[HttpGet]
[AllowedIncludes("author", "reviews")]
public async Task<IActionResult> GetAllAsync()
{
    return await JsonApiQueryAsync(_db.Books, ResourceType);
}
```

Forbidden includes return 403 with the requested, forbidden, and allowed lists in `meta`.

### Wildcards

```csharp
[AllowedIncludes("author.*", "comments")]
```

- `author.*` allows `author` and `author.profile`, but not `author.profile.settings`.
- `*` allows any top-level include, but no nesting.
- `[AllowedIncludes()]` (empty) allows no includes at all.

Matching is case-insensitive. Invalid patterns (e.g., `**`) log a warning at startup.

### Filter-path validation

Dot-notation filter paths are validated against the same list. With `[AllowedIncludes("author")]`, `filter[author.name]=John` works but `filter[admin.role]=X` returns 403. This only kicks in when the attribute is present. Without it, all filter paths are allowed.

## Query complexity limits

The toolkit enforces configurable limits on query complexity to prevent resource exhaustion. Configure them via `JsonApiOptions`:

```csharp
builder.Services.AddJsonApiToolkit(options =>
{
    options.MaxFilters = 50;             // max filter conditions
    options.MaxFilterGroups = 10;        // max OR/NOT blocks
    options.MaxFilterDepth = 3;          // max group nesting
    options.MaxFilterValueLength = 1000; // max value string length
    options.MaxIncludeDepth = 3;         // max include path depth
    options.MaxPageSize = 100;           // max items per page
    options.DefaultPageSize = 10;
    options.StrictPagination = false;      // reject vs clamp invalid pagination
    options.StrictQueryValidation = false; // reject vs skip invalid query params
});
```

| Limit | Default | Behavior when exceeded |
|-------|---------|------------------------|
| `MaxFilters` | 50 | 400 Bad Request |
| `MaxFilterGroups` | 10 | 400 Bad Request |
| `MaxFilterDepth` | 3 | 400 Bad Request |
| `MaxFilterValueLength` | 1000 | 400 Bad Request |
| `MaxIncludeDepth` | 3 | 400 Bad Request |
| `MaxPageSize` | 100 | Clamped, or 400 if `StrictPagination` |

Exceeded limits return:

```json
{
  "errors": [{
    "status": "400",
    "code": "QUERY_TOO_COMPLEX",
    "title": "Query exceeds complexity limits",
    "detail": "Query contains 75 filters, but maximum allowed is 50.",
    "source": { "parameter": "filter" },
    "meta": { "limit": 50, "actual": 75, "configKey": "JsonApiOptions.MaxFilters" }
  }]
}
```

Raise the limits if your application genuinely needs them, but monitor query performance when you do.

## Strict query validation

By default, invalid query parameters are logged at warning level and skipped:
the request returns 200 and the bad parameter simply has no effect. Enable
`StrictQueryValidation` to return 400 Bad Request instead:

```csharp
builder.Services.AddJsonApiToolkit(options => options.StrictQueryValidation = true);
```

With strict query validation on, the first invalid parameter fails the request:

| Request shape | Error code |
|---------------|------------|
| Unknown filter field (`filter[bogus]=x`) | `INVALID_FILTER_FIELD` |
| Unconvertible filter value (`filter[createdAt]=abc`) | `INVALID_FILTER_VALUE` |
| Unknown filter operator (`filter[title][contains]=x`) | `INVALID_FILTER_OPERATOR` |
| Unknown sort field (`sort=bogus`, `sort=author.name`) | `INVALID_SORT_FIELD` |
| `filter[and][...]` or nested groups | `UNSUPPORTED_FILTER_GROUP` |
| Bracket include-filter without matching `include` | `FILTER_NOT_ALLOWED` |
| Malformed filter key (`filter[x`) | `VALIDATION_FAILED` |

Without the flag, unconvertible filter values on non-string properties surface
as 500 Internal Server Error; strict mode turns them into descriptive 400s.

## Strict pagination

By default, invalid pagination is silently clamped to valid ranges (page 0 → 1, page 99999 → last page, oversized page → `MaxPageSize`). Enable `StrictPagination` to return errors instead:

```csharp
builder.Services.AddJsonApiToolkit(options => options.StrictPagination = true);
```

With strict pagination on:

- `page[number] < 1` → 400 Bad Request
- `page[size] < 1` or `> MaxPageSize` → 400 Bad Request
- `page[number]` greater than total pages → 404 Not Found (only when results exist)

```json
{
  "errors": [{
    "status": "400",
    "code": "INVALID_PAGE_SIZE",
    "title": "Invalid page size",
    "detail": "Page size '200' exceeds maximum allowed size of 100.",
    "source": { "parameter": "page[size]" },
    "meta": { "value": 200, "max": 100, "configKey": "JsonApiOptions.MaxPageSize" }
  }]
}
```

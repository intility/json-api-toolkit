# Security

JsonApiToolkit provides security features to control which relationships can be included in JSON:API responses.

## AllowedIncludes Attribute

The `[AllowedIncludes]` attribute restricts which relationships clients can request via the `include` query parameter. This prevents exposure of sensitive relationships and protects against potentially expensive queries.

### Basic Usage

Apply the attribute to controller actions:

```csharp
[HttpGet("users")]
[AllowedIncludes("profile", "posts")]
public async Task<IActionResult> GetUsers()
{
    return await JsonApiQueryAsync(_context.Users, "user");
}
```

### Wildcard Patterns

Use wildcards to allow nested includes at specific levels:

```csharp
[HttpGet("posts")]
[AllowedIncludes("author.*", "comments")]
public async Task<IActionResult> GetPosts()
{
    return await JsonApiQueryAsync(_context.Posts, "post");
}
```

**Wildcard Rules:**
- `author.*` allows `author` and `author.profile` but not `author.profile.settings`
- `*` allows all top-level includes but no nested ones

### Configuration Options

**Empty array** - No includes allowed:
```csharp
[AllowedIncludes()]
```

**No attribute** - All includes allowed (default behavior)

### Error Responses

When forbidden includes are requested, a 403 Forbidden response is returned:

```json
{
  "errors": [{
    "status": "403",
    "title": "Forbidden Include",
    "detail": "The requested include 'sensitive' was not found",
    "meta": {
      "requestedIncludes": ["profile", "sensitive"],
      "forbiddenIncludes": ["sensitive"],
      "allowedIncludes": ["profile", "posts"]
    }
  }]
}
```

### Case Sensitivity

All matching is case-insensitive:
- `Author` matches `author`
- `author.*` matches `Author.Posts`

### Pattern Validation

Invalid patterns are logged as warnings during application startup:

```
AllowedIncludesAttribute validation warnings for UsersController.GetUsers:
Pattern 'user.**' contains '**' which is not supported. Use single '*' for wildcards.
```

## Query Complexity Limits

JsonApiToolkit enforces configurable limits on query complexity to prevent resource exhaustion attacks.

### Configuration

Configure limits via `JsonApiOptions` in your `Program.cs`:

```csharp
builder.Services.AddJsonApiToolkit(options => {
    options.MaxFilters = 50;           // Max filter conditions (default: 50)
    options.MaxFilterGroups = 10;      // Max OR/NOT blocks (default: 10)
    options.MaxFilterDepth = 3;        // Max group nesting depth (default: 3)
    options.MaxFilterValueLength = 1000; // Max value string length (default: 1000)
    options.MaxIncludeDepth = 3;       // Max include path depth (default: 3)
    options.MaxPageSize = 100;         // Max page size, clamped (default: 100)
    options.DefaultPageSize = 10;      // Default when not specified (default: 10)
});
```

### Limit Behaviors

| Option | Behavior When Exceeded |
|--------|----------------------|
| `MaxFilters` | Returns 400 Bad Request |
| `MaxFilterGroups` | Returns 400 Bad Request |
| `MaxFilterDepth` | Returns 400 Bad Request |
| `MaxFilterValueLength` | Returns 400 Bad Request |
| `MaxIncludeDepth` | Returns 400 Bad Request |
| `MaxPageSize` | Silently clamped to max value |

### Error Response

When limits are exceeded, a 400 Bad Request is returned with details:

```json
{
  "errors": [{
    "status": "400",
    "code": "QUERY_TOO_COMPLEX",
    "title": "Query exceeds complexity limits",
    "detail": "Query contains 75 filters, but maximum allowed is 50. Reduce filter count or configure a higher limit via JsonApiOptions.MaxFilters.",
    "source": { "parameter": "filter" },
    "meta": {
      "limit": 50,
      "actual": 75,
      "configKey": "JsonApiOptions.MaxFilters"
    }
  }]
}
```

## Filter Path Validation

When using `[AllowedIncludes]`, dot-notation filter paths are also validated against the allowed list.

### How It Works

If you use `filter[author.name]=John` (filtering the primary resource by a related entity's field), the `author` relationship must be in the `AllowedIncludes` list:

```csharp
[HttpGet("posts")]
[AllowedIncludes("author", "comments")]
public async Task<IActionResult> GetPosts()
{
    // filter[author.name]=John ✓ - allowed
    // filter[author.bio]=X ✓ - allowed
    // filter[admin.role]=X ✗ - 403 Forbidden (admin not in AllowedIncludes)
    return await JsonApiQueryAsync(_context.Posts, "post");
}
```

### Error Response

When filtering on a non-allowed relationship:

```json
{
  "errors": [{
    "status": "403",
    "title": "Forbidden",
    "detail": "Filtering on relationship 'admin' is not allowed. Add 'admin' to AllowedIncludes or remove the filter."
  }]
}
```

> [!NOTE]
> This validation only applies when `[AllowedIncludes]` is present. Without the attribute, all filter paths are allowed.

## DoS Protection

The query complexity limits protect against several denial-of-service attack vectors:

### Resource Exhaustion
Complex queries with many filters or deep nesting can cause excessive CPU usage. Limits on `MaxFilters`, `MaxFilterGroups`, and `MaxFilterDepth` prevent attackers from crafting queries that overwhelm the server.

### Stack Overflow
Deeply nested filter groups could cause stack overflow during recursive expression building. The `MaxFilterDepth` limit and internal recursion guards prevent this.

### Memory Exhaustion
Very large filter values or responses could exhaust server memory. The `MaxFilterValueLength` and `MaxPageSize` limits bound memory usage per request.

### Recommended Defaults

The default limits are designed to handle typical API usage while blocking abuse:

| Limit | Default | Rationale |
|-------|---------|-----------|
| MaxFilters | 50 | Covers complex search UIs |
| MaxFilterGroups | 10 | Allows OR chains for search |
| MaxFilterDepth | 3 | Entity → Relationship → Property |
| MaxFilterValueLength | 1000 | Long enough for UUIDs, GUIDs, text search |
| MaxIncludeDepth | 3 | Matches filter depth |
| MaxPageSize | 100 | Prevents large data dumps |

> [!TIP]
> If your application requires higher limits, increase them via configuration. Monitor query performance when raising limits.
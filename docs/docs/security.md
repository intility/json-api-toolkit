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
    return await JsonApiOkAsync(_context.Users, "user");
}
```

### Wildcard Patterns

Use wildcards to allow nested includes at specific levels:

```csharp
[HttpGet("posts")]
[AllowedIncludes("author.*", "comments")]
public async Task<IActionResult> GetPosts()
{
    return await JsonApiOkAsync(_context.Posts, "post");
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
    "detail": "The requested include 'sensitive' is not allowed for this endpoint",
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
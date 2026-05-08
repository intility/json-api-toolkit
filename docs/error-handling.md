# Error Handling

Throw `JsonApi*Exception` types in your code and the toolkit converts them into JSON:API error responses with the right HTTP status. Only unhandled exceptions get logged with stack traces; handled errors log just type and message.

## Exception types

| Exception | Status | Use case |
|-----------|--------|----------|
| `JsonApiBadRequestException` | 400 | Validation or malformed input |
| `JsonApiUnauthorizedException` | 401 | Not authenticated |
| `JsonApiForbiddenException` | 403 | Not authorized |
| `JsonApiNotFoundException` | 404 | Resource not found |
| `JsonApiConflictException` | 409 | Unique constraint or conflict |
| `JsonApiTooManyRequestsException` | 429 | Rate limited |

Anything else becomes a 500 with full stack trace in logs.

> [!IMPORTANT]
> Don't wrap controller actions in a `try/catch` that swallows everything. The toolkit's exception filter needs the exception to bubble up.

## Basic usage

```csharp
if (string.IsNullOrWhiteSpace(request.Title))
    throw new JsonApiBadRequestException("Title cannot be empty");

var todo = await _db.Todos.FirstOrDefaultAsync(t => t.Id == id)
    ?? throw new JsonApiNotFoundException($"Todo {id} not found");
```

The client gets:

```json
{
  "errors": [
    {
      "status": "404",
      "title": "Not Found",
      "detail": "Todo 42 not found."
    }
  ]
}
```

## Adding error codes, source pointers, and meta

Every `JsonApi*Exception` accepts optional `code`, `errorSource`, and `meta` arguments for richer responses:

```csharp
throw new JsonApiBadRequestException(
    message: "Invalid email format",
    code: "INVALID_EMAIL",
    errorSource: new ErrorSource { Pointer = "/data/attributes/email" },
    meta: new Dictionary<string, object>
    {
        ["expectedFormat"] = "user@domain.com",
        ["provided"] = request.Email
    }
);
```

Produces:

```json
{
  "errors": [{
    "status": "400",
    "title": "Bad Request",
    "detail": "Invalid email format",
    "code": "INVALID_EMAIL",
    "source": { "pointer": "/data/attributes/email" },
    "meta": { "expectedFormat": "user@domain.com", "provided": "..." }
  }]
}
```

For query-parameter errors, set `errorSource: new ErrorSource { Parameter = "sort" }` instead of a pointer.

## Factory methods

For common errors, the `JsonApiErrors` factory produces consistent codes, sources, and meta without spelling them out each time:

```csharp
var book = await _db.Books.FindAsync(id)
    ?? throw JsonApiErrors.NotFound(ResourceType, id);

if (await _db.Users.AnyAsync(u => u.Email == email))
    throw JsonApiErrors.AlreadyExists("users", "email", email);

if (string.IsNullOrWhiteSpace(request.Title))
    throw JsonApiErrors.RequiredFieldMissing("title");
```

The full list of factory methods (and the standard error codes they emit) is in the [API reference for `JsonApiErrors`](api/JsonApiToolkit.Models.Errors/JsonApiErrors.md) and [`JsonApiErrorCodes`](api/JsonApiToolkit.Models.Errors/JsonApiErrorCodes.md). Use those codes in client code to handle errors programmatically.

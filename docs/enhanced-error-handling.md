# Enhanced Error Handling in JsonApiToolkit

JsonApiToolkit provides a clean, consistent way to handle errors in your ASP.NET Core APIs. By throwing specific exceptions in your services or controllers, you get:

- **Standardized JSON:API error responses** for your clients
- **Clear, minimal logging**: only unexpected errors include stack traces

## Supported Exceptions

Throw these exceptions in your code to trigger the corresponding HTTP status and error response:

| Exception Type                    | HTTP Status | Typical Use Case                      |
|-----------------------------------|-------------|---------------------------------------|
| `JsonApiBadRequestException`      | 400         | Validation or malformed input         |
| `JsonApiUnauthorizedException`    | 401         | Not authenticated                     |
| `JsonApiForbiddenException`       | 403         | Not authorized                        |
| `JsonApiNotFoundException`        | 404         | Resource not found                    |
| `JsonApiConflictException`        | 409         | Unique constraint or conflict         |
| `JsonApiTooManyRequestsException` | 429         | Rate limiting exceeded                |

Any other unhandled exception will result in a 500 Internal Server Error.

> [!TIP]
> If you are missing an exception type for your use case, please create an issue on GitHub.

## How It Works

- Throw a specific exception (e.g., `JsonApiNotFoundException`, `JsonApiBadRequestException`) in your code when an error occurs.
- The toolkit automatically converts this into the correct HTTP status code and a JSON:API error response.
- Only unexpected errors (500) are logged with stack traces; handled errors (400, 404, etc.) log just the type and message.

## Example Usage

```csharp
if (string.IsNullOrWhiteSpace(request.Title))
    throw new JsonApiBadRequestException("Todo title cannot be empty.");

var todo = await _dbContext.Todos.FirstOrDefaultAsync(t => t.Id == todoId)
    ?? throw new JsonApiNotFoundException($"Todo with ID {todoId} not found.");
```

---

## Example Client Response

If a todo is not found, the client receives:

```json
{
  "errors": [
    {
      "status": "404",
      "title": "Not Found",
      "detail": "Todo with ID 42 not found."
    }
  ]
}
```

---

## Example Console Log

For the same error, your log will show:

```
[09:07:11 INF] Handled JSON:API exception: JsonApiNotFoundException - Todo with ID 42 not found.
```

*No stack trace is logged for handled errors like 400, 404, or 409.*

---

## Advanced Error Information

All JSON:API exceptions support additional structured error information following the JSON:API specification:

### Enhanced Constructor

```csharp
public JsonApiBadRequestException(
    string message,
    string? code = null,
    ErrorSource? errorSource = null,
    Dictionary<string, object>? meta = null,
    Exception? innerException = null
)
```

### Usage with Additional Error Details

```csharp
// Basic usage (existing code continues to work)
throw new JsonApiBadRequestException("Invalid email format");

// Enhanced usage with error codes and source information
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

// For query parameter errors
throw new JsonApiBadRequestException(
    message: "Invalid sort field 'invalidField'",
    code: "INVALID_SORT",
    errorSource: new ErrorSource { Parameter = "sort" }
);
```

### Enhanced Error Response

The enhanced exception produces richer error responses:

```json
{
  "errors": [
    {
      "status": "400",
      "title": "Bad Request", 
      "detail": "Invalid email format",
      "code": "INVALID_EMAIL",
      "source": {
        "pointer": "/data/attributes/email"
      },
      "meta": {
        "expectedFormat": "user@domain.com",
        "provided": "invalid-email"
      }
    }
  ]
}
```

> [!IMPORTANT]
> When using these exceptions, ensure that the parent is not wrapped in a try-catch block that catches all exceptions. This will prevent the toolkit from handling the error correctly.

---

## Error Factory Methods (v1.3.0+)

For common error scenarios, use the `JsonApiErrors` factory class to create consistent, well-structured errors with proper codes, source information, and metadata:

### Available Factory Methods

| Factory | Status | Use Case |
|---------|--------|----------|
| `JsonApiErrors.NotFound(type, id)` | 404 | Resource not found |
| `JsonApiErrors.RelatedNotFound(type, id, relationship, relatedId)` | 404 | Related resource not found |
| `JsonApiErrors.InvalidFilterValue(field, value, expectedType)` | 400 | Type conversion failed |
| `JsonApiErrors.InvalidFilterField(field, entityType)` | 400 | Field doesn't exist |
| `JsonApiErrors.InvalidFilterOperator(op)` | 400 | Unknown filter operator |
| `JsonApiErrors.InvalidSortField(field, entityType)` | 400 | Sort field doesn't exist |
| `JsonApiErrors.IncludeNotAllowed(include)` | 403 | Include blocked by AllowedIncludes |
| `JsonApiErrors.FilterNotAllowed(relationshipPath)` | 403 | Filter on disallowed relationship |
| `JsonApiErrors.AlreadyExists(type, field, value)` | 409 | Duplicate key violation |
| `JsonApiErrors.ValidationFailed(field, message)` | 400 | Generic validation error |
| `JsonApiErrors.RequiredFieldMissing(field)` | 400 | Required field not provided |
| `JsonApiErrors.QueryTooComplex(limitName, limit, actual, configKey)` | 400 | Query exceeds limits |

### Usage Examples

```csharp
// Resource not found
var book = await _db.Books.FindAsync(id)
    ?? throw JsonApiErrors.NotFound("books", id);

// Invalid filter value
if (!int.TryParse(filterValue, out _))
    throw JsonApiErrors.InvalidFilterValue("age", filterValue, typeof(int));

// Duplicate resource
if (await _db.Users.AnyAsync(u => u.Email == email))
    throw JsonApiErrors.AlreadyExists("users", "email", email);

// Validation error
if (string.IsNullOrWhiteSpace(request.Title))
    throw JsonApiErrors.RequiredFieldMissing("title");
```

### Example Response

Using `JsonApiErrors.NotFound("books", 123)` produces:

```json
{
  "errors": [{
    "status": "404",
    "code": "RESOURCE_NOT_FOUND",
    "title": "Not Found",
    "detail": "Resource 'books' with id '123' not found.",
    "meta": {
      "resourceType": "books",
      "id": 123
    }
  }]
}
```

### Standard Error Codes

All factory methods use codes from `JsonApiErrorCodes`:

```csharp
public static class JsonApiErrorCodes
{
    public const string ResourceNotFound = "RESOURCE_NOT_FOUND";
    public const string ResourceAlreadyExists = "RESOURCE_ALREADY_EXISTS";
    public const string InvalidFilterField = "INVALID_FILTER_FIELD";
    public const string InvalidFilterValue = "INVALID_FILTER_VALUE";
    public const string InvalidFilterOperator = "INVALID_FILTER_OPERATOR";
    public const string FilterNotAllowed = "FILTER_NOT_ALLOWED";
    public const string IncludeNotAllowed = "INCLUDE_NOT_ALLOWED";
    public const string InvalidSortField = "INVALID_SORT_FIELD";
    public const string QueryTooComplex = "QUERY_TOO_COMPLEX";
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string RequiredFieldMissing = "REQUIRED_FIELD_MISSING";
    // ... and more
}
```

Use these codes in your client applications to handle specific error types programmatically.

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

> [!IMPORTANT]
> If used in a service, ensure the service call is not wrapped in a try-catch block that catches all exceptions. This would prevent the toolkit from handling the error correctly.

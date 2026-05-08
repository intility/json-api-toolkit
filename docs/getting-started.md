# Getting Started

This guide walks you through installing and configuring JsonApiToolkit in your .NET application.

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download) or later.
- An ASP.NET Core project.

## Installation

```bash
dotnet add package Intility.JsonApiToolkit
```

## Setup

1. **Register services:**
   In your `Program.cs` or `Startup.cs`, add the toolkit services to your dependency injection container:

   ```csharp
   builder.Services.AddJsonApiToolkit();
   ```

   You can also configure options for query limits and pagination:

   ```csharp
   builder.Services.AddJsonApiToolkit(options => {
       options.MaxFilters = 100;                  // Default: 50
       options.MaxFilterGroups = 20;              // Default: 10
       options.MaxFilterDepth = 5;                // Default: 3
       options.MaxPageSize = 200;                 // Default: 100
       options.DefaultPageSize = 25;              // Default: 10
       options.StrictPagination = true;           // Default: false
       options.EnableDatabaseProjection = true;   // Default: false
   });
   ```

   > [!TIP]
   > See the [Security](security.md#query-complexity-limits) documentation for security options and [Performance](performance.md) for database projection.

2. **Inherit from `JsonApiController`:**
   Your controller gets `JsonApiQueryAsync`, `JsonApiOk`, `JsonApiCreated`, etc. Convention is to declare a `ResourceType` constant per controller so the resource type string is defined once.

   ```csharp
   [ApiController]
   [Route("api/[controller]")]
   public class BooksController : JsonApiController
   {
       private const string ResourceType = "book";
       private readonly AppDbContext _context;

       public BooksController(AppDbContext context) => _context = context;

       [HttpGet]
       public async Task<IActionResult> GetAllAsync()
       {
           return await JsonApiQueryAsync(_context.Books, ResourceType);
       }
   }
   ```

3. **Configuration is automatic:**
   `AddJsonApiToolkit()` registers JSON serialization settings (camelCase, ignore nulls) and the `application/vnd.api+json` media type.

## Example

With the controller above, this request:

```
GET /api/books?filter[title][like]=Hobbit&include=author&page[size]=5&sort=-publishedDate
```

Returns a JSON:API document:

```json
{
  "data": [
    {
      "id": "1",
      "type": "book",
      "attributes": { "title": "The Hobbit", "publishedDate": "1937-09-21" },
      "relationships": {
        "author": { "data": { "id": "1", "type": "author" } }
      }
    }
  ],
  "included": [
    { "id": "1", "type": "author", "attributes": { "name": "J.R.R. Tolkien" } }
  ],
  "meta": { "totalCount": 1, "totalPages": 1 },
  "links": { "self": "...", "first": "...", "last": "..." }
}
```

Filtering, sorting, pagination, and includes all happen without extra code. See [Querying](querying.md) for the full parameter reference and [Recipes](recipes.md) for common scenarios.


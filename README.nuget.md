# JsonApiToolkit

Build [JSON:API](https://jsonapi.org/) endpoints in ASP.NET Core. Translates JSON:API query parameters (`filter[]`, `sort`, `include`, `fields[]`, `page[]`) into typed EF Core queries and shapes responses as spec-compliant documents, so your controllers stay short.

## Install

```sh
dotnet add package Intility.JsonApiToolkit
```

## Quick example

Register in `Program.cs`:

```csharp
builder.Services.AddJsonApiToolkit();
```

Derive controllers from `JsonApiController`:

```csharp
public class BooksController : JsonApiController
{
    private const string ResourceType = "book";

    [HttpGet]
    public async Task<IActionResult> GetAllAsync()
    {
        return await JsonApiQueryAsync(_db.Books, ResourceType);
    }
}
```

Then call with JSON:API query parameters:

```
GET /api/books?filter[title]=Hobbit&include=author&fields[book]=title&page[size]=10&sort=-published
```

Filtering, sorting, includes, sparse fieldsets, and pagination all work without extra code.

## Links

- **Documentation**: <https://intility.github.io/json-api-toolkit/>
- **Source**: <https://github.com/intility/json-api-toolkit>
- **Issues**: <https://github.com/intility/json-api-toolkit/issues>
- **License**: MIT

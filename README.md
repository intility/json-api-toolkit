<h1 align="center">
  <img src="https://avatars.githubusercontent.com/u/35199565" width="124px"/><br/>
  JsonApiToolkit
</h1>

<p align="center">
    <em>Build JSON:API endpoints in ASP.NET Core.</em>
</p>
<p align="center">
    <a href="https://dotnet.microsoft.com/">
        <img src="https://img.shields.io/badge/.NET-10.0-blue.svg?logo=dotnet&logoColor=white&label=.NET" alt=".NET version">
    </a>
    <a href="https://www.nuget.org/packages/Intility.JsonApiToolkit">
        <img src="https://img.shields.io/nuget/v/Intility.JsonApiToolkit.svg?logo=nuget&logoColor=white&label=NuGet" alt="NuGet">
    </a>
    <a href="https://jsonapi.org/">
        <img src="https://img.shields.io/badge/JSON%3AAPI-1.1-blue.svg" alt="JSON:API version">
    </a>
    <a href="https://github.com/intility/json-api-toolkit/blob/main/LICENSE">
        <img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="License">
    </a>
    <a href="https://github.com/intility/json-api-toolkit/actions/workflows/ci-cd.yml">
        <img src="https://github.com/intility/json-api-toolkit/actions/workflows/ci-cd.yml/badge.svg" alt="CI/CD">
    </a>
</p>

## Description

JsonApiToolkit translates [JSON:API](https://jsonapi.org/) query parameters (`filter[]`, `sort`, `include`, `fields[]`, `page[]`) into typed EF Core queries and shapes responses as spec-compliant documents, so your ASP.NET Core controllers stay short.

## Installation

```bash
dotnet add package Intility.JsonApiToolkit
```

## Usage

Register the toolkit in `Program.cs`:

```csharp
builder.Services.AddJsonApiToolkit();
```

Derive controllers from `JsonApiController` and let `JsonApiQueryAsync` handle the request:

```csharp
public class BooksController : JsonApiController
{
    private const string ResourceType = "book";

    [HttpGet]
    [AllowedIncludes("author", "publisher")]
    public async Task<IActionResult> GetAllAsync()
    {
        return await JsonApiQueryAsync(_dbContext.Books, ResourceType);
    }
}
```

Then call the endpoint with JSON:API query parameters:

```
GET /api/books?filter[title]=javascript&include=author&fields[book]=title,published&page[size]=10&sort=-published
```

## What it provides

- **JSON:API documents** - compliant `data` / `included` / `meta` / `links` / `errors` envelope on every response
- **Filtering** - `filter[field]=value` with operators, nested paths, and filtering on included resources
- **Sorting** - `sort=field,-other` with multi-field and descending support
- **Pagination** - `page[number]` / `page[size]` with link generation, total counts, and clamping
- **Sparse fieldsets** - `fields[type]=a,b` to limit returned attributes per resource type
- **Included resources** - `include=author,publisher.country` with allowlisting via `[AllowedIncludes]`
- **EF Core integration** - query operators translate directly to SQL via `IQueryable`
- **Strict mode** - return 404 for out-of-range pages and validate filter paths against allowed includes

## Documentation

Full documentation is at <https://intility.github.io/json-api-toolkit/>.


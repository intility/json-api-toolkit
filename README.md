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

## Packages

| Package | Purpose |
|---------|---------|
| [`Intility.JsonApiToolkit`](https://www.nuget.org/packages/Intility.JsonApiToolkit) | ASP.NET Core library: filtering, sorting, pagination, includes, sparse fieldsets, errors. |
| [`Intility.JsonApiToolkit.TypeGen`](https://www.nuget.org/packages/Intility.JsonApiToolkit.TypeGen) | .NET tool that generates TypeScript types from your C# models. |
| [`@intility/json-api-client`](https://jsr.io/@intility/json-api-client) | Typed TypeScript client, query builder, and TanStack Query adapter. |

See [End to end](https://intility.github.io/json-api-toolkit/end-to-end/) for how the three fit together.

## Documentation

Full documentation is at <https://intility.github.io/json-api-toolkit/>.


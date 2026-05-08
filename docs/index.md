# JsonApiToolkit

Build [JSON:API](https://jsonapi.org/) endpoints in ASP.NET Core.

## What is JSON:API?

[JSON:API](https://jsonapi.org/) is a specification for how a client should request and modify resources, and how a server should respond. It standardizes filtering, sorting, pagination, sparse fieldsets, and including related resources, so every API doesn't reinvent its own query syntax.

A typical JSON:API request looks like:

```
GET /api/books?filter[title][like]=Hobbit&include=author&fields[book]=title&page[size]=10&sort=-published
```

The response is a structured document with `data`, `included`, `meta`, `links`, and `errors` sections.

## What this toolkit does

JsonApiToolkit translates JSON:API query parameters into typed EF Core queries and returns spec-compliant response documents, so your controllers stay short:

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

Filtering, sorting, includes, sparse fieldsets, and pagination all work without extra code.

## When to use it

Reach for JsonApiToolkit when:

- You want a standard query syntax across endpoints instead of bespoke filter parameters.
- Your data model maps cleanly to resources with relationships (typical CRUD APIs over EF Core).
- You want consistent error envelopes and content negotiation without writing them yourself.

It's a less opinionated alternative to [JsonApiDotNetCore](https://github.com/json-api-dotnet/JsonApiDotNetCore): you keep your own controllers, DbContext, and routing, and the toolkit slots in where you want JSON:API semantics. If you need full convention-over-configuration with auto-generated routes and resource definitions, [JsonApiDotNetCore](https://github.com/json-api-dotnet/JsonApiDotNetCore) may fit better.

## Where to start

- New here? [Getting Started](getting-started.md) walks you through installing and registering the toolkit.
- Building queries? [Querying](querying.md) covers the supported parameters, [Building Custom Queries](build-query.md) covers exports and aggregations.
- Locking it down? [Security](security.md) covers `[AllowedIncludes]` and query complexity limits.
- Stuck? [Troubleshooting](troubleshooting.md) covers logging and common pitfalls.

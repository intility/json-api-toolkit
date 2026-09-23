# End to end: API, types, client

JsonApiToolkit ships as three packages that work together:

| Package | What it does |
|---------|--------------|
| `Intility.JsonApiToolkit` (NuGet) | Turns JSON:API query parameters into EF Core queries and shapes the response. |
| `Intility.JsonApiToolkit.TypeGen` (NuGet tool) | Generates TypeScript types and resource descriptors from your C# models. |
| `@intility/json-api-client` (JSR) | Typed fetch client, query builder, and hydration for the browser or Node. |

This page walks through all three with one `Article` resource. The runnable
version is `samples/ContractApi` in the repository.

## 1. Build the API

```bash
dotnet add package Intility.JsonApiToolkit
```

Mark each resource with `[JsonApiResource]`. The name is the wire `type`:

```csharp
[JsonApiResource("authors")]
public class Author
{
    public int Id { get; set; }
    public required string Name { get; set; }
}

[JsonApiResource("articles")]
public class Article
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public string? Body { get; set; }
    public Author? Author { get; set; }
}
```

Register the toolkit. `UseResourceAttributeTypeNames` makes included
resources use the attribute name, so they match the generated descriptors:

```csharp
builder.Services.AddJsonApiToolkit(options =>
{
    options.UseResourceAttributeTypeNames = true;
});
```

Write the controller:

```csharp
[ApiController]
[Route("articles")]
public class ArticlesController(AppDbContext db) : JsonApiController
{
    private const string ResourceType = "articles";

    [HttpGet]
    [AllowedIncludes("author")]
    public Task<IActionResult> GetAllAsync() =>
        JsonApiQueryAsync(db.Articles, ResourceType);

    [HttpGet("{id:int}")]
    public Task<IActionResult> GetByIdAsync(int id) =>
        JsonApiOkAsync(db.Articles.Where(a => a.Id == id), ResourceType);
}
```

See [Getting Started](getting-started.md) and [Querying](querying.md) for the
rest of the server side.

## 2. Generate the TypeScript types

```bash
dotnet tool install Intility.JsonApiToolkit.TypeGen --prerelease
dotnet build -c Release
jsonapi-typegen --assembly bin/Release/net10.0/MyApi.dll --out api-types.gen.ts
```

The output has one interface and one descriptor per resource:

```ts
export interface Article {
  id: string;
  title: string;
  body: string | null;
  author: Author | null;
}

export const Article: JsonApiResourceDescriptor<Article> = {
  type: "articles",
  attributes: ["title", "body"],
  toOne: ["author"],
  toMany: [],
};
```

Add `--check` to the same command in CI. It exits non-zero when the checked-in
file is stale. See [TypeScript type generation](typegen.md).

## 3. Call the API from TypeScript

```bash
deno add jsr:@intility/json-api-client
# or: npx jsr add @intility/json-api-client
```

```ts
import { createJsonApiClient } from "@intility/json-api-client";
import { Article } from "./api-types.gen.ts";

const client = createJsonApiClient({ baseUrl: "/api" });
const articles = client.resource(Article);

const { data, pagination } = await articles.list((q) =>
  q.filter("author.name", "like", "Berg").include("author").page(1, 20)
);

const article = await articles.get(1, (q) => q.include("author"));
const created = await articles.post({ title: "Hello" });
await articles.patch(created.id, { title: "Renamed" });
await articles.delete(created.id);
```

The descriptor gives the client the path, the wire type, and the relationship
names. Hydration fills in what the wire omits. A non-2xx response throws
`JsonApiRequestError`. See [TypeScript client](typescript-client.md) for the
query builder, error handling, and the TanStack Query adapter.

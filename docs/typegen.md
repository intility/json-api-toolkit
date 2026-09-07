# TypeScript type generation

`dotnet jsonapi-typegen` generates TypeScript resource types from your
`[JsonApiResource]`-attributed C# models. It reuses JsonApiToolkit's own
attribute/relationship classification, so the generated types cannot drift
from what your API serializes on the wire.

## Installation

```bash
dotnet tool install Intility.JsonApiToolkit.TypeGen --prerelease
```

> [!NOTE]
> The tool is pre-release. Drop `--prerelease` once a stable version ships.

## Mark your resources

```csharp
[JsonApiResource("articles")]
public class Article
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string? Body { get; set; }
    public Author? Author { get; set; }
    public List<Comment> Comments { get; set; } = [];
}
```

`[JsonApiResource("articles")]` sets the wire `type` string. It also fixes
the `fields[]`/included-type naming asymmetry: set
`JsonApiOptions.UseResourceAttributeTypeNames` so included resources carry
the same type name as their descriptor (see [Querying](querying.md)).

## Generate

Build your API project first, then point the tool at the compiled assembly:

```bash
jsonapi-typegen --assembly bin/Release/net10.0/MyApi.dll --out api-types.gen.ts
```

Use `--check` to fail CI on drift instead of writing:

```bash
jsonapi-typegen --assembly bin/Release/net10.0/MyApi.dll --out api-types.gen.ts --check
```

`--client-import <specifier>` points the generated `import type` at a
different module than the default `@intility/json-api-client` (for example
a relative path inside a monorepo).

## Output

One interface and one descriptor constant per resource:

```ts
export interface Article {
  id: string;
  title: string;
  body: string | null;
  author: Author | null;
  comments: Comment[];
}

export const Article: JsonApiResourceDescriptor<Article> = {
  type: "articles",
  attributes: ["title", "body"],
  toOne: ["author"],
  toMany: ["comments"],
};
```

Nullability follows the C# nullable annotations honestly on attributes.
Relationships are always `T | null` or `T[]`, regardless of the C#
annotation, matching what hydration fills in when the wire omits a
relationship.

Pass the descriptor to `@intility/json-api-client`:

```ts
import { createJsonApiClient } from "@intility/json-api-client";
import { Article } from "./api-types.gen.ts";

const client = createJsonApiClient({ baseUrl: "/api" });
const articles = client.resource(Article);

articles.list((q) => q.include("author").fields(Article, ["title"]));
```

`client.resource(Article)` reads the wire type and relationship names off
the descriptor, so the path and hydration can't drift from the interface.
There are no separate per-field name constants: the descriptor's
`attributes` array is what `fields()` types against.

## Limits

- A relationship to a type without `[JsonApiResource]` is dropped, with a
  stderr warning, rather than guessed at.
- A property type the toolkit doesn't otherwise map (outside
  string/bool/numeric/`DateTime`/`Guid`/enum/primitive arrays) is skipped
  with a warning instead of guessed at.

## CI

Regenerate as part of build, and gate on drift:

```bash
dotnet jsonapi-typegen --assembly bin/Release/net10.0/MyApi.dll --out api-types.gen.ts --check
```

A nonzero exit means the checked-in file no longer matches the assembly;
regenerate and commit.

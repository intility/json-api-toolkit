# TypeScript type generation

`jsonapi-typegen` generates TypeScript resource types from your
`[JsonApiResource]`-attributed C# models. It reuses JsonApiToolkit's own
attribute and relationship classification, so the generated types match what
your API sends.

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

`[JsonApiResource("articles")]` sets the wire `type`. Set
`JsonApiOptions.UseResourceAttributeTypeNames` so included resources carry
the same name as their descriptor (see [Querying](querying.md#sparse-fieldsets)).

## Generate

Build your API project first, then point the tool at the compiled assembly:

```bash
jsonapi-typegen --assembly bin/Release/net10.0/MyApi.dll --out api-types.gen.ts
```

| Flag | Effect |
|------|--------|
| `--check` | Do not write. Exit non-zero when the file is stale. Use it as a CI gate. |
| `--client-import <specifier>` | Import the descriptor type from another module than `@intility/json-api-client`, for example a relative path in a monorepo. |

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

> [!TIP]
> Exclude the generated file from your lint and format rules.

Attribute nullability follows the C# annotations. Relationships are always
`T | null` or `T[]`, because hydration fills those in when the wire omits a
relationship.

Pass the descriptor to `client.resource()` in
[`@intility/json-api-client`](typescript-client.md). The client reads the
path, wire type, and relationship names from it. The `attributes` array is
what `fields()` types against.

## Limits

- A relationship to a type without `[JsonApiResource]` is dropped with a
  warning.
- A property type the toolkit does not map (outside string, bool, numeric,
  `DateTime`, `Guid`, enum, and primitive arrays) is skipped with a warning.
- An enum is emitted as a string literal union, which assumes
  `JsonStringEnumConverter`. The tool can only see the converter when it is
  applied with `[JsonConverter]` on the enum or the property. A converter
  registered globally in `Program.cs` is not visible, so the tool warns.
  Add the attribute to silence the warning, or verify against a real
  response.

# JsonApiToolkit TypeGen

A .NET tool that generates TypeScript resource types from
`[JsonApiResource]`-attributed C# models. It reuses the exact
attribute/relationship classification that
[JsonApiToolkit](https://www.nuget.org/packages/Intility.JsonApiToolkit)
uses at runtime, so the generated types cannot drift from what your API
serializes on the wire.

## Installation

```bash
dotnet tool install Intility.JsonApiToolkit.TypeGen --prerelease
```

## Usage

Build your API project first, then point the tool at the compiled assembly:

```bash
jsonapi-typegen --assembly bin/Release/net10.0/MyApi.dll --out api-types.gen.ts
```

Pass `--check` to verify the output is up to date without writing it. This
exits non-zero on drift, which makes it useful as a CI gate:

```bash
jsonapi-typegen --assembly bin/Release/net10.0/MyApi.dll --out api-types.gen.ts --check
```

## How it works

Mark your resource models with `[JsonApiResource]` in the API project. The
tool loads the assembly, finds the attributed types, and emits, per
resource, one TypeScript interface and one descriptor constant of the same
name:

```ts
export interface Article {
  id: string;
  title: string;
  publishedAt: string | null;
  author: Author | null;
  comments: Comment[];
}

export const Article: JsonApiResourceDescriptor<Article> = {
  type: "articles",
  attributes: ["title", "publishedAt"],
  toOne: ["author"],
  toMany: ["comments"],
};
```

Attributes and relationships are classified the same way JsonApiToolkit
maps them. The descriptor is what
[`@intility/json-api-client`](https://jsr.io/@intility/json-api-client)
needs to keep hydration honest: `client.resource(Article)` infers the type,
uses the wire type as the path, and fills in what the wire omits
(null-stripped attributes as `null`, un-included relationships as `null` or
`[]`). Set `UseResourceAttributeTypeNames` in the API's `JsonApiOptions` so
included resources carry the same type names and match their descriptors.

The descriptor type is imported from `@intility/json-api-client` by default.
Pass `--client-import <specifier>` to point at a different module (for
example a relative path inside a monorepo).

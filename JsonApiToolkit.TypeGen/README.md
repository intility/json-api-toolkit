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
tool loads the assembly, finds the attributed types, and emits one
TypeScript interface per resource, with attributes and relationships
classified the same way JsonApiToolkit maps them.

The generated types pair with
[`@intility/json-api-client`](https://jsr.io/@intility/json-api-client)
for type-safe queries and hydration on the frontend.

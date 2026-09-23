# JsonApiToolkit TypeGen

A .NET tool that generates TypeScript resource types and descriptors from
`[JsonApiResource]`-attributed C# models. It reuses the classification
[JsonApiToolkit](https://www.nuget.org/packages/Intility.JsonApiToolkit)
uses at runtime, so the generated types match what your API sends.

## Installation

```bash
dotnet tool install Intility.JsonApiToolkit.TypeGen --prerelease
```

## Usage

Build your API project, then point the tool at the compiled assembly:

```bash
jsonapi-typegen --assembly bin/Release/net10.0/MyApi.dll --out api-types.gen.ts
```

Add `--check` in CI. It exits non-zero when the checked-in file is stale.

The output is consumed by
[`@intility/json-api-client`](https://jsr.io/@intility/json-api-client).

## Documentation

- [TypeScript type generation](https://intility.github.io/json-api-toolkit/typegen/)
- [End to end: API, types, client](https://intility.github.io/json-api-toolkit/end-to-end/)

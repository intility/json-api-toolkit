# @intility/json-api-client

[![TypeScript Release](https://github.com/intility/json-api-toolkit/actions/workflows/typescript-release.yml/badge.svg)](https://github.com/intility/json-api-toolkit/actions/workflows/typescript-release.yml)

Typed TypeScript client for [JsonApiToolkit](https://github.com/intility/json-api-toolkit)
backends: fetch client with resource handles, a type-safe query builder,
hydration of compound documents, JSON:API error types, and an optional
TanStack Query adapter.

## Installation

```bash
deno add jsr:@intility/json-api-client
# or: npx jsr add @intility/json-api-client
```

## Usage

Generate `api-types.gen.ts` from your C# models with
[`jsonapi-typegen`](https://intility.github.io/json-api-toolkit/typegen/),
then:

```ts
import { createJsonApiClient } from "@intility/json-api-client";
import { Article } from "./api-types.gen.ts";

const client = createJsonApiClient({ baseUrl: "/api" });
const articles = client.resource(Article);

const { data, pagination } = await articles.list((q) =>
  q.filter("published", true).include("author").page(1, 20)
);
const article = await articles.get(1);
```

## Documentation

- [TypeScript client guide](https://intility.github.io/json-api-toolkit/typescript-client/)
- [End to end: API, types, client](https://intility.github.io/json-api-toolkit/end-to-end/)

# json-api-client

[![TypeScript Release](https://github.com/intility/json-api-toolkit/actions/workflows/typescript-release.yml/badge.svg)](https://github.com/intility/json-api-toolkit/actions/workflows/typescript-release.yml)

**json-api-client** is a lightweight, Deno-based TypeScript library designed to
make working with
[JsonApiToolkit](https://github.com/intility/json-api-toolkit) responses
in TypeScript applications easier.

## Features

- **Type-safe**: Uses your own TypeScript types for attributes and
  relationships.
- **Hydration**: Hydrate your API responses into TypeScript objects.
- **Query Builder**: Generate query strings for filtering, sorting, pagination,
  and inclusion.

## Prerequisites

- **JsonApiToolkit**: This library is designed to work with
  [JsonApiToolkit](https://github.com/intility/json-api-toolkit). Make
  sure the api you want to interact with is using JsonApiToolkit.

## Getting Started

You can read more about json-api-client & JsonApiToolkit
[**here**](https://intility.github.io/json-api-toolkit/),
or follow the instructions below for a quick start.

### Installation

The package is published on [JSR](https://jsr.io/@intility/json-api-client):

```bash
# Deno
deno add jsr:@intility/json-api-client

# Node.js (via the jsr CLI)
npx jsr add @intility/json-api-client
```

### Define your types

```ts
export interface Todo {
  id: string;
  type: "todo";
  title: string;
  completed: boolean;
  dueDate: string;
  owner: User;
  tags: Tag[];
}

export interface User {
  id: string;
  type: "user";
  name: string;
  email: string;
}

export interface Tag {
  id: string;
  type: "tag";
  label: string;
}
```

### Create hooks for hydrated JSON:API responses

This library has zero dependencies, therefore no hook is provided. You can use
any library to fetch data, but here is an example using `react-query`:

```ts
import { QueryKey, useQuery, UseQueryOptions } from '@tanstack/react-query';
import {
  HydratedArrayResult,
  HydratedSingleResult,
  hydrateResponse,
  JsonApiArrayResponse,
  JsonApiSingleResponse,
} from '@intility/json-api-client';

// For list endpoints (returns array of resources)
export function useHydratedListQuery<THydrated>(
  queryKey: QueryKey,
  options?: Omit<
    UseQueryOptions<JsonApiArrayResponse, Error, HydratedArrayResult<THydrated>>,
    'queryKey' | 'select'
  >,
) {
  return useQuery<JsonApiArrayResponse, Error, HydratedArrayResult<THydrated>>({
    queryKey,
    select: (data) => hydrateResponse<THydrated>(data),
    ...options,
  });
}

// For single resource endpoints (returns single resource)
export function useHydratedSingleQuery<THydrated>(
  queryKey: QueryKey,
  options?: Omit<
    UseQueryOptions<JsonApiSingleResponse, Error, HydratedSingleResult<THydrated>>,
    'queryKey' | 'select'
  >,
) {
  return useQuery<JsonApiSingleResponse, Error, HydratedSingleResult<THydrated>>({
    queryKey,
    select: (data) => hydrateResponse<THydrated>(data),
    ...options,
  });
}
```

Then in your component, you can use the hooks to fetch and automatically hydrate
your data. Make sure to pass the correct type for the hydrated data.

```ts
// For a list of todos
const { data, isLoading } = useHydratedListQuery<Todo>(
  [queryKey, "todos", queryString],
);

// For a single todo
const { data, isLoading } = useHydratedSingleQuery<Todo>(
  [queryKey, "todos", id],
);
```

> [!IMPORTANT]
> This example uses a default `queryFn` which is pointed at with the `queryKey`.
> You can use any `queryFn` you want.

Now you can use the `data` object in your component.

```tsx
{
  isLoading ? <div>Loading…</div> : (
    <ul>
      {data.data.map((todo) => (
        <li key={todo.id}>
          <h2>{todo.title}</h2>
          <p>Due date: {todo.dueDate}</p>
          <p>Owner: {todo.owner.name}</p>
          <p>Tags: {todo.tags.map((tag) => tag.label).join(", ")}</p>
        </li>
      ))}
    </ul>
  );
}
```

### Query builder

In addition to hydrating your data, you can use the query builder to create
query strings for filtering, sorting, pagination, and inclusion. Append

```ts
const queryString = new JsonApiQueryBuilder<Todo>()
  .filter("completed", true)
  .sort("dueDate")
  .include("owner", "tags")
  .page(1, 10)
  .build();
```

This will create a query string that looks like this:

```
?filter[completed]=true&sort=dueDate&page[number]=1&page[size]=10&include=owner,tags
```

> [!TIP]
> The query builder supports nested includes using dot notation.

A `Date` filter value serializes to ISO 8601 (`date.toISOString()`), not
`String(date)`'s locale format. `in`/`nin` values join with commas:

```ts
new JsonApiQueryBuilder<Todo>().filter("dueDate", "in", [
  new Date("2025-01-01"),
  new Date("2025-02-01"),
]);
// filter[dueDate][in]=2025-01-01T00:00:00.000Z,2025-02-01T00:00:00.000Z
```

> [!WARNING]
> There is no escaping for a comma inside a value; a string value
> containing a literal comma breaks the join for `in`/`nin`.

`.sort()` only accepts direct attributes, unlike `.filter()`: the backend
silently ignores a dot-path sort field instead of walking the relationship,
so a type-safe "this compiles but does nothing" trap isn't worth having.

Repeat `.sort()` or `.include()` calls append, same as `.filter()`, rather
than replacing what an earlier call set:

```ts
new JsonApiQueryBuilder<Todo>().sort("title").sort("-dueDate");
// same as .sort("title", "-dueDate") -> sort=title,-dueDate
```

`.filter("owner.name", ...)` is type-checked one relationship level deep.
Deeper paths (the backend walks dot-paths up to 5 segments, including through
a to-many relationship via `Any()`, e.g. `"comments.author.name"`) still
compile, but only the first segment is checked against a real relationship;
everything past the first dot is on the honor system, same as `in`/`nin`
comma-joining not escaping commas.

Null checks use dedicated methods, since the `isnull`/`isnotnull` operators
ignore whatever value you'd otherwise pass:

```ts
const queryString = new JsonApiQueryBuilder<Todo>()
  .filterNull("dueDate")
  .build();
```

```
?filter[dueDate][isnull]=true
```

`filterNotNull("dueDate")` works the same way with `isnotnull`.

### Sparse fieldsets

Request only the fields you need to reduce payload size:

```ts
const queryString = new JsonApiQueryBuilder<Todo>()
  .filter("completed", false)
  .fields("todos", ["title", "dueDate"])
  .fields("users", ["name"])
  .include("owner")
  .build();
```

```
?filter[completed]=false&include=owner&fields[todos]=title,dueDate&fields[users]=name
```

`fields()` also takes a generated resource descriptor (see
`dotnet jsonapi-typegen`) instead of a hand-written type string, so the wire
`type` can't drift from what the descriptor says:

```ts
const queryString = new JsonApiQueryBuilder<Todo>()
  .fields(TodoDescriptor, ["title", "dueDate"])
  .build();
```

### Filtering included resources

`filterIncluded()` trims which resources come back in `included`, without
touching the primary `data` array. Distinct from dot-path filtering
(`.filter("owner.name", ...)`), which filters the primary resource itself by
a related field. Requires the relationship to also be passed to `.include()`.

```ts
const queryString = new JsonApiQueryBuilder<Todo>()
  .filterIncluded("tags", "label", "eq", "urgent")
  .include("tags")
  .build();
```

```
?filter[tags][label][eq]=urgent&include=tags
```

> [!NOTE]
> The backend's filtered-include only applies to to-many relationships (EF
> Core's collection `.Where()`); on a to-one relationship, `filterIncluded()`
> compiles but has no effect.

### Filter groups

Use logical grouping for complex filter expressions:

```ts
const queryString = new JsonApiQueryBuilder<Todo>()
  .or((b) => {
    b.filter("title", "like", "%urgent%");
    b.filter("completed", "eq", "false");
  })
  .build();
```

```
?filter[or][0][title][like]=%urgent%&filter[or][1][completed]=false
```

There is no `.and()`: top-level filters are already AND'd together. Groups
take flat filter lists only; nesting a group inside `or()`/`not()` is a
compile error, because the backend only parses one flat level.

### Error handling

The library provides TypeScript types for JSON:API error responses, matching
the error structure produced by
[JsonApiToolkit](https://github.com/intility/Intility.JsonApiToolkit) v1.3.0+:

```ts
import {
  isJsonApiErrorResponse,
  JsonApiErrorCodes,
} from "@intility/json-api-client";

const response = await fetch("/api/todos");
const body = await response.json();

if (isJsonApiErrorResponse(body)) {
  for (const error of body.errors) {
    if (error.code === JsonApiErrorCodes.RESOURCE_NOT_FOUND) {
      // handle not found
    }
    console.error(`${error.title}: ${error.detail}`);
  }
}
```

The `isJsonApiErrorResponse()` type guard narrows the type so you get full
autocomplete on the `errors` array and individual error fields (`status`,
`code`, `title`, `detail`, `source`, `meta`).

---

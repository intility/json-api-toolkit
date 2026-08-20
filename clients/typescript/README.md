# jsonapi-ts-tools

[![🚀 Release](https://github.com/intility/jsonapi-ts-tools/actions/workflows/release.yml/badge.svg)](https://github.com/intility/jsonapi-ts-tools/actions/workflows/release.yml)

**jsonapi-ts-tools** is a lightweight, Deno-based TypeScript library designed to
make working with
[JsonApiToolkit](https://github.com/intility/Intility.JsonApiToolkit) responses
in TypeScript applications easier.

## Features

- **Type-safe**: Uses your own TypeScript types for attributes and
  relationships.
- **Hydration**: Hydrate your API responses into TypeScript objects.
- **Query Builder**: Generate query strings for filtering, sorting, pagination,
  and inclusion.

## Prerequisites

- **JsonApiToolkit**: This library is designed to work with
  [JsonApiToolkit](https://github.com/intility/Intility.JsonApiToolkit). Make
  sure the api you want to interact with is using JsonApiToolkit.

## Getting Started

You can read more about jsonapi-ts-tools & JsonApiToolkit
[**here**](https://intility.github.io/Intility.JsonApiToolkit/docs/integrations/ts-tools.html),
or follow the instructions below for a quick start.

### Installation

```bash
npm install @intility/jsonapi-ts-tools
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
} from '@intility/jsonapi-ts-tools';

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

You can also nest groups:

```ts
const queryString = new JsonApiQueryBuilder<Todo>()
  .or((b) => {
    b.filter("title", "eq", "Important");
    b.and((inner) => {
      inner.filter("completed", "eq", "true");
      inner.filter("dueDate", "gt", "2025-01-01");
    });
  })
  .build();
```

### Error handling

The library provides TypeScript types for JSON:API error responses, matching
the error structure produced by
[JsonApiToolkit](https://github.com/intility/Intility.JsonApiToolkit) v1.3.0+:

```ts
import {
  isJsonApiErrorResponse,
  JsonApiErrorCodes,
} from "@intility/jsonapi-ts-tools";

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

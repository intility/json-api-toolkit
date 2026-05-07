# Integrations / Frontend Consumption

# jsonapi-ts-tools

**jsonapi-ts-tools** is a lightweight, Deno-based TypeScript library designed
to make working with
[JsonApiToolkit](https://github.com/intility/Intility.JsonApiToolkit) responses
in TypeScript applications easier.

## Features

- **Type-safe**: Uses your own TypeScript types for attributes and relationships.
- **Hydration**: Hydrate your API responses into TypeScript objects.
- **Query Builder**: Generate query strings for filtering, sorting, pagination,
  and inclusion.

## Prerequisites
- **JsonApiToolkit**: This library is designed to work with [JsonApiToolkit](https://github.com/intility/Intility.JsonApiToolkit). Make sure the api you want to interact with is using JsonApiToolkit.

## Getting Started

You can read more about jsonapi-ts-tools & JsonApiToolkit [**here**](https://intility.github.io/Intility.JsonApiToolkit/docs/integrations/ts-tools.html), or follow the instructions below for a quick start.

### Installation

```bash
npm install @intility/jsonapi-ts-tools
```

### Define your types

```ts
export interface Todo {
  id: string;
  type: 'todo';
  title: string;
  completed: boolean;
  dueDate: string;
  owner: User;
  tags: Tag[];
}

export interface User {
  id: string;
  type: 'user';
  name: string;
  email: string;
}

export interface Tag {
  id: string;
  type: 'tag';
  label: string;
}
```

### Create a hook for hydrated JSON:API responses
This library has zero dependencies, therefore no hook is provided. You can use any library to fetch data, but here is an example using `react-query`:

```ts
import { QueryKey, useQuery, UseQueryOptions } from '@tanstack/react-query';
import {
  HydratedQueryResult,
  hydrateResponse,
  JsonApiResponse,
} from '@intility/jsonapi-ts-tools';

export function useHydratedQuery<THydrated>(
  queryKey: QueryKey,
  options?: Omit<
    UseQueryOptions<JsonApiResponse, Error, HydratedQueryResult<THydrated>>,
    'queryKey' | 'select'
  >,
) {
  return useQuery<JsonApiResponse, Error, HydratedQueryResult<THydrated>>({
    queryKey,
    select: (data) => hydrateResponse<THydrated>(data),
    ...options,
  });
}
```

Then in your component, you can use the `useHydratedQuery` hook to fetch and automatically hydrate your data. Make sure to pass the correct type for the hydrated data.

```ts
const { data, isLoading } = useHydratedQuery<Todo>(
    [queryKey, 'todos', queryString],
);
```

> [!IMPORTANT]
> This example uses a default `queryFn` which is pointed at with the `queryKey`. You can use any `queryFn` you want.

Now you can use the `data` object in your component.

```tsx
{isLoading ? <div>Loading…</div> : (
  <ul>
    {data.map((todo) => (
      <li key={todo.id}>
        <h2>{todo.title}</h2>
        <p>Due date: {todo.dueDate}</p>
        <p>Owner: {todo.owner.name}</p>
        <p>Tags: {todo.tags.map((tag) => tag.label).join(', ')}</p>
      </li>
    ))}
  </ul>
)}
```

### Query builder
In addition to hydrating your data, you can use the query builder to create query strings for filtering, sorting, pagination, and inclusion. Append 

```ts
const queryString = new JsonApiQueryBuilder<Todo>()
  .filter('completed', true)
  .sort('dueDate')
  .include('owner', 'tags')
  .page(1, 10)
  .build();
```

This will create a query string that looks like this:

```
?filter[completed]=true&sort=dueDate&page[number]=1&page[size]=10&include=owner,tags
```

> [!TIP]
> The query builder supports nested includes using dot notation.


### Kitchen sink example

**What this query does**

- Filters for incomplete todos (`completed = false`)
- Filters for todos due between today and 7 days from now **OR** tagged as "urgent"
- Filters for todos owned by Alice **or** Bob, **and** where `dueDate` is not null
- Sorts by `dueDate` ascending, then by `title` descending
- Includes related `owner`, `tags`, and the owner's `email`
- Requests page 2, 20 items per page

---

**Query builder code**

```ts
const queryString = new JsonApiQueryBuilder<Todo>()
  .filter('completed', 'eq', false)
  .or(or =>
    or
      .filter('dueDate', 'ge', '2025-05-21')
      .filter('dueDate', 'le', '2025-05-28')
      .or(or2 =>
        or2.filter('tags', 'in', ['urgent'])
      )
  )
  .and(and =>
    and
      .or(or =>
        or
          .filter('owner', 'eq', 'alice-id')
          .filter('owner', 'eq', 'bob-id')
      )
      .not(not =>
        not.filter('dueDate', 'isnull', true)
      )
  )
  .sort('dueDate', '-title')
  .include('owner', 'tags', 'owner.email')
  .paginate(2, 20)
  .build();
```

---

**Output query string**

```
filter[completed]=false
&filter[or][0][dueDate][ge]=2025-05-21
&filter[or][1][dueDate][le]=2025-05-28
&filter[or][2][or][0][tags][in]=urgent
&filter[and][0][or][0][owner][eq]=alice-id
&filter[and][0][or][1][owner][eq]=bob-id
&filter[and][1][not][0][dueDate][isnull]=true
&sort=dueDate,-title
&include=owner,tags,owner.email
&page[number]=2
&page[size]=20
```

### Further Information

For more details on the package itself, visit the repository:

*   **GitHub:** [jsonapi-ts-tools](https://github.com/intility/jsonapi-ts-tools)
  

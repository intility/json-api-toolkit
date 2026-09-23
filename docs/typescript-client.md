# TypeScript client

`@intility/json-api-client` is a typed client for JsonApiToolkit backends. It
has four parts: a fetch client with resource handles, a query builder,
hydration of compound documents, and JSON:API error types. An optional
`tanstack-query` subpath adapts resource handles to TanStack Query.

## Installation

```bash
deno add jsr:@intility/json-api-client
# or: npx jsr add @intility/json-api-client
```

## Client and resource handles

```ts
import { createJsonApiClient } from "@intility/json-api-client";
import { Article } from "./api-types.gen.ts";

const client = createJsonApiClient({ baseUrl: "/api", fetch: authorizedFetch });
const articles = client.resource(Article);

const { data, pagination } = await articles.list((q) => q.page(1, 20));
const article = await articles.get(1, (q) => q.include("author"));
const created = await articles.post({ title: "Hello" });
await articles.patch(created.id, { title: "Renamed" });
await articles.delete(created.id);
```

`fetch` is optional. Pass a wrapper to attach auth headers. The client owns
the content type, status handling, and parsing.

Pass a generated descriptor (see [TypeScript type generation](typegen.md))
and the handle infers `T`, uses the wire type as the path, and fills in what
the wire omits: stripped attributes become `null`, un-included relationships
become `null` or `[]`. Pass a plain path instead, `client.resource<Article>("articles")`,
and those fields stay `undefined`.

`post` and `patch` bodies are plain camelCase DTOs. The backend has no
JSON:API request deserializer.

`get`, `post`, and `patch` throw a `TypeError` when the endpoint returns a
collection document. That is a backend action shape mismatch: use
`JsonApiOkAsync`, not `JsonApiQueryAsync`, in single-resource actions.

## Query builder

Every read takes a callback that receives a `JsonApiQueryBuilder<T>`. You can
also use the builder on its own:

```ts
const qs = new JsonApiQueryBuilder<Article>()
  .filter("published", true)
  .filter("author.name", "like", "Berg")
  .sort("-publishedAt")
  .include("author", "comments.author")
  .fields(Article, ["title", "publishedAt"])
  .page(1, 10)
  .build();
```

```
?filter[published]=true&filter[author.name][like]=Berg&sort=-publishedAt&include=author,comments.author&fields[articles]=title,publishedAt&page[number]=1&page[size]=10
```

Rules to know:

- `filter(field, value)` is equality. `filter(field, op, value)` takes
  `eq`, `ne`, `gt`, `ge`, `lt`, `le`, `like`, `in`, `nin`.
- Dot paths in `filter` are type-checked one relationship deep. Deeper paths
  compile but are not checked past the first segment.
- `sort` only accepts direct attributes. The backend ignores dot-path sorts.
- Repeated `filter`, `sort`, and `include` calls append.
- A `Date` value serializes as ISO 8601. `in` and `nin` values join with
  commas and are not escaped.
- `fields` accepts a descriptor or a type string.

### Null checks

```ts
q.filterNull("publishedAt");      // filter[publishedAt][isnull]=true
q.filterNotNull("publishedAt");   // filter[publishedAt][isnotnull]=true
```

### Filter groups

Top-level filters are combined with AND. Use `or` and `not` for groups.
Inside a group, `filter` always takes the operator. Groups are flat: a
group inside a group is a compile error, because the backend parses one
level only.

```ts
q.or((b) => {
  b.filter("title", "like", "urgent");
  b.filter("published", "eq", false);
});
// filter[or][0][title][like]=urgent&filter[or][1][published]=false
```

### Filtering included resources

`filterIncluded` trims the `included` array without touching the primary
`data`. The relationship must also be in `include`. The backend applies this
to to-many relationships only.

```ts
q.filterIncluded("comments", "text", "like", "spam").include("comments");
// filter[comments][text][like]=spam&include=comments
```

Compare with `filter("comments.text", "like", "spam")`, which filters the
articles themselves by a related field.

## Error handling

A non-2xx response throws `JsonApiRequestError`. It carries `status` and the
parsed `errors` array. `message` is the first error's `detail`, then `title`.

```ts
import { JsonApiErrorCodes, JsonApiRequestError } from "@intility/json-api-client";

try {
  await articles.get(999);
} catch (e) {
  if (e instanceof JsonApiRequestError && e.hasCode(JsonApiErrorCodes.RESOURCE_NOT_FOUND)) {
    // handle not found
  }
}
```

`fieldErrors()` groups errors by the attribute named in `source.pointer`,
which is useful for form validation. For raw responses, the
`isJsonApiErrorResponse()` type guard narrows a parsed body to the error
document shape.

## TanStack Query adapter

The `tanstack-query` subpath wraps a resource handle in option objects for
`useQuery`, `useInfiniteQuery`, and `useMutation`. The core package has no
dependency on TanStack.

```ts
import { createJsonApiClient } from "@intility/json-api-client";
import { createJsonApiErrorHandler, jsonApiResource } from "@intility/json-api-client/tanstack-query";
import { MutationCache, QueryClient, useInfiniteQuery, useMutation, useQuery } from "@tanstack/react-query";

const queryClient = new QueryClient({
  mutationCache: new MutationCache({
    onError: createJsonApiErrorHandler({ show: (message) => toast(message) }),
  }),
});

const client = createJsonApiClient({ baseUrl: "/api" });
const articles = jsonApiResource(client.resource(Article), queryClient);

const list = useQuery(articles.list((q) => q.filter("published", true)));
const one = useQuery(articles.detail(id, (q) => q.include("author")));
const pages = useInfiniteQuery(articles.infiniteList(20, (q) => q.sort("title")));

const create = useMutation(articles.post<{ title: string }>());
const update = useMutation(articles.patch<{ title: string }>());
const remove = useMutation(articles.delete());
```

The option objects also work in route loaders through `queryClient.prefetchQuery`.

`post` invalidates lists. `patch` and `delete` invalidate lists and details.
The invalidation lives in `onSuccess`. If you add your own `onSuccess`, call
the base one:

```ts
const base = articles.post<{ title: string }>();
const create = useMutation({
  ...base,
  onSuccess: async () => {
    await base.onSuccess();
    toast.success("Created");
  },
});
```

`createJsonApiErrorHandler` shows one message per failed mutation. Mutations
that set `onError` in their `useMutation` options are skipped. Callbacks
passed to `mutate(vars, { onError })` are not skipped.

### Bifrost floating messages

Bifrost exposes `showFloatingMessage` through a hook, and hooks cannot run in
a `MutationCache` handler. Bridge them with a component under the
`<FloatingMessage>` provider:

```tsx
// floating-message-bridge.tsx
import type { ReactNode } from "react";
import useFloatingMessage from "@intility/bifrost-react/hooks/useFloatingMessage";

let show: ((message: ReactNode) => void) | undefined;

export function showFloatingError(message: ReactNode): void {
  show?.(message);
}

export function FloatingMessageBridge() {
  const { showFloatingMessage } = useFloatingMessage();
  show = (message) => showFloatingMessage(message, { state: "alert" });
  return null;
}
```

Render `<FloatingMessageBridge />` inside `<FloatingMessage>`, and pass
`showFloatingError` as `show` to `createJsonApiErrorHandler`.

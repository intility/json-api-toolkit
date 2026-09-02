/**
 * TypeScript tools for JSON:API responses from
 * [JsonApiToolkit](https://github.com/intility/json-api-toolkit) backends.
 *
 * The library has four parts:
 *
 * - **Client**: `createJsonApiClient` builds a fetch-based client. Its
 *   `resource<T>(path)` handles cover query building, hydration, and error
 *   handling for reads and writes in one call path.
 * - **Hydration**: `hydrateResponse` resolves relationships from the
 *   `included` array and returns plain objects as `{ data, meta, links }`.
 *   Used internally by the client; exported for interop.
 * - **Query builder**: `JsonApiQueryBuilder` builds type-safe JSON:API
 *   query strings with filters, sorts, includes, sparse fieldsets, and
 *   pagination.
 * - **Error types**: JSON:API error types, the `isJsonApiErrorResponse`
 *   type guard, and `JsonApiRequestError` (thrown by the client on
 *   non-2xx responses), matching the C# toolkit's error model.
 *
 * ## Usage
 *
 * ```ts
 * import { createJsonApiClient } from '@intility/json-api-client';
 *
 * const client = createJsonApiClient({ baseUrl: '/api', fetch });
 * const books = client.resource<Book>('books');
 *
 * const { data, pagination } = await books.list((q) =>
 *   q.filter('author.name', 'John').include('author').page(1, 25));
 *
 * const book = await books.get(id, (q) => q.include('author'));
 * const created = await books.create({ title: '...' });
 * await books.update(created.id, { title: 'renamed' });
 * await books.remove(created.id);
 * ```
 *
 * @module
 */

export * from './client.ts';
export * from './hydrate.ts';
export * from './errors.ts';
export * from './types/jsonapi.ts';
export * from './types/query-builder.ts';
export * from './types/filters.ts';
export * from './types/errors.ts';
export * from './query-builder/JsonApiQueryBuilder.ts';
export * from './query-builder/FilterGroupBuilder.ts';

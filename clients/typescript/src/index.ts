/**
 * TypeScript tools for JSON:API responses from
 * [JsonApiToolkit](https://github.com/intility/json-api-toolkit) backends.
 *
 * The library has three parts:
 *
 * - **Hydration**: `hydrateResponse` resolves relationships from the
 *   `included` array and returns plain objects as `{ data, meta, links }`.
 * - **Query builder**: `JsonApiQueryBuilder` builds type-safe JSON:API
 *   query strings with filters, sorts, includes, sparse fieldsets, and
 *   pagination.
 * - **Error types**: JSON:API error types and the
 *   `isJsonApiErrorResponse` type guard, matching the C# toolkit's error
 *   model.
 *
 * ## Usage
 *
 * ```ts
 * import {
 *   hydrateResponse,
 *   JsonApiQueryBuilder,
 * } from '@intility/json-api-client';
 *
 * const query = new JsonApiQueryBuilder<Book>()
 *   .filter('author.name', 'John')
 *   .include('author')
 *   .sort('-publishedAt')
 *   .page(1, 25)
 *   .build();
 *
 * const response = await fetch(`/api/books?${query}`);
 * const { data, meta } = hydrateResponse<Book>(await response.json());
 * ```
 *
 * @module
 */

export * from './hydrate.ts';
export * from './errors.ts';
export * from './types/jsonapi.ts';
export * from './types/query-builder.ts';
export * from './types/filters.ts';
export * from './types/errors.ts';
export * from './query-builder/JsonApiQueryBuilder.ts';
export * from './query-builder/FilterGroupBuilder.ts';

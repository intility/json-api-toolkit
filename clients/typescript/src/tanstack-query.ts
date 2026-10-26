/**
 * TanStack Query adapter for `@intility/json-api-client`.
 *
 * Wraps a resource handle in plain option objects for `useQuery`,
 * `useInfiniteQuery`, and `useMutation`. It owns cache keys, option
 * assembly, and the next-page check; fetch, hydration, and errors stay in
 * the core client. No runtime dependency on TanStack: the `QueryClient`
 * type is structural, so any v5 client fits.
 *
 * ```ts
 * import { jsonApiResource } from '@intility/json-api-client/tanstack-query';
 *
 * const todos = jsonApiResource(client.resource<Todo>('todos'), queryClient);
 * useQuery(todos.list((q) => q.filter('completed', true)));
 * useMutation(todos.post());
 * ```
 *
 * @module
 */

import type { JsonApiResourceHandle, QueryFn } from './client.ts';
import type { HydratedList } from './types/jsonapi.ts';

/** The part of a TanStack `QueryClient` the adapter uses. */
export interface InvalidatingQueryClient {
  invalidateQueries(filters: { queryKey: readonly unknown[] }): Promise<void>;
}

export interface JsonApiQueryResource<T> {
  /** `["jsonapi", path]`; prefix of every key this resource produces. */
  readonly queryKey: readonly [string, string];
  list(query?: QueryFn<T>): {
    queryKey: readonly [string, string, 'list', string];
    queryFn: () => Promise<HydratedList<T>>;
  };
  detail(id: string | number, query?: QueryFn<T>): {
    queryKey: readonly [string, string, 'detail', string, string];
    queryFn: () => Promise<T>;
  };
  /**
   * Pages derive from `pagination` in each response, never from `links`.
   * The adapter sets `page()` per request; do not call it in `query`.
   */
  infiniteList(pageSize: number, query?: QueryFn<T>): {
    queryKey: readonly [string, string, 'list', string, number];
    queryFn: (ctx: { pageParam: number }) => Promise<HydratedList<T>>;
    initialPageParam: number;
    getNextPageParam: (last: HydratedList<T>) => number | undefined;
  };
  /** Invalidates lists on success. */
  post<TBody = unknown>(): {
    mutationFn: (body: TBody) => Promise<T>;
    onSuccess: () => Promise<void>;
  };
  /** Invalidates lists and details on success. */
  patch<TBody = unknown>(): {
    mutationFn: (vars: { id: string | number; body: TBody }) => Promise<T>;
    onSuccess: () => Promise<void>;
  };
  /** Invalidates lists and details on success. */
  delete(): {
    mutationFn: (id: string | number) => Promise<void>;
    onSuccess: () => Promise<void>;
  };
  /** Everything under this resource. */
  invalidate(): Promise<void>;
  invalidateLists(): Promise<void>;
}

export function jsonApiResource<T>(
  handle: JsonApiResourceHandle<T>,
  queryClient: InvalidatingQueryClient,
): JsonApiQueryResource<T> {
  const queryKey = ['jsonapi', handle.path] as const;
  const listKey = [...queryKey, 'list'] as const;
  const invalidate = () => queryClient.invalidateQueries({ queryKey });
  const invalidateLists = () =>
    queryClient.invalidateQueries({ queryKey: listKey });

  return {
    queryKey,
    list(query) {
      const params = handle.params(query);
      return {
        queryKey: [...listKey, params],
        queryFn: () => handle.list({ params }),
      };
    },
    detail(id, query) {
      const params = handle.params(query);
      return {
        queryKey: [...queryKey, 'detail', String(id), params],
        queryFn: () => handle.get(id, { params }),
      };
    },
    infiniteList(pageSize, query) {
      return {
        queryKey: [...listKey, handle.params(query), pageSize],
        queryFn: ({ pageParam }) =>
          handle.list((q) => {
            query?.(q);
            q.page(pageParam, pageSize);
          }),
        initialPageParam: 1,
        getNextPageParam: (last) => {
          const p = last.pagination;
          return p && p.currentPage < p.totalPages
            ? p.currentPage + 1
            : undefined;
        },
      };
    },
    post: () => ({
      mutationFn: (body) => handle.post(body),
      onSuccess: invalidateLists,
    }),
    patch: () => ({
      mutationFn: ({ id, body }) => handle.patch(id, body),
      onSuccess: invalidate,
    }),
    delete: () => ({
      mutationFn: (id) => handle.delete(id),
      onSuccess: invalidate,
    }),
    invalidate,
    invalidateLists,
  };
}

export interface JsonApiErrorHandlerOptions {
  /** Displays one message, e.g. a Bifrost floating message. */
  show: (message: string) => void;
  /** Turns the error into text. Defaults to `error.message`. */
  format?: (error: unknown) => string;
}

/**
 * Global mutation error handler for `MutationCache({ onError })`. Skips
 * mutations that define their own `onError`, so per-call handlers win.
 */
export function createJsonApiErrorHandler(
  options: JsonApiErrorHandlerOptions,
): (
  error: unknown,
  variables?: unknown,
  context?: unknown,
  mutation?: { options: { onError?: unknown } },
) => void {
  const format = options.format ??
    ((e: unknown) => e instanceof Error ? e.message : String(e));
  return (error, _variables, _context, mutation) => {
    if (mutation?.options.onError) return;
    options.show(format(error));
  };
}

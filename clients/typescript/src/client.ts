import type {
  HydratedList,
  JsonApiArrayResponse,
  JsonApiResourceDescriptor,
  JsonApiResourceDescriptorBase,
  JsonApiResourceDescriptors,
  JsonApiSingleResponse,
} from './types/jsonapi.ts';
import { hydrateResponse } from './hydrate.ts';
import { isJsonApiErrorResponse, JsonApiRequestError } from './errors.ts';
import { JsonApiQueryBuilder } from './query-builder/JsonApiQueryBuilder.ts';

const JSON_API_CONTENT_TYPE = 'application/vnd.api+json';

export interface JsonApiClientOptions {
  /** Origin + path prefix, no trailing slash required (e.g. "https://api.example.com"). */
  baseUrl: string;
  /**
   * Standard fetch signature. Attach auth (tokens, dynamic headers) in a
   * wrapper and pass it here; the client owns content-type headers, status
   * handling, and parsing. Defaults to the global `fetch`.
   */
  fetch?: typeof fetch;
  /**
   * Generated descriptors for resources that show up as `included` but
   * never get their own handle. Descriptors passed to `resource()` are
   * registered automatically.
   */
  resources?: readonly JsonApiResourceDescriptorBase[];
}

/** Pre-built query string, for interop with externally-built params (e.g. gjallarbru). */
export interface RawQueryParams {
  params: string;
}

export type QueryFn<T> = (builder: JsonApiQueryBuilder<T>) => unknown;

/**
 * Typed handle for one resource path. Bodies for `post`/`patch` are plain
 * camelCase DTOs (the backend has no JSON:API request deserializer);
 * responses are JSON:API documents, hydrated into plain objects.
 */
export interface JsonApiResourceHandle<T> {
  list(query?: QueryFn<T> | RawQueryParams): Promise<HydratedList<T>>;
  get(id: string | number, query?: QueryFn<T>): Promise<T>;
  post(body: unknown): Promise<T>;
  patch(id: string | number, body: unknown): Promise<T>;
  /** 204 No Content on success. */
  delete(id: string | number): Promise<void>;
}

export interface JsonApiClient {
  /**
   * Handle for a generated resource. `T` is inferred from the descriptor;
   * hydration fills what the wire omits (see {@link JsonApiResourceDescriptor}).
   * @param path Collection path relative to `baseUrl`; defaults to the wire type.
   */
  resource<T>(
    descriptor: JsonApiResourceDescriptor<T>,
    path?: string,
  ): JsonApiResourceHandle<T>;
  /**
   * Handle for a hand-typed resource. No descriptor, so absent attributes
   * and un-included relationships stay `undefined`.
   * @param path Collection path relative to `baseUrl`, e.g. "articles".
   */
  resource<T>(path: string): JsonApiResourceHandle<T>;
}

function queryString<T>(query?: QueryFn<T> | RawQueryParams): string {
  if (!query) return '';
  if (typeof query === 'function') {
    const builder = new JsonApiQueryBuilder<T>();
    query(builder);
    return builder.build();
  }
  return query.params;
}

async function readBody(res: Response): Promise<unknown> {
  if (res.status === 204) return null;
  const text = await res.text();
  if (!text) return null;
  try {
    return JSON.parse(text);
  } catch {
    return null;
  }
}

/**
 * Builds a JSON:API client. `client.resource<T>(path)` returns a typed
 * handle covering query building, fetch, hydration, and error handling for
 * reads and writes in one call path.
 *
 * Any non-2xx response throws {@link JsonApiRequestError}.
 */
export function createJsonApiClient(
  options: JsonApiClientOptions,
): JsonApiClient {
  const fetchImpl = options.fetch ?? fetch;
  const baseUrl = options.baseUrl.replace(/\/+$/, '');
  const descriptors: JsonApiResourceDescriptors = {};
  for (const d of options.resources ?? []) descriptors[d.type] = d;

  async function send(
    method: string,
    path: string,
    opts: { qs?: string; body?: unknown } = {},
  ): Promise<unknown> {
    const url = opts.qs
      ? `${baseUrl}/${path}?${opts.qs}`
      : `${baseUrl}/${path}`;
    const res = await fetchImpl(url, {
      method,
      headers: opts.body !== undefined
        ? { 'Content-Type': JSON_API_CONTENT_TYPE }
        : undefined,
      body: opts.body !== undefined ? JSON.stringify(opts.body) : undefined,
    });
    const doc = await readBody(res);
    if (!res.ok) {
      throw new JsonApiRequestError(
        res.status,
        isJsonApiErrorResponse(doc) ? doc.errors : [],
      );
    }
    return doc;
  }

  function single<T>(doc: unknown): T {
    return hydrateResponse<T>(doc as JsonApiSingleResponse, descriptors).data;
  }

  return {
    resource<T>(
      source: JsonApiResourceDescriptor<T> | string,
      path?: string,
    ): JsonApiResourceHandle<T> {
      if (typeof source !== 'string') descriptors[source.type] = source;
      const cleanPath =
        (path ?? (typeof source === 'string' ? source : source.type))
          .replace(/^\/+|\/+$/g, '');
      return {
        async list(query) {
          const doc = await send('GET', cleanPath, { qs: queryString(query) });
          return hydrateResponse<T>(doc as JsonApiArrayResponse, descriptors);
        },
        async get(id, query) {
          return single<T>(
            await send('GET', `${cleanPath}/${id}`, { qs: queryString(query) }),
          );
        },
        async post(body) {
          return single<T>(await send('POST', cleanPath, { body }));
        },
        async patch(id, body) {
          return single<T>(await send('PATCH', `${cleanPath}/${id}`, { body }));
        },
        async delete(id) {
          await send('DELETE', `${cleanPath}/${id}`);
        },
      };
    },
  };
}

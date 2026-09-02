import type {
  HydratedList,
  JsonApiArrayResponse,
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
}

/** Pre-built query string, for interop with externally-built params (e.g. gjallarbru). */
export interface RawQueryParams {
  params: string;
}

export type QueryFn<T> = (builder: JsonApiQueryBuilder<T>) => unknown;

/**
 * Typed handle for one resource path. Bodies for `create`/`update` are
 * plain camelCase DTOs (the backend has no JSON:API request deserializer);
 * responses are JSON:API documents, hydrated into plain objects.
 */
export interface JsonApiResourceHandle<T> {
  list(query?: QueryFn<T> | RawQueryParams): Promise<HydratedList<T>>;
  get(id: string | number, query?: QueryFn<T>): Promise<T>;
  create(body: unknown): Promise<T>;
  update(id: string | number, body: unknown): Promise<T>;
  /** 204 No Content on success. */
  remove(id: string | number): Promise<void>;
}

export interface JsonApiClient {
  /** @param path Collection path relative to `baseUrl`, e.g. "articles". */
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
    return hydrateResponse<T>(doc as JsonApiSingleResponse).data;
  }

  return {
    resource<T>(path: string): JsonApiResourceHandle<T> {
      const cleanPath = path.replace(/^\/+|\/+$/g, '');
      return {
        async list(query) {
          const doc = await send('GET', cleanPath, { qs: queryString(query) });
          return hydrateResponse<T>(doc as JsonApiArrayResponse);
        },
        async get(id, query) {
          return single<T>(
            await send('GET', `${cleanPath}/${id}`, { qs: queryString(query) }),
          );
        },
        async create(body) {
          return single<T>(await send('POST', cleanPath, { body }));
        },
        async update(id, body) {
          return single<T>(await send('PATCH', `${cleanPath}/${id}`, { body }));
        },
        async remove(id) {
          await send('DELETE', `${cleanPath}/${id}`);
        },
      };
    },
  };
}

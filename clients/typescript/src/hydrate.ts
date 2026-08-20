// utils/hydrate.ts

import type {
  HydratedArrayResult,
  HydratedSingleResult,
  JsonApiArrayResponse,
  JsonApiResource,
  JsonApiResponse,
  JsonApiSingleResponse,
} from './types/jsonapi.ts';

type ResourceMap = Record<string, Record<string, JsonApiResource>>;

/**
 * Builds a lookup map for included resources.
 */
function buildResourceMap(
  included: JsonApiResource[] = [],
): ResourceMap {
  const map: ResourceMap = {};
  for (const res of included) {
    (map[res.type] ??= {})[res.id] = res;
  }
  return map;
}

/**
 * Hydrates a single resource, resolving relationships.
 */
function hydrateOne<T = unknown>(
  resource: JsonApiResource,
  map: ResourceMap,
  deep: boolean,
  visited: Set<string>,
): T {
  const key = `${resource.type}:${resource.id}`;
  if (visited.has(key)) {
    // Prevent infinite recursion
    return { id: resource.id, type: resource.type, circular: true } as T;
  }
  visited.add(key);

  const out: Record<string, unknown> = {
    id: resource.id,
    type: resource.type,
    ...resource.attributes,
  };

  if (resource.relationships) {
    for (const [relName, rel] of Object.entries(resource.relationships)) {
      const relData = rel.data;
      if (Array.isArray(relData)) {
        out[relName] = relData
          .map((ref) => {
            const related = map[ref.type]?.[ref.id];
            if (!related) return null;
            return deep
              ? hydrateOne(related, map, true, new Set(visited))
              : { id: related.id, type: related.type, ...related.attributes };
          })
          .filter(Boolean);
      } else if (relData) {
        const related = map[relData.type]?.[relData.id];
        out[relName] = related
          ? (deep
            ? hydrateOne(related, map, true, new Set(visited))
            : { id: related.id, type: related.type, ...related.attributes })
          : null;
      } else {
        out[relName] = null;
      }
    }
  }
  return out as T;
}

/**
 * Hydrates a JSON:API response, returning { data, meta, links }.
 * Preserves single vs array structure from input.
 */
export function hydrateResponse<T = unknown>(
  response: JsonApiSingleResponse,
): HydratedSingleResult<T>;
export function hydrateResponse<T = unknown>(
  response: JsonApiArrayResponse,
): HydratedArrayResult<T>;
export function hydrateResponse<T = unknown>(
  response: JsonApiResponse | null | undefined,
): HydratedSingleResult<T> | HydratedArrayResult<T> | null;
export function hydrateResponse<T = unknown>(
  response: JsonApiResponse | null | undefined,
): HydratedSingleResult<T> | HydratedArrayResult<T> | null {
  // Handle null/undefined input (e.g., from 204 No Content)
  if (response === null || response === undefined) {
    return null;
  }

  const { data, included, meta, links } = response;

  // Preserve null for single resource responses (e.g., 204 No Content)
  if (data === null) {
    return { data: null, meta, links } as HydratedSingleResult<T>;
  }

  // Handle undefined/missing data as empty array
  if (data === undefined) {
    return { data: [] as T[], meta, links };
  }

  const map = buildResourceMap(included);

  if (Array.isArray(data)) {
    return {
      data: data.map((res) => hydrateOne<T>(res, map, true, new Set())),
      meta,
      links,
    };
  }

  return {
    data: hydrateOne<T>(data, map, true, new Set()),
    meta,
    links,
  };
}

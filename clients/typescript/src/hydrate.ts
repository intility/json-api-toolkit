import type {
  HydratedList,
  HydratedSingle,
  JsonApiArrayResponse,
  JsonApiResource,
  JsonApiSingleResponse,
} from './types/jsonapi.ts';

type IncludedMap = Record<string, Record<string, JsonApiResource>>;

function buildIncludedMap(included: JsonApiResource[] = []): IncludedMap {
  const map: IncludedMap = {};
  for (const res of included) {
    (map[res.type] ??= {})[res.id] = res;
  }
  return map;
}

/**
 * Flattens one resource: `{ id, ...attributes, ...relationships }`.
 * Relationships resolve from `included`; a linked resource that is not
 * included resolves to `null` (to-one) or is dropped (to-many). A cycle
 * back to a resource already on the current path resolves to `null`.
 */
function hydrateOne<T>(
  resource: JsonApiResource,
  map: IncludedMap,
  path: Set<string>,
): T {
  const key = `${resource.type}:${resource.id}`;
  const out: Record<string, unknown> = {
    id: resource.id,
    ...resource.attributes,
  };
  const nextPath = new Set(path).add(key);

  const resolve = (ref: { id: string; type: string }): unknown => {
    const related = map[ref.type]?.[ref.id];
    if (!related || nextPath.has(`${ref.type}:${ref.id}`)) return null;
    return hydrateOne(related, map, nextPath);
  };

  for (const [name, rel] of Object.entries(resource.relationships ?? {})) {
    out[name] = Array.isArray(rel.data)
      ? rel.data.map(resolve).filter((r) => r !== null)
      : rel.data
      ? resolve(rel.data)
      : null;
  }
  return out as T;
}

/**
 * Hydrates a JSON:API document into plain objects. Lists return
 * `{ data, pagination }`; single resources return `{ data }`.
 *
 * Runtime validation is out of scope: the caller's `T` is trusted.
 */
export function hydrateResponse<T>(
  response: JsonApiSingleResponse,
): HydratedSingle<T>;
export function hydrateResponse<T>(
  response: JsonApiArrayResponse,
): HydratedList<T>;
export function hydrateResponse<T>(
  response: JsonApiSingleResponse | JsonApiArrayResponse,
): HydratedSingle<T> | HydratedList<T> {
  const map = buildIncludedMap(response.included);
  if (Array.isArray(response.data)) {
    return {
      data: response.data.map((res) => hydrateOne<T>(res, map, new Set())),
      pagination: response.meta?.pagination,
    };
  }
  return { data: hydrateOne<T>(response.data, map, new Set()) };
}

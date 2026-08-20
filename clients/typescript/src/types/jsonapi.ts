// deno-lint-ignore-file no-explicit-any
export interface JsonApiResource<T = any> {
  id: string;
  type: string;
  attributes: T;
  relationships?: Record<string, JsonApiRelationship>;
  links?: JsonApiLinks;
}

export type JsonApiRelationship =
  | { data: { id: string; type: string } | null }
  | { data: Array<{ id: string; type: string }> }
  | { data: null };

export interface JsonApiSingleResponse<T = any> {
  data: JsonApiResource<T>;
  included?: JsonApiResource[];
  meta?: { pagination?: JsonApiPaginationMeta };
  links?: JsonApiLinks;
}

export interface JsonApiArrayResponse<T = any> {
  data: JsonApiResource<T>[];
  included?: JsonApiResource[];
  meta?: { pagination?: JsonApiPaginationMeta };
  links?: JsonApiLinks;
}

export type JsonApiResponse<T = any> =
  | JsonApiSingleResponse<T>
  | JsonApiArrayResponse<T>;

export interface JsonApiPaginationMeta {
  totalResources: number;
  totalPages: number;
  currentPage: number;
  pageSize: number;
}

export interface JsonApiLinks {
  self?: string;
  related?: string;
  first?: string;
  last?: string;
  prev?: string;
  next?: string;
}

/**
 * Type for hydrated single resource result.
 */
export interface HydratedSingleResult<T> {
  data: T;
  meta?: { pagination?: JsonApiPaginationMeta };
  links?: JsonApiLinks;
}

/**
 * Type for hydrated array resource result.
 */
export interface HydratedArrayResult<T> {
  data: T[];
  meta?: { pagination?: JsonApiPaginationMeta };
  links?: JsonApiLinks;
}

/**
 * Union type for hydrated query result.
 */
export type HydratedQueryResult<T> =
  | HydratedSingleResult<T>
  | HydratedArrayResult<T>;

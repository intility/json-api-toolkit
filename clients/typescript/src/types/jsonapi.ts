export type JsonApiAttributes = Record<string, unknown>;

export interface JsonApiResource<T = JsonApiAttributes> {
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

export type JsonApiMeta = Record<string, unknown> & {
  pagination?: JsonApiPaginationMeta;
};

export interface JsonApiSingleResponse<T = JsonApiAttributes> {
  data: JsonApiResource<T>;
  included?: JsonApiResource[];
  meta?: JsonApiMeta;
  links?: JsonApiLinks;
}

export interface JsonApiArrayResponse<T = JsonApiAttributes> {
  data: JsonApiResource<T>[];
  included?: JsonApiResource[];
  meta?: JsonApiMeta;
  links?: JsonApiLinks;
}

export type JsonApiResponse<T = JsonApiAttributes> =
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

export interface HydratedSingle<T> {
  data: T;
}

export interface HydratedList<T> {
  data: T[];
  /** Present only when the request was paginated (any `page[...]` param). */
  pagination?: JsonApiPaginationMeta;
}

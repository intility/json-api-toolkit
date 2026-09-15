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

/**
 * Runtime shape of one resource, emitted by `dotnet jsonapi-typegen` next to
 * the interface it describes. Lets hydration be honest about what the wire
 * omits: null-stripped attributes become `null`, un-included relationships
 * become `null` (to-one) or `[]` (to-many). Names are checked against `T`,
 * so a stale generated file fails to compile.
 */
export interface JsonApiResourceDescriptor<T>
  extends JsonApiResourceDescriptorBase {
  readonly attributes: readonly (keyof T & string)[];
  readonly toOne: readonly (keyof T & string)[];
  readonly toMany: readonly (keyof T & string)[];
}

/** Untyped form of {@link JsonApiResourceDescriptor}, for registries. */
export interface JsonApiResourceDescriptorBase {
  /** Wire `type`, also the default collection path. */
  readonly type: string;
  readonly attributes: readonly string[];
  readonly toOne: readonly string[];
  readonly toMany: readonly string[];
}

/** Descriptors keyed by wire type, for resolving included resources. */
export type JsonApiResourceDescriptors = Record<
  string,
  JsonApiResourceDescriptorBase
>;

export interface HydratedSingle<T> {
  data: T;
}

export interface HydratedList<T> {
  data: T[];
  /** Present only when the request was paginated (any `page[...]` param). */
  pagination?: JsonApiPaginationMeta;
}

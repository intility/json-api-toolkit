type UUID = `${string}-${string}-${string}-${string}-${string}`;

/**
 * Primitive types for attribute detection.
 */
export type JsonApiPrimitive =
  | string
  | number
  | boolean
  | Date
  | UUID
  | null
  | undefined;

/**
 * Only string keys from T.
 */
type StringKeys<T> = Extract<keyof T, string>;

/**
 * Attribute values: primitives and primitive arrays (JSON columns).
 * Nullability is stripped first, so `string | null` classifies as `string`.
 */
type IsAttribute<V> = NonNullable<V> extends
  JsonApiPrimitive | JsonApiPrimitive[] ? true
  : false;

/**
 * Extracts keys from T whose values are attributes, excluding "id" and "type".
 */
export type DirectAttributeKeys<T> = Exclude<
  {
    [K in StringKeys<T>]: IsAttribute<T[K]> extends true ? K : never;
  }[StringKeys<T>],
  'id' | 'type'
>;

/**
 * Extracts keys from T whose values are objects or object arrays
 * (relationships). `User | null` and optional properties count.
 */
export type RelationshipKeys<T> = {
  [K in StringKeys<T>]: IsAttribute<T[K]> extends true ? never
    : NonNullable<T[K]> extends object ? K
    : never;
}[StringKeys<T>];

/**
 * Extracts attributes from a to-one relationship's type (to-many yields none).
 */
type RelationshipAttributeKeys<T, R extends keyof T> = NonNullable<
  T[R]
> extends Array<unknown> ? never
  : NonNullable<T[R]> extends object ? DirectAttributeKeys<NonNullable<T[R]>>
  : never;

/**
 * Direct attribute keys of a relationship's target type, whether the
 * relationship is to-one or to-many. Used for `filterIncluded()`, which
 * (unlike dot-path filtering) reaches through to-many relationships too.
 */
export type IncludedAttributeKeys<T, R extends RelationshipKeys<T>> =
  DirectAttributeKeys<
    NonNullable<T[R]> extends Array<infer E> ? E : NonNullable<T[R]>
  >;

/**
 * Nested relationship attribute keys in the format "relationship.attribute".
 */
type NestedAttributeKeys<T> = {
  [R in RelationshipKeys<T>]: RelationshipAttributeKeys<T, R> extends never
    ? never
    : `${R}.${RelationshipAttributeKeys<T, R>}`;
}[RelationshipKeys<T>];

/**
 * Escape hatch for filter paths deeper than one relationship level, e.g.
 * "comments.author.name" (a to-many relationship's to-one relationship's
 * attribute). The backend walks dot-paths up to a recursion guard
 * (`FilterExpressionComposer.MaxRecursionDepth`, 5 segments) and goes
 * through to-many relationships via `Any()`; modeling every reachable
 * path at the type level isn't worth it, so only the first segment (a
 * real relationship on `T`) is checked. Everything past the first dot is
 * unverified at compile time; a typo two levels deep is a runtime 200
 * with the filter silently ignored (or a 400 in strict mode), not a
 * compile error.
 */
type DeepAttributeKeys<T> = {
  [R in RelationshipKeys<T>]: `${R}.${string}.${string}`;
}[RelationshipKeys<T>];

/**
 * Combined attribute keys: direct attributes, one typed level of nested
 * relationship attributes, and the deep-path escape hatch above.
 */
export type AttributeKeys<T> =
  | DirectAttributeKeys<T>
  | NestedAttributeKeys<T>
  | DeepAttributeKeys<T>;

/**
 * Supported JSON:API filter operators for `filter(field, op, value)`.
 * Excludes `isnull`/`isnotnull`: use `filterNull()`/`filterNotNull()` instead,
 * since those operators ignore the filter value entirely on the wire.
 */
export type FilterOp =
  | 'eq' // equal
  | 'ne' // not equal
  | 'gt' // greater than
  | 'lt' // less than
  | 'ge' // greater than or equal
  | 'le' // less than or equal
  | 'like' // like
  | 'in' // in
  | 'nin'; // not in

/**
 * The full set of operators the wire accepts, including the null checks.
 * Internal: `SimpleFilter` needs this so `filterNull()`/`filterNotNull()`
 * can construct a valid filter; the public `filter()` op param stays
 * narrowed to `FilterOp`.
 */
export type WireFilterOp = FilterOp | 'isnull' | 'isnotnull';

/**
 * Sort type for a JSON:API resource. Direct attributes only: the backend
 * silently ignores a dot-path sort field (unlike filters, which do walk
 * relationships), so `AttributeKeys<T>` would let a compiling call do
 * nothing on the wire.
 */
export type JsonApiSort<T> = Array<
  DirectAttributeKeys<T> | `-${DirectAttributeKeys<T>}`
>;

/**
 * Include type for a JSON:API resource (supports dot notation for nested relationships).
 */
export type JsonApiInclude<T> = Array<
  RelationshipKeys<T> | `${RelationshipKeys<T>}.${string}`
>;

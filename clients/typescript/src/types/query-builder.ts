type UUID = `${string}-${string}-${string}-${string}-${string}`;

/**
 * Primitive types for attribute detection.
 */
export type Primitive =
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
type IsAttribute<V> = NonNullable<V> extends Primitive | Primitive[] ? true
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
 * Nested relationship attribute keys in the format "relationship.attribute".
 */
type NestedAttributeKeys<T> = {
  [R in RelationshipKeys<T>]: RelationshipAttributeKeys<T, R> extends never
    ? never
    : `${R}.${RelationshipAttributeKeys<T, R>}`;
}[RelationshipKeys<T>];

/**
 * Combined attribute keys: direct attributes + nested relationship attributes.
 */
export type AttributeKeys<T> = DirectAttributeKeys<T> | NestedAttributeKeys<T>;

/**
 * Supported JSON:API filter operators.
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
  | 'nin' // not in
  | 'isnull' // is null
  | 'isnotnull'; // is not null

/**
 * Sort type for a JSON:API resource.
 */
export type Sort<T> = Array<AttributeKeys<T> | `-${AttributeKeys<T>}`>;

/**
 * Include type for a JSON:API resource (supports dot notation for nested relationships).
 */
export type Include<T> = Array<
  RelationshipKeys<T> | `${RelationshipKeys<T>}.${string}`
>;

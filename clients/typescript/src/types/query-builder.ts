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
 * Extracts keys from T whose values are primitives (attributes),
 * but excludes "id" and "type".
 */
export type DirectAttributeKeys<T> = Exclude<
  {
    [K in StringKeys<T>]: T[K] extends Primitive ? K : never;
  }[StringKeys<T>],
  'id' | 'type'
>;

/**
 * Extract keys from T whose values are objects or arrays (relationships).
 */
export type RelationshipKeys<T> = {
  [K in StringKeys<T>]: T[K] extends Array<unknown> | object
    ? (T[K] extends Primitive ? never : K)
    : never;
}[StringKeys<T>];

/**
 * Extracts primitive attributes from a relationship type (excluding arrays).
 */
type RelationshipAttributeKeys<T, R extends keyof T> = T[R] extends
  Array<unknown> ? never
  : T[R] extends object ? Exclude<
      {
        [K in StringKeys<T[R]>]: T[R][K] extends Primitive ? K : never;
      }[StringKeys<T[R]>],
      'id' | 'type'
    >
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

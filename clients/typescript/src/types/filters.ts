import type { AttributeKeys, WireFilterOp } from './query-builder.ts';

/**
 * Represents a simple filter on a single attribute.
 */
export type SimpleFilter<T> = {
  field: AttributeKeys<T>;
  op: WireFilterOp;
  value: unknown;
};

/**
 * Logical group types for filter groups. No "and" (top-level filters are
 * already AND'd) and no nesting: the backend only parses one flat level.
 */
export type LogicalGroupType = 'or' | 'not';

/**
 * A filter group is either a single attribute filter, or a logical group of
 * flat (non-nested) attribute filters.
 */
export type FilterGroup<T> =
  | { type: 'simple'; filter: SimpleFilter<T> }
  | { type: LogicalGroupType; filters: SimpleFilter<T>[] };

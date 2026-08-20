// deno-lint-ignore-file no-explicit-any
import type { AttributeKeys, FilterOp } from './query-builder.ts';

/**
 * Represents a simple filter on a single attribute.
 */
export type SimpleFilter<T> = {
  field: AttributeKeys<T>;
  op: FilterOp;
  value: any;
};

/**
 * Logical group types for filter groups.
 */
export type LogicalGroupType = 'or' | 'and' | 'not';

/**
 * Recursive filter group type: either a simple filter or a logical group of filters.
 */
export type FilterGroup<T> =
  | { type: 'simple'; filter: SimpleFilter<T> }
  | { type: LogicalGroupType; filters: FilterGroup<T>[] };

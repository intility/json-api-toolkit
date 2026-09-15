import type { AttributeKeys, FilterOp, WireFilterOp } from './query-builder.ts';

/**
 * Represents a simple filter on a single attribute.
 */
export type SimpleFilter<T> = {
  field: AttributeKeys<T>;
  op: WireFilterOp;
  value: unknown;
};

/**
 * A filter on an included relationship's field, scoped to one `or`/`not`
 * group. Wire form: `filter[or][0][relationship][field][op]=value`. The
 * backend parses this bracket form inside groups the same way it parses
 * the ungrouped form (`filterIncluded` on `JsonApiQueryBuilder`).
 */
export type IncludedGroupFilter = {
  relationship: string;
  field: string;
  op: FilterOp;
  value: unknown;
};

/**
 * One item in a group's flat filter list: either a plain attribute filter
 * or an included-relationship filter.
 */
export type GroupFilterItem<T> = SimpleFilter<T> | IncludedGroupFilter;

/**
 * Logical group types for filter groups. No "and" (top-level filters are
 * already AND'd) and no nesting: the backend only parses one flat level.
 */
export type LogicalGroupType = 'or' | 'not';

/**
 * A filter group is either a single attribute filter, or a logical group of
 * flat (non-nested) attribute/included filters.
 */
export type FilterGroup<T> =
  | { type: 'simple'; filter: SimpleFilter<T> }
  | { type: LogicalGroupType; filters: GroupFilterItem<T>[] };

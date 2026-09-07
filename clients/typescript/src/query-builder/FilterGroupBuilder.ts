import type {
  AttributeKeys,
  FilterOp,
  IncludedAttributeKeys,
  RelationshipKeys,
} from '../types/query-builder.ts';
import type { GroupFilterItem } from '../types/filters.ts';

/**
 * Builder for the flat filter list inside an `or()`/`not()` group.
 * No nesting: the backend only parses one flat level of a logical group.
 */
export class FilterGroupBuilder<T> {
  private filters: GroupFilterItem<T>[] = [];

  /**
   * Add a simple filter to this group.
   */
  filter<K extends AttributeKeys<T>>(
    field: K,
    op: FilterOp,
    value: unknown,
  ): this {
    this.filters.push({ field, op, value });
    return this;
  }

  /**
   * Filter an included relationship within this group:
   * `filter[or][0][relationship][field][op]=value`. Mirrors
   * `JsonApiQueryBuilder#filterIncluded`; requires the relationship to
   * also be passed to `.include()`, or the backend drops the filter.
   */
  filterIncluded<R extends RelationshipKeys<T>>(
    relationship: R,
    field: IncludedAttributeKeys<T, R>,
    op: FilterOp,
    value: unknown,
  ): this {
    this.filters.push({
      relationship: String(relationship),
      field: String(field),
      op,
      value,
    });
    return this;
  }

  /**
   * Returns the built flat filter list.
   */
  build(): GroupFilterItem<T>[] {
    return this.filters;
  }
}

import type { AttributeKeys, FilterOp } from '../types/query-builder.ts';
import type { SimpleFilter } from '../types/filters.ts';

/**
 * Builder for the flat filter list inside an `or()`/`not()` group.
 * No nesting: the backend only parses one flat level of a logical group.
 */
export class FilterGroupBuilder<T> {
  private filters: SimpleFilter<T>[] = [];

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
   * Returns the built flat filter list.
   */
  build(): SimpleFilter<T>[] {
    return this.filters;
  }
}

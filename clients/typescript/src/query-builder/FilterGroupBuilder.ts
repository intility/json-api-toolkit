import type { AttributeKeys, FilterOp } from '../types/query-builder.ts';
import type { FilterGroup } from '../types/filters.ts';

/**
 * Builder for logical filter groups (or, and, not).
 * Alows for chaining and nesting of filter groups.
 */
export class FilterGroupBuilder<T> {
  private groups: FilterGroup<T>[] = [];

  /**
   * Add a simple filter to this group.
   */
  filter<K extends AttributeKeys<T>>(
    field: K,
    op: FilterOp,
    value: unknown,
  ): this {
    this.groups.push({
      type: 'simple',
      filter: { field, op, value },
    });
    return this;
  }

  /**
   * Add an "or" logical group to this group.
   */
  or(cb: (b: FilterGroupBuilder<T>) => void): this {
    const builder = new FilterGroupBuilder<T>();
    cb(builder);
    this.groups.push({
      type: 'or',
      filters: builder.groups,
    });
    return this;
  }

  /**
   * Add an "and" logical group to this group.
   */
  and(cb: (b: FilterGroupBuilder<T>) => void): this {
    const builder = new FilterGroupBuilder<T>();
    cb(builder);
    this.groups.push({
      type: 'and',
      filters: builder.groups,
    });
    return this;
  }

  /**
   * Add a "not" logical group to this group.
   */
  not(cb: (b: FilterGroupBuilder<T>) => void): this {
    const builder = new FilterGroupBuilder<T>();
    cb(builder);
    this.groups.push({
      type: 'not',
      filters: builder.groups,
    });
    return this;
  }

  /**
   * Returns the built filter group array.
   */
  build(): FilterGroup<T>[] {
    return this.groups;
  }
}

import type {
  AttributeKeys,
  DirectAttributeKeys,
  FilterOp,
  Include,
  Sort,
} from '../types/query-builder.ts';
import { FilterGroupBuilder } from './FilterGroupBuilder.ts';
import type { FilterGroup } from '../types/filters.ts';
import type {
  JsonApiResourceDescriptor,
  JsonApiResourceDescriptorBase,
} from '../types/jsonapi.ts';

/**
 * Serializes one attribute filter into a `filter[...]` query parameter.
 */
function serializeSimpleFilter(
  filter: { field: unknown; op: string; value: unknown },
  prefix: string[],
): [string, string] {
  const { field, op, value } = filter;
  const key = op === 'eq'
    ? [...prefix, String(field)]
    : [...prefix, String(field), op];
  const val = Array.isArray(value) ? value.join(',') : value;
  return [`filter${key.map((k) => `[${k}]`).join('')}`, String(val)];
}

/**
 * Serializes a filter group (a simple filter, or a flat or/not group) into
 * JSON:API query parameter key-value pairs.
 */
function serializeFilterGroup<T>(group: FilterGroup<T>): [string, string][] {
  if (group.type === 'simple') {
    return [serializeSimpleFilter(group.filter, [])];
  }
  return group.filters.map((f, i) =>
    serializeSimpleFilter(f, [group.type, String(i)])
  );
}

/**
 * Main JSON:API query builder.
 * Supports type-safe filters (including logical groups), sorts, includes, and pagination.
 */
export class JsonApiQueryBuilder<T> {
  private filterGroups: FilterGroup<T>[] = [];
  private sorts: Sort<T> = [];
  private includes: Include<T> = [];
  private pagination: { number?: number; size?: number } = {};
  private fieldsets: Map<string, string[]> = new Map();

  /**
   * Add a simple filter.
   * @param field - The attribute to filter on
   * @param value - The value to filter by (uses "eq" operator)
   */
  filter<K extends AttributeKeys<T>>(field: K, value: unknown): this;
  /**
   * Add a simple filter with explicit operator.
   * @param field - The attribute to filter on
   * @param op - The filter operator
   * @param value - The value to filter by
   */
  filter<K extends AttributeKeys<T>>(
    field: K,
    op: FilterOp,
    value: unknown,
  ): this;
  filter<K extends AttributeKeys<T>>(
    field: K,
    opOrValue: unknown,
    value?: unknown,
  ): this {
    const op = value === undefined ? 'eq' : (opOrValue as FilterOp);
    const val = value === undefined ? opOrValue : value;
    this.filterGroups.push({
      type: 'simple',
      filter: { field, op, value: val },
    });
    return this;
  }

  /**
   * Add an "or" logical filter group.
   */
  or(cb: (b: FilterGroupBuilder<T>) => void): this {
    const builder = new FilterGroupBuilder<T>();
    cb(builder);
    this.filterGroups.push({
      type: 'or',
      filters: builder.build(),
    });
    return this;
  }

  /**
   * Add a "not" logical filter group.
   */
  not(cb: (b: FilterGroupBuilder<T>) => void): this {
    const builder = new FilterGroupBuilder<T>();
    cb(builder);
    this.filterGroups.push({
      type: 'not',
      filters: builder.build(),
    });
    return this;
  }

  /**
   * Set sort fields (comma-separated, supports -field for descending).
   */
  sort(...fields: Sort<T>): this {
    this.sorts = fields;
    return this;
  }

  /**
   * Set included relationships (comma-separated, supports dot notation).
   */
  include(...fields: Include<T>): this {
    this.includes = fields;
    return this;
  }

  /**
   * Set a sparse fieldset for a resource: `fields[type]=field1,field2`.
   * Pass a generated descriptor for the wire type and typed field names.
   */
  fields<R>(
    descriptor: JsonApiResourceDescriptor<R>,
    fields: DirectAttributeKeys<R>[],
  ): this;
  /** Sparse fieldset by wire type name, untyped field names. */
  fields(type: string, fields: string[]): this;
  /** Sparse fieldset by wire type name, field names typed against `R`. */
  fields<R>(type: string, fields: DirectAttributeKeys<R>[]): this;
  fields(
    source: string | JsonApiResourceDescriptorBase,
    fields: string[],
  ): this {
    this.fieldsets.set(
      typeof source === 'string' ? source : source.type,
      fields,
    );
    return this;
  }

  /**
   * Set pagination (page number and size).
   */
  page(number: number, size: number): this {
    this.pagination = { number, size };
    return this;
  }

  /**
   * Build the query string for use in a JSON:API request.
   */
  build(): string {
    const params = new URLSearchParams();

    // Serialize all filter groups
    for (const group of this.filterGroups) {
      for (const [key, value] of serializeFilterGroup(group)) {
        params.append(key, value);
      }
    }

    // Sort
    if (this.sorts.length) {
      params.append('sort', this.sorts.join(','));
    }

    // Include
    if (this.includes.length) {
      params.append('include', this.includes.join(','));
    }

    // Sparse fieldsets
    for (const [type, fields] of this.fieldsets) {
      if (fields.length) {
        params.append(`fields[${type}]`, fields.join(','));
      }
    }

    // Pagination
    if (this.pagination.number !== undefined) {
      params.append('page[number]', String(this.pagination.number));
    }
    if (this.pagination.size !== undefined) {
      params.append('page[size]', String(this.pagination.size));
    }

    return params.toString();
  }
}

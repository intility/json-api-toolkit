import { assertEquals } from '@std/assert';
import { JsonApiQueryBuilder } from './JsonApiQueryBuilder.ts';
import type { JsonApiResourceDescriptor } from '../types/jsonapi.ts';

interface User {
  id: string;
  type: string;
  name: string;
  email: string;
}

interface Tag {
  id: string;
  type: string;
  label: string;
}

interface Todo {
  id: string;
  type: string;
  title: string;
  completed: boolean;
  dueDate: string;
  owner: User;
  tags: Tag[];
}

/** Helper: parse build() output into a URLSearchParams for flexible assertions. */
function parse(query: string): URLSearchParams {
  return new URLSearchParams(query);
}

/** Fixture as `dotnet jsonapi-typegen` would emit it next to `Todo`. */
const TodoDescriptor: JsonApiResourceDescriptor<Todo> = {
  type: 'todos',
  attributes: ['title', 'completed', 'dueDate'],
  toOne: ['owner'],
  toMany: ['tags'],
};

Deno.test('JsonApiQueryBuilder', async (t) => {
  // --- Filters ---

  await t.step('simple filter defaults to eq operator', () => {
    const qs = new JsonApiQueryBuilder<Todo>()
      .filter('title', 'hello')
      .build();

    assertEquals(parse(qs).get('filter[title]'), 'hello');
  });

  await t.step('filter with explicit operator', () => {
    const qs = new JsonApiQueryBuilder<Todo>()
      .filter('title', 'like', '%hello%')
      .build();

    assertEquals(parse(qs).get('filter[title][like]'), '%hello%');
  });

  await t.step('multiple filters', () => {
    const qs = new JsonApiQueryBuilder<Todo>()
      .filter('title', 'hello')
      .filter('completed', true)
      .build();

    const params = parse(qs);
    assertEquals(params.get('filter[title]'), 'hello');
    assertEquals(params.get('filter[completed]'), 'true');
  });

  await t.step('filter with in operator and array value', () => {
    const qs = new JsonApiQueryBuilder<Todo>()
      .filter('title', 'in', ['a', 'b', 'c'])
      .build();

    assertEquals(parse(qs).get('filter[title][in]'), 'a,b,c');
  });

  await t.step('Date value serializes to ISO 8601, not a locale string', () => {
    const qs = new JsonApiQueryBuilder<Todo>()
      .filter('dueDate', 'gt', new Date('2025-01-20T00:00:00.000Z'))
      .build();

    assertEquals(
      parse(qs).get('filter[dueDate][gt]'),
      '2025-01-20T00:00:00.000Z',
    );
  });

  await t.step(
    'array of Date values in an "in" filter also serializes to ISO',
    () => {
      const qs = new JsonApiQueryBuilder<Todo>()
        .filter('dueDate', 'in', [
          new Date('2025-01-01T00:00:00.000Z'),
          new Date('2025-02-01T00:00:00.000Z'),
        ])
        .build();

      assertEquals(
        parse(qs).get('filter[dueDate][in]'),
        '2025-01-01T00:00:00.000Z,2025-02-01T00:00:00.000Z',
      );
    },
  );

  await t.step('nested attribute filter (dot notation)', () => {
    const qs = new JsonApiQueryBuilder<Todo>()
      .filter('owner.name', 'Alice')
      .build();

    assertEquals(parse(qs).get('filter[owner.name]'), 'Alice');
  });

  await t.step('deep dot-path filter (escape hatch, 2+ levels)', () => {
    const qs = new JsonApiQueryBuilder<Todo>()
      .filter('owner.company.name', 'eq', 'Acme')
      .build();

    assertEquals(parse(qs).get('filter[owner.company.name]'), 'Acme');
  });

  await t.step('filterNull() emits filter[field][isnull]', () => {
    const qs = new JsonApiQueryBuilder<Todo>()
      .filterNull('dueDate')
      .build();

    assertEquals(parse(qs).get('filter[dueDate][isnull]'), 'true');
  });

  await t.step('filterNotNull() emits filter[field][isnotnull]', () => {
    const qs = new JsonApiQueryBuilder<Todo>()
      .filterNotNull('dueDate')
      .build();

    assertEquals(parse(qs).get('filter[dueDate][isnotnull]'), 'true');
  });

  await t.step(
    'filterIncluded() on a to-one relationship emits filter[rel][field][op]',
    () => {
      const qs = new JsonApiQueryBuilder<Todo>()
        .filterIncluded('owner', 'name', 'like', 'Ali')
        .build();

      assertEquals(parse(qs).get('filter[owner][name][like]'), 'Ali');
    },
  );

  await t.step(
    'filterIncluded() on a to-many relationship emits filter[rel][field][op]',
    () => {
      const qs = new JsonApiQueryBuilder<Todo>()
        .filterIncluded('tags', 'label', 'eq', 'urgent')
        .build();

      assertEquals(parse(qs).get('filter[tags][label][eq]'), 'urgent');
    },
  );

  // --- Logical groups ---

  await t.step('or() group', () => {
    const qs = new JsonApiQueryBuilder<Todo>()
      .or((b) => {
        b.filter('title', 'eq', 'A');
        b.filter('title', 'eq', 'B');
      })
      .build();

    const params = parse(qs);
    assertEquals(params.get('filter[or][0][title]'), 'A');
    assertEquals(params.get('filter[or][1][title]'), 'B');
  });

  await t.step('not() group', () => {
    const qs = new JsonApiQueryBuilder<Todo>()
      .not((b) => {
        b.filter('completed', 'eq', 'true');
      })
      .build();

    assertEquals(parse(qs).get('filter[not][0][completed]'), 'true');
  });

  // --- Sorting ---

  await t.step('sort ascending', () => {
    const qs = new JsonApiQueryBuilder<Todo>()
      .sort('title')
      .build();

    assertEquals(parse(qs).get('sort'), 'title');
  });

  await t.step('sort descending', () => {
    const qs = new JsonApiQueryBuilder<Todo>()
      .sort('-dueDate')
      .build();

    assertEquals(parse(qs).get('sort'), '-dueDate');
  });

  await t.step('sort multiple fields', () => {
    const qs = new JsonApiQueryBuilder<Todo>()
      .sort('title', '-dueDate')
      .build();

    assertEquals(parse(qs).get('sort'), 'title,-dueDate');
  });

  await t.step('repeat sort() calls append, same as filter()', () => {
    const qs = new JsonApiQueryBuilder<Todo>()
      .sort('title')
      .sort('-dueDate')
      .build();

    assertEquals(parse(qs).get('sort'), 'title,-dueDate');
  });

  // --- Includes ---

  await t.step('include relationships', () => {
    const qs = new JsonApiQueryBuilder<Todo>()
      .include('owner', 'tags')
      .build();

    assertEquals(parse(qs).get('include'), 'owner,tags');
  });

  await t.step('repeat include() calls append, same as filter()', () => {
    const qs = new JsonApiQueryBuilder<Todo>()
      .include('owner')
      .include('tags')
      .build();

    assertEquals(parse(qs).get('include'), 'owner,tags');
  });

  await t.step('include with dot notation', () => {
    const qs = new JsonApiQueryBuilder<Todo>()
      .include('owner.department')
      .build();

    assertEquals(parse(qs).get('include'), 'owner.department');
  });

  // --- Sparse fieldsets ---

  await t.step('fields for a resource type', () => {
    const qs = new JsonApiQueryBuilder<Todo>()
      .fields('todos', ['title', 'completed'])
      .build();

    assertEquals(parse(qs).get('fields[todos]'), 'title,completed');
  });

  await t.step('fields from a generated resource descriptor', () => {
    const qs = new JsonApiQueryBuilder<Todo>()
      .fields(TodoDescriptor, ['title', 'completed'])
      .build();

    assertEquals(parse(qs).get('fields[todos]'), 'title,completed');
  });

  await t.step('fields for multiple resource types', () => {
    const qs = new JsonApiQueryBuilder<Todo>()
      .fields('todos', ['title'])
      .fields('users', ['name', 'email'])
      .build();

    const params = parse(qs);
    assertEquals(params.get('fields[todos]'), 'title');
    assertEquals(params.get('fields[users]'), 'name,email');
  });

  // --- Pagination ---

  await t.step('page number and size', () => {
    const qs = new JsonApiQueryBuilder<Todo>()
      .page(2, 25)
      .build();

    const params = parse(qs);
    assertEquals(params.get('page[number]'), '2');
    assertEquals(params.get('page[size]'), '25');
  });

  // --- Combined ---

  await t.step('combined filter + sort + include + fields + page', () => {
    const qs = new JsonApiQueryBuilder<Todo>()
      .filter('completed', false)
      .sort('-dueDate')
      .include('owner', 'tags')
      .fields('todos', ['title', 'dueDate'])
      .page(1, 10)
      .build();

    const params = parse(qs);
    assertEquals(params.get('filter[completed]'), 'false');
    assertEquals(params.get('sort'), '-dueDate');
    assertEquals(params.get('include'), 'owner,tags');
    assertEquals(params.get('fields[todos]'), 'title,dueDate');
    assertEquals(params.get('page[number]'), '1');
    assertEquals(params.get('page[size]'), '10');
  });

  // --- Edge cases ---

  await t.step('empty builder produces empty string', () => {
    const qs = new JsonApiQueryBuilder<Todo>().build();
    assertEquals(qs, '');
  });

  await t.step('fields with empty array produces no output', () => {
    const qs = new JsonApiQueryBuilder<Todo>()
      .fields('todos', [])
      .build();

    assertEquals(qs, '');
  });
});

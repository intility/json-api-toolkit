import { assertEquals } from '@std/assert';
import { FilterGroupBuilder } from './FilterGroupBuilder.ts';

interface Todo {
  id: string;
  type: string;
  title: string;
  completed: boolean;
  dueDate: string;
}

Deno.test('FilterGroupBuilder', async (t) => {
  await t.step('simple filter produces correct structure', () => {
    const builder = new FilterGroupBuilder<Todo>();
    builder.filter('title', 'eq', 'hello');

    assertEquals(builder.build(), [
      { type: 'simple', filter: { field: 'title', op: 'eq', value: 'hello' } },
    ]);
  });

  await t.step('multiple filters', () => {
    const builder = new FilterGroupBuilder<Todo>();
    builder.filter('title', 'eq', 'hello');
    builder.filter('completed', 'eq', true);

    const result = builder.build();
    assertEquals(result.length, 2);
    assertEquals(result[0], {
      type: 'simple',
      filter: { field: 'title', op: 'eq', value: 'hello' },
    });
    assertEquals(result[1], {
      type: 'simple',
      filter: { field: 'completed', op: 'eq', value: true },
    });
  });

  await t.step('nested or group', () => {
    const builder = new FilterGroupBuilder<Todo>();
    builder.or((b) => {
      b.filter('title', 'eq', 'A');
      b.filter('title', 'eq', 'B');
    });

    assertEquals(builder.build(), [
      {
        type: 'or',
        filters: [
          {
            type: 'simple',
            filter: { field: 'title', op: 'eq', value: 'A' },
          },
          {
            type: 'simple',
            filter: { field: 'title', op: 'eq', value: 'B' },
          },
        ],
      },
    ]);
  });

  await t.step('nested and group', () => {
    const builder = new FilterGroupBuilder<Todo>();
    builder.and((b) => {
      b.filter('completed', 'eq', true);
      b.filter('dueDate', 'gt', '2025-01-01');
    });

    const result = builder.build();
    assertEquals(result.length, 1);
    assertEquals(result[0].type, 'and');
  });

  await t.step('nested not group', () => {
    const builder = new FilterGroupBuilder<Todo>();
    builder.not((b) => {
      b.filter('completed', 'eq', true);
    });

    const result = builder.build();
    assertEquals(result.length, 1);
    assertEquals(result[0].type, 'not');
  });

  await t.step('deeply nested groups', () => {
    const builder = new FilterGroupBuilder<Todo>();
    builder.or((b) => {
      b.filter('title', 'eq', 'A');
      b.and((inner) => {
        inner.filter('completed', 'eq', true);
        inner.filter('dueDate', 'lt', '2025-12-31');
      });
    });

    const result = builder.build();
    assertEquals(result.length, 1);
    assertEquals(result[0].type, 'or');

    // Verify the inner structure
    if (result[0].type !== 'simple') {
      assertEquals(result[0].filters.length, 2);
      assertEquals(result[0].filters[0].type, 'simple');
      assertEquals(result[0].filters[1].type, 'and');
    }
  });

  await t.step('empty builder returns empty array', () => {
    const builder = new FilterGroupBuilder<Todo>();
    assertEquals(builder.build(), []);
  });

  await t.step('chaining returns this', () => {
    const builder = new FilterGroupBuilder<Todo>();
    const returned = builder
      .filter('title', 'eq', 'hello')
      .filter('completed', 'eq', true);

    assertEquals(returned, builder);
  });
});

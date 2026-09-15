import { assertEquals } from '@std/assert';
import { FilterGroupBuilder } from './FilterGroupBuilder.ts';

interface Owner {
  id: string;
  name: string;
}

interface Todo {
  id: string;
  type: string;
  title: string;
  completed: boolean;
  dueDate: string;
  owner: Owner;
}

Deno.test('FilterGroupBuilder', async (t) => {
  await t.step('simple filter produces correct structure', () => {
    const builder = new FilterGroupBuilder<Todo>();
    builder.filter('title', 'eq', 'hello');

    assertEquals(builder.build(), [
      { field: 'title', op: 'eq', value: 'hello' },
    ]);
  });

  await t.step('multiple filters', () => {
    const builder = new FilterGroupBuilder<Todo>();
    builder.filter('title', 'eq', 'hello');
    builder.filter('completed', 'eq', true);

    const result = builder.build();
    assertEquals(result.length, 2);
    assertEquals(result[0], { field: 'title', op: 'eq', value: 'hello' });
    assertEquals(result[1], { field: 'completed', op: 'eq', value: true });
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

  await t.step('filterIncluded adds a relationship-field filter', () => {
    const builder = new FilterGroupBuilder<Todo>();
    builder.filterIncluded('owner', 'name', 'like', 'a');

    assertEquals(builder.build(), [
      { relationship: 'owner', field: 'name', op: 'like', value: 'a' },
    ]);
  });

  await t.step('filter and filterIncluded can mix in one group', () => {
    const builder = new FilterGroupBuilder<Todo>();
    builder.filter('title', 'eq', 'hello').filterIncluded(
      'owner',
      'name',
      'like',
      'a',
    );

    assertEquals(builder.build(), [
      { field: 'title', op: 'eq', value: 'hello' },
      { relationship: 'owner', field: 'name', op: 'like', value: 'a' },
    ]);
  });
});

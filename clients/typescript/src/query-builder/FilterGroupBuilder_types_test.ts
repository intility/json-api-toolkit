/**
 * Type-level probes: nesting a logical group, or using `.and()`, must be a
 * compile error. The backend only parses one flat level (see DESIGN.md),
 * so the builder must not be able to express what it cannot carry.
 *
 * Wrapped in an uncalled function: `deno check` still type-checks the body,
 * but nothing here runs, so the `@ts-expect-error` calls never throw.
 */
import { FilterGroupBuilder } from './FilterGroupBuilder.ts';
import { JsonApiQueryBuilder } from './JsonApiQueryBuilder.ts';

interface User {
  id: string;
  type: string;
  name: string;
}

interface Todo {
  id: string;
  type: string;
  title: string;
  completed: boolean;
  owner: User | null;
}

function _typeOnlyProbes() {
  new JsonApiQueryBuilder<Todo>().or((b) => {
    b.filter('title', 'eq', 'A');
    // @ts-expect-error groups only take flat filters, no nested or/and/not
    b.or((inner) => inner.filter('completed', 'eq', true));
  });

  const builder = new FilterGroupBuilder<Todo>();
  // @ts-expect-error "and" is removed everywhere: top-level filters are already AND'd
  builder.and((b) => b.filter('completed', 'eq', true));

  const outer = new JsonApiQueryBuilder<Todo>();
  // @ts-expect-error "and" is removed everywhere: top-level filters are already AND'd
  outer.and((b) => b.filter('completed', 'eq', true));

  const nullCheck = new JsonApiQueryBuilder<Todo>();
  // @ts-expect-error isnull/isnotnull are not valid ops for the 3-arg form; use filterNull()/filterNotNull()
  nullCheck.filter('completed', 'isnull', true);

  const included = new JsonApiQueryBuilder<Todo>();
  // @ts-expect-error "title" is not an attribute of the "owner" relationship's type
  included.filterIncluded('owner', 'title', 'eq', 'x');

  const sorter = new JsonApiQueryBuilder<Todo>();
  // @ts-expect-error sort() is direct attributes only; the backend silently ignores a dot-path sort field
  sorter.sort('owner.name');
}

Deno.test('type probes compile', () => {
  void _typeOnlyProbes;
});

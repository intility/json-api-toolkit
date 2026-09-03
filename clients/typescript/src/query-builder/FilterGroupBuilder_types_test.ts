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

interface Todo {
  id: string;
  type: string;
  title: string;
  completed: boolean;
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
}

Deno.test('type probes compile', () => {
  void _typeOnlyProbes;
});

import { assertEquals } from '@std/assert';
import { createJsonApiClient } from './client.ts';
import { JsonApiRequestError } from './errors.ts';
import {
  createJsonApiErrorHandler,
  jsonApiResource,
} from './tanstack-query.ts';

interface Todo {
  id: string;
  title: string;
}

function setup(pages: Record<number, unknown[]>, totalPages: number) {
  const urls: string[] = [];
  const client = createJsonApiClient({
    baseUrl: 'https://api.test',
    fetch: ((input: string) => {
      urls.push(input);
      const page = Number(new URL(input).searchParams.get('page[number]') ?? 1);
      const body = {
        data: pages[page] ?? [],
        meta: {
          pagination: {
            currentPage: page,
            totalPages,
            totalResources: 0,
            pageSize: 1,
          },
        },
      };
      return Promise.resolve(
        new Response(JSON.stringify(body), {
          status: 200,
          headers: { 'content-type': 'application/vnd.api+json' },
        }),
      );
    }) as typeof fetch,
  });
  const invalidated: unknown[] = [];
  const queryClient = {
    invalidateQueries: (f: { queryKey: readonly unknown[] }) => {
      invalidated.push(f.queryKey);
      return Promise.resolve();
    },
  };
  const todos = jsonApiResource(client.resource<Todo>('/todos/'), queryClient);
  return { todos, urls, invalidated };
}

const todo = { type: 'todos', id: '1', attributes: { title: 'a' } };

Deno.test('jsonApiResource', async (t) => {
  await t.step(
    'list key carries the serialized query and fetches with it',
    async () => {
      const { todos, urls } = setup({ 1: [todo] }, 1);
      const opts = todos.list((q) => q.sort('title'));
      assertEquals(opts.queryKey, ['jsonapi', 'todos', 'list', 'sort=title']);
      const result = await opts.queryFn();
      assertEquals(result.data, [{ id: '1', title: 'a' }]);
      assertEquals(urls, ['https://api.test/todos?sort=title']);
    },
  );

  await t.step('detail key includes the id', async () => {
    const { todos, urls } = setup({}, 1);
    const opts = todos.detail(7);
    assertEquals(opts.queryKey, ['jsonapi', 'todos', 'detail', '7', '']);
    await opts.queryFn().catch(() => undefined);
    assertEquals(urls, ['https://api.test/todos/7']);
  });

  await t.step(
    'infiniteList pages from meta and stops at the last page',
    async () => {
      const { todos, urls } = setup({ 1: [todo], 2: [todo] }, 2);
      const opts = todos.infiniteList(1, (q) => q.sort('title'));
      assertEquals(opts.queryKey, [
        'jsonapi',
        'todos',
        'list',
        'sort=title',
        1,
      ]);
      const first = await opts.queryFn({ pageParam: opts.initialPageParam });
      assertEquals(opts.getNextPageParam(first), 2);
      const second = await opts.queryFn({ pageParam: 2 });
      assertEquals(opts.getNextPageParam(second), undefined);
      assertEquals(urls.map((u) => decodeURIComponent(u.split('?')[1])), [
        'sort=title&page[number]=1&page[size]=1',
        'sort=title&page[number]=2&page[size]=1',
      ]);
    },
  );

  await t.step('mutations invalidate the right prefix', async () => {
    const { todos, invalidated } = setup({}, 1);
    await todos.post().onSuccess();
    await todos.patch().onSuccess();
    await todos.delete().onSuccess();
    assertEquals(invalidated, [
      ['jsonapi', 'todos', 'list'],
      ['jsonapi', 'todos'],
      ['jsonapi', 'todos'],
    ]);
  });
});

Deno.test('createJsonApiErrorHandler', () => {
  const shown: string[] = [];
  const onError = createJsonApiErrorHandler({ show: (m) => shown.push(m) });
  const error = new JsonApiRequestError(400, [{ title: 'Bad' }]);
  onError(error);
  onError(error, undefined, undefined, { options: { onError: () => {} } });
  onError('boom', undefined, undefined, { options: {} });
  assertEquals(shown, ['Bad', 'boom']);
});

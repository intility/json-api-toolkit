import { assertEquals, assertRejects } from '@std/assert';
import { createJsonApiClient } from './client.ts';
import { JsonApiRequestError } from './errors.ts';
import type { JsonApiResourceDescriptor } from './types/jsonapi.ts';

type Todo = { id: string; title: string; completed: boolean };

const todoDoc = {
  id: '1',
  type: 'todos',
  attributes: { title: 'a', completed: false },
};
const hydratedTodo: Todo = { id: '1', title: 'a', completed: false };

/** Records the request it received and returns a canned Response. */
function fakeFetch(
  response: () => Response,
): { fetch: typeof fetch; lastRequest: () => Request } {
  let last: Request;
  return {
    fetch: ((input: string | URL | Request, init?: RequestInit) => {
      last = new Request(input, init);
      return Promise.resolve(response());
    }) as typeof fetch,
    lastRequest: () => last,
  };
}

function jsonResponse(status: number, body: unknown): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/vnd.api+json' },
  });
}

function setup(response: () => Response) {
  const { fetch: f, lastRequest } = fakeFetch(response);
  const client = createJsonApiClient({
    baseUrl: 'https://api.test/',
    fetch: f,
  });
  return { todos: client.resource<Todo>('/todos'), lastRequest };
}

Deno.test('list', async (t) => {
  await t.step('builds the URL from baseUrl, path, and query', async () => {
    const { todos, lastRequest } = setup(() => jsonResponse(200, { data: [] }));
    await todos.list((q) => q.filter('completed', true));
    assertEquals(
      lastRequest().url,
      'https://api.test/todos?filter%5Bcompleted%5D=true',
    );
    assertEquals(lastRequest().method, 'GET');
  });

  await t.step('accepts pre-built params for interop', async () => {
    const { todos, lastRequest } = setup(() => jsonResponse(200, { data: [] }));
    await todos.list({ params: 'page[number]=2' });
    assertEquals(lastRequest().url, 'https://api.test/todos?page[number]=2');
  });

  await t.step('returns hydrated data and pagination', async () => {
    const pagination = {
      totalResources: 1,
      totalPages: 1,
      currentPage: 1,
      pageSize: 10,
    };
    const { todos } = setup(() =>
      jsonResponse(200, { data: [todoDoc], meta: { pagination } })
    );
    assertEquals(await todos.list(), { data: [hydratedTodo], pagination });
  });

  await t.step('throws JsonApiRequestError on non-2xx', async () => {
    const { todos } = setup(() =>
      jsonResponse(404, {
        errors: [{ status: '404', code: 'RESOURCE_NOT_FOUND' }],
      })
    );
    const error = await assertRejects(() => todos.list(), JsonApiRequestError);
    assertEquals(error.status, 404);
    assertEquals(error.hasCode('RESOURCE_NOT_FOUND'), true);
  });

  await t.step('non-JSON:API error body yields empty errors', async () => {
    const { todos } = setup(() => new Response(null, { status: 415 }));
    const error = await assertRejects(() => todos.list(), JsonApiRequestError);
    assertEquals(error.status, 415);
    assertEquals(error.errors, []);
  });
});

Deno.test('resource(descriptor)', async (t) => {
  await t.step('paths from the wire type and fills omissions', async () => {
    type Article = { id: string; title: string; author: Todo | null };
    const Article: JsonApiResourceDescriptor<Article> = {
      type: 'articles',
      attributes: ['title'],
      toOne: ['author'],
      toMany: [],
    };
    const { fetch: f, lastRequest } = fakeFetch(() =>
      jsonResponse(200, {
        data: { id: '1', type: 'articles', attributes: { title: 'A' } },
      })
    );
    const client = createJsonApiClient({
      baseUrl: 'https://api.test',
      fetch: f,
    });
    const article = await client.resource(Article).get(1);
    assertEquals(lastRequest().url, 'https://api.test/articles/1');
    assertEquals(article, { id: '1', title: 'A', author: null });
  });
});

Deno.test('get', async (t) => {
  await t.step('appends the id and returns the resource', async () => {
    const { todos, lastRequest } = setup(() =>
      jsonResponse(200, { data: todoDoc })
    );
    const todo = await todos.get(1, (q) => q.include('owner' as never));
    assertEquals(lastRequest().url, 'https://api.test/todos/1?include=owner');
    assertEquals(todo, hydratedTodo);
  });
});

Deno.test('writes', async (t) => {
  await t.step(
    'post sends a plain DTO with the JSON:API content type',
    async () => {
      const { todos, lastRequest } = setup(() =>
        jsonResponse(201, { data: todoDoc })
      );
      const created = await todos.post({ title: 'a' });
      const req = lastRequest();
      assertEquals(req.method, 'POST');
      assertEquals(req.url, 'https://api.test/todos');
      assertEquals(req.headers.get('content-type'), 'application/vnd.api+json');
      assertEquals(await req.json(), { title: 'a' });
      assertEquals(created, hydratedTodo);
    },
  );

  await t.step('patch hits the id path with a partial DTO', async () => {
    const { todos, lastRequest } = setup(() =>
      jsonResponse(200, { data: todoDoc })
    );
    await todos.patch('1', { completed: true });
    const req = lastRequest();
    assertEquals(req.method, 'PATCH');
    assertEquals(req.url, 'https://api.test/todos/1');
    assertEquals(await req.json(), { completed: true });
  });

  await t.step('delete sends no body and resolves void on 204', async () => {
    const { todos, lastRequest } = setup(() =>
      new Response(null, { status: 204 })
    );
    assertEquals(await todos.delete(1), undefined);
    assertEquals(lastRequest().method, 'DELETE');
    assertEquals(lastRequest().url, 'https://api.test/todos/1');
    assertEquals(lastRequest().headers.get('content-type'), null);
  });
});

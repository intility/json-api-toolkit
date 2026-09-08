import { assertEquals } from '@std/assert';
import { hydrateResponse } from './hydrate.ts';
import type {
  JsonApiResource,
  JsonApiResourceDescriptor,
} from './types/jsonapi.ts';

type User = { id: string; name: string; friend?: User | null };
type Todo = {
  id: string;
  title: string;
  owner?: User | null;
  tags?: { id: string; label: string }[];
};

const alice: JsonApiResource = {
  id: '10',
  type: 'users',
  attributes: { name: 'Alice' },
};

Deno.test('hydrateResponse', async (t) => {
  await t.step('flattens attributes onto id, without type', () => {
    const { data } = hydrateResponse<Todo>({
      data: { id: '1', type: 'todos', attributes: { title: 'Buy milk' } },
    });
    assertEquals(data, { id: '1', title: 'Buy milk' });
  });

  await t.step('list returns data and pagination', () => {
    const pagination = {
      totalResources: 1,
      totalPages: 1,
      currentPage: 1,
      pageSize: 10,
    };
    const result = hydrateResponse<Todo>({
      data: [{ id: '1', type: 'todos', attributes: { title: 'Buy milk' } }],
      meta: { pagination },
      links: { self: '/todos' },
    });
    assertEquals(result, {
      data: [{ id: '1', title: 'Buy milk' }],
      pagination,
    });
  });

  await t.step('resolves to-one and to-many from included', () => {
    const { data } = hydrateResponse<Todo>({
      data: {
        id: '1',
        type: 'todos',
        attributes: { title: 'Buy milk' },
        relationships: {
          owner: { data: { id: '10', type: 'users' } },
          tags: {
            data: [{ id: '20', type: 'tags' }, { id: '21', type: 'tags' }],
          },
        },
      },
      included: [
        alice,
        { id: '20', type: 'tags', attributes: { label: 'urgent' } },
        { id: '21', type: 'tags', attributes: { label: 'shopping' } },
      ],
    });
    assertEquals(data.owner, { id: '10', name: 'Alice' });
    assertEquals(data.tags, [
      { id: '20', label: 'urgent' },
      { id: '21', label: 'shopping' },
    ]);
  });

  await t.step('linked but not included: to-one null, to-many dropped', () => {
    const { data } = hydrateResponse<Todo>({
      data: {
        id: '1',
        type: 'todos',
        attributes: { title: 'Buy milk' },
        relationships: {
          owner: { data: { id: '99', type: 'users' } },
          tags: {
            data: [{ id: '20', type: 'tags' }, { id: '99', type: 'tags' }],
          },
        },
      },
      included: [{ id: '20', type: 'tags', attributes: { label: 'urgent' } }],
    });
    assertEquals(data.owner, null);
    assertEquals(data.tags, [{ id: '20', label: 'urgent' }]);
  });

  await t.step('empty to-one relationship is null', () => {
    const { data } = hydrateResponse<Todo>({
      data: {
        id: '1',
        type: 'todos',
        attributes: { title: 'Buy milk' },
        relationships: { owner: { data: null } },
      },
    });
    assertEquals(data.owner, null);
  });

  await t.step('descriptor fills what the wire omits', () => {
    type Article = {
      id: string;
      title: string;
      body: string | null;
      author: User | null;
      comments: { id: string }[];
    };
    const descriptors = {
      articles: {
        type: 'articles',
        attributes: ['title', 'body'],
        toOne: ['author'],
        toMany: ['comments'],
      } satisfies JsonApiResourceDescriptor<Article>,
    };
    const { data } = hydrateResponse<Article>(
      // body is null-stripped, author and comments are not included
      { data: { id: '1', type: 'articles', attributes: { title: 'A' } } },
      descriptors,
    );
    assertEquals(data, {
      id: '1',
      title: 'A',
      body: null,
      author: null,
      comments: [],
    });
  });

  await t.step('resolves nested includes, cycles stop at null', () => {
    const { data } = hydrateResponse<User>({
      data: {
        id: '1',
        type: 'users',
        attributes: { name: 'Zed' },
        relationships: { friend: { data: { id: '10', type: 'users' } } },
      },
      included: [
        {
          ...alice,
          relationships: { friend: { data: { id: '1', type: 'users' } } },
        },
      ],
    });
    assertEquals(data.friend?.name, 'Alice');
    assertEquals(data.friend?.friend, null);
  });
});

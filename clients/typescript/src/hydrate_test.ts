// deno-lint-ignore-file no-explicit-any
import { assertEquals } from '@std/assert';
import { hydrateResponse } from './hydrate.ts';
import type {
  JsonApiArrayResponse,
  JsonApiSingleResponse,
} from './types/jsonapi.ts';

Deno.test('hydrateResponse', async (t) => {
  await t.step('hydrates array response with flattened attributes', () => {
    const response: JsonApiArrayResponse = {
      data: [
        {
          id: '1',
          type: 'todos',
          attributes: { title: 'Buy milk', completed: false },
        },
        {
          id: '2',
          type: 'todos',
          attributes: { title: 'Walk dog', completed: true },
        },
      ],
    };

    const result = hydrateResponse(response);

    assertEquals(result.data.length, 2);
    assertEquals(result.data[0], {
      id: '1',
      type: 'todos',
      title: 'Buy milk',
      completed: false,
    });
    assertEquals(result.data[1], {
      id: '2',
      type: 'todos',
      title: 'Walk dog',
      completed: true,
    });
  });

  await t.step('hydrates single resource response', () => {
    const response: JsonApiSingleResponse = {
      data: {
        id: '1',
        type: 'todos',
        attributes: { title: 'Buy milk', completed: false },
      },
    };

    const result = hydrateResponse(response);

    assertEquals(result.data, {
      id: '1',
      type: 'todos',
      title: 'Buy milk',
      completed: false,
    });
  });

  await t.step('resolves to-one relationship from included', () => {
    const response: JsonApiSingleResponse = {
      data: {
        id: '1',
        type: 'todos',
        attributes: { title: 'Buy milk' },
        relationships: {
          owner: { data: { id: '10', type: 'users' } },
        },
      },
      included: [
        {
          id: '10',
          type: 'users',
          attributes: { name: 'Alice', email: 'alice@example.com' },
        },
      ],
    };

    const result = hydrateResponse(response);

    assertEquals(result.data, {
      id: '1',
      type: 'todos',
      title: 'Buy milk',
      owner: {
        id: '10',
        type: 'users',
        name: 'Alice',
        email: 'alice@example.com',
      },
    });
  });

  await t.step('resolves to-many relationship from included', () => {
    const response: JsonApiSingleResponse = {
      data: {
        id: '1',
        type: 'todos',
        attributes: { title: 'Buy milk' },
        relationships: {
          tags: {
            data: [
              { id: '20', type: 'tags' },
              { id: '21', type: 'tags' },
            ],
          },
        },
      },
      included: [
        { id: '20', type: 'tags', attributes: { label: 'urgent' } },
        { id: '21', type: 'tags', attributes: { label: 'shopping' } },
      ],
    };

    const result = hydrateResponse(response);

    assertEquals(result.data, {
      id: '1',
      type: 'todos',
      title: 'Buy milk',
      tags: [
        { id: '20', type: 'tags', label: 'urgent' },
        { id: '21', type: 'tags', label: 'shopping' },
      ],
    });
  });

  await t.step('handles circular references without infinite loop', () => {
    const response: JsonApiSingleResponse = {
      data: {
        id: '1',
        type: 'users',
        attributes: { name: 'Alice' },
        relationships: {
          friend: { data: { id: '2', type: 'users' } },
        },
      },
      included: [
        {
          id: '2',
          type: 'users',
          attributes: { name: 'Bob' },
          relationships: {
            friend: { data: { id: '1', type: 'users' } },
          },
        },
        {
          id: '1',
          type: 'users',
          attributes: { name: 'Alice' },
          relationships: {
            friend: { data: { id: '2', type: 'users' } },
          },
        },
      ],
    };

    const result = hydrateResponse(response);

    // Alice -> Bob resolves, Bob -> Alice hits circular guard
    const data = result.data as any;
    assertEquals(data.id, '1');
    assertEquals(data.name, 'Alice');
    assertEquals(data.friend.id, '2');
    assertEquals(data.friend.name, 'Bob');
    assertEquals(data.friend.friend, {
      id: '1',
      type: 'users',
      circular: true,
    });
  });

  await t.step('returns null for null input', () => {
    const result = hydrateResponse(null);
    assertEquals(result, null);
  });

  await t.step('returns null for undefined input', () => {
    const result = hydrateResponse(undefined);
    assertEquals(result, null);
  });

  await t.step('preserves null data in single resource response', () => {
    const response = { data: null } as any;
    const result = hydrateResponse(response);

    assertEquals(result.data, null);
  });

  await t.step(
    'resolves missing to-one relationship to null',
    () => {
      const response: JsonApiSingleResponse = {
        data: {
          id: '1',
          type: 'todos',
          attributes: { title: 'Buy milk' },
          relationships: {
            owner: { data: { id: '99', type: 'users' } },
          },
        },
        included: [],
      };

      const result = hydrateResponse(response);

      assertEquals((result.data as any).owner, null);
    },
  );

  await t.step(
    'filters out missing to-many relationship entries',
    () => {
      const response: JsonApiSingleResponse = {
        data: {
          id: '1',
          type: 'todos',
          attributes: { title: 'Buy milk' },
          relationships: {
            tags: {
              data: [
                { id: '20', type: 'tags' },
                { id: '99', type: 'tags' },
              ],
            },
          },
        },
        included: [
          { id: '20', type: 'tags', attributes: { label: 'urgent' } },
        ],
      };

      const result = hydrateResponse(response);

      assertEquals((result.data as any).tags.length, 1);
      assertEquals((result.data as any).tags[0].label, 'urgent');
    },
  );

  await t.step('resolves deep nested relationships', () => {
    const response: JsonApiSingleResponse = {
      data: {
        id: '1',
        type: 'todos',
        attributes: { title: 'Buy milk' },
        relationships: {
          owner: { data: { id: '10', type: 'users' } },
        },
      },
      included: [
        {
          id: '10',
          type: 'users',
          attributes: { name: 'Alice' },
          relationships: {
            department: { data: { id: '100', type: 'departments' } },
          },
        },
        {
          id: '100',
          type: 'departments',
          attributes: { name: 'Engineering' },
        },
      ],
    };

    const result = hydrateResponse(response);

    assertEquals((result.data as any).owner.name, 'Alice');
    assertEquals((result.data as any).owner.department, {
      id: '100',
      type: 'departments',
      name: 'Engineering',
    });
  });

  await t.step('handles empty included array', () => {
    const response: JsonApiSingleResponse = {
      data: {
        id: '1',
        type: 'todos',
        attributes: { title: 'Buy milk' },
        relationships: {
          owner: { data: { id: '10', type: 'users' } },
        },
      },
      included: [],
    };

    const result = hydrateResponse(response);

    assertEquals((result.data as any).owner, null);
  });

  await t.step('propagates meta and links', () => {
    const response: JsonApiArrayResponse = {
      data: [
        {
          id: '1',
          type: 'todos',
          attributes: { title: 'Buy milk' },
        },
      ],
      meta: {
        pagination: {
          totalResources: 100,
          totalPages: 10,
          currentPage: 1,
          pageSize: 10,
        },
      },
      links: {
        self: '/api/todos?page[number]=1',
        next: '/api/todos?page[number]=2',
        last: '/api/todos?page[number]=10',
      },
    };

    const result = hydrateResponse(response);

    assertEquals(result.meta, {
      pagination: {
        totalResources: 100,
        totalPages: 10,
        currentPage: 1,
        pageSize: 10,
      },
    });
    assertEquals(result.links, {
      self: '/api/todos?page[number]=1',
      next: '/api/todos?page[number]=2',
      last: '/api/todos?page[number]=10',
    });
  });

  await t.step('handles undefined data as empty array', () => {
    const response = {} as any;
    const result = hydrateResponse(response);

    assertEquals(result.data, []);
  });

  await t.step('resolves null to-one relationship data', () => {
    const response: JsonApiSingleResponse = {
      data: {
        id: '1',
        type: 'todos',
        attributes: { title: 'Buy milk' },
        relationships: {
          owner: { data: null },
        },
      },
    };

    const result = hydrateResponse(response);

    assertEquals((result.data as any).owner, null);
  });
});

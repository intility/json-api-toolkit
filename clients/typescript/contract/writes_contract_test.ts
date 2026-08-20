/**
 * Write contract: the backend has no JSON:API request deserializer.
 * Bodies are plain camelCase DTOs, but the Content-Type must still be
 * application/vnd.api+json. Responses come back as JSON:API documents.
 *
 * Runs last (alphabetically) and cleans up after itself so the read
 * contract keeps seeing the pristine seed.
 */
import { assert, assertEquals } from '@std/assert';
import { getDoc, request } from './helpers.ts';

Deno.test('write path', async (t) => {
  let createdId = '';

  await t.step(
    'POST plain DTO returns 201 + Location + JSON:API document',
    async () => {
      const { doc, status, headers } = await request('POST', 'articles', {
        body: {
          title: 'Contract test article',
          body: 'Written by the contract suite',
          published: true,
          publishedAt: '2025-03-01T09:00:00Z',
          authorId: 2,
          tags: ['contract'],
        },
      });
      assertEquals(status, 201);
      createdId = doc.data.id;
      assert(headers.get('location')?.endsWith(`/articles/${createdId}`));
      assertEquals(doc.data.type, 'articles');
      assertEquals(doc.data.attributes.title, 'Contract test article');
      assertEquals(doc.data.attributes.tags, ['contract']);
      assertEquals(doc.data.attributes.viewCount, 0);
      assertEquals(doc.data.attributes.authorId, 2);
    },
  );

  await t.step(
    'POST without required field is 400 REQUIRED_FIELD_MISSING',
    async () => {
      const { doc, status } = await request('POST', 'articles', {
        body: { body: 'no title here' },
      });
      assertEquals(status, 400);
      assertEquals(doc.errors[0].code, 'REQUIRED_FIELD_MISSING');
      assertEquals(doc.errors[0].source, { pointer: '/data/attributes/title' });
      assertEquals(doc.errors[0].meta, { field: 'title' });
    },
  );

  await t.step(
    'PATCH applies partial updates, other fields untouched',
    async () => {
      const { doc, status } = await request('PATCH', `articles/${createdId}`, {
        body: { viewCount: 5 },
      });
      assertEquals(status, 200);
      assertEquals(doc.data.attributes.viewCount, 5);
      assertEquals(doc.data.attributes.title, 'Contract test article');
      assertEquals(doc.data.attributes.published, true);
    },
  );

  await t.step(
    'PATCH on a missing resource is 404 RESOURCE_NOT_FOUND',
    async () => {
      const { doc, status } = await request('PATCH', 'articles/999999', {
        body: { title: 'nope' },
      });
      assertEquals(status, 404);
      assertEquals(doc.errors[0].code, 'RESOURCE_NOT_FOUND');
    },
  );

  await t.step('DELETE returns 204 with an empty body', async () => {
    const { doc, status } = await request('DELETE', `articles/${createdId}`);
    assertEquals(status, 204);
    assertEquals(doc, null);
    const { status: after } = await getDoc(`articles/${createdId}`);
    assertEquals(after, 404);
  });
});

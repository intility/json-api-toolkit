/**
 * Error contract: status codes, error document shapes, and which error
 * responses carry machine-readable codes (not all of them do).
 */
import { assert, assertEquals, assertFalse } from '@std/assert';
import { isJsonApiErrorResponse } from '../src/index.ts';
import { getDoc, request } from './helpers.ts';

Deno.test('not found', async (t) => {
  await t.step('WART: plain GET 404 has no error code', async () => {
    const { doc, status } = await getDoc('articles/999');
    assertEquals(status, 404);
    assertEquals(doc.errors, [
      { status: '404', title: 'Not Found', detail: 'Resource not found' },
    ]);
    assert(isJsonApiErrorResponse(doc));
  });

  await t.step(
    'explicit JsonApiErrors.NotFound carries RESOURCE_NOT_FOUND and meta',
    async () => {
      const { doc, status } = await request('DELETE', 'articles/999');
      assertEquals(status, 404);
      assertEquals(doc.errors[0].code, 'RESOURCE_NOT_FOUND');
      assertEquals(doc.errors[0].meta, { resourceType: 'articles', id: 999 });
    },
  );
});

Deno.test('include allowlisting', async (t) => {
  await t.step(
    'unlisted include is 403 INCLUDE_NOT_ALLOWED with meta',
    async () => {
      const { doc, status } = await getDoc('articles/3?include=bogus');
      assertEquals(status, 403);
      assertEquals(doc.errors[0].code, 'INCLUDE_NOT_ALLOWED');
      assertEquals(doc.errors[0].meta, {
        requestedIncludes: ['bogus'],
        forbiddenIncludes: ['bogus'],
        allowedIncludes: ['author', 'comments', 'comments.author'],
      });
    },
  );

  await t.step('empty [AllowedIncludes] forbids every include', async () => {
    const { doc, status } = await getDoc('authors?include=articles');
    assertEquals(status, 403);
    assertEquals(doc.errors[0].code, 'INCLUDE_NOT_ALLOWED');
    assertEquals(doc.errors[0].meta.allowedIncludes, []);
  });

  await t.step(
    'WART: un-indexed group syntax on an allowlisted action is 403',
    async () => {
      // filter[or][field][op] parses as an include-filter on a relationship
      // named "or", which a non-empty allowlist then rejects
      const { doc, status } = await getDoc(
        'articles/3?filter%5Bor%5D%5BviewCount%5D%5Bgt%5D=230',
      );
      assertEquals(status, 403);
      assertEquals(doc.errors[0].code, 'FILTER_NOT_ALLOWED');
      assertEquals(doc.errors[0].meta.forbiddenFilterPaths, ['or']);
    },
  );

  await t.step(
    'indexed (builder-emitted) groups pass the allowlist and apply',
    async () => {
      const { doc, status } = await getDoc(
        'authors?filter%5Bor%5D%5B0%5D%5Bname%5D=x',
      );
      assertEquals(status, 200);
      assertEquals(doc.data, []);
    },
  );
});

Deno.test('content negotiation', async (t) => {
  await t.step(
    'wrong request content type is 415 with an empty body',
    async () => {
      const { doc, status } = await request('POST', 'articles', {
        body: { title: 'x' },
        contentType: 'application/json',
      });
      assertEquals(status, 415);
      assertEquals(doc, null); // NOT a JSON:API error document
    },
  );

  await t.step('responses are served as application/vnd.api+json', async () => {
    const { headers } = await getDoc('articles/1');
    assertEquals(
      headers.get('content-type'),
      'application/vnd.api+json; charset=utf-8',
    );
    // error responses too
    const { headers: errHeaders, status } = await getDoc('articles/999');
    assertEquals(status, 404);
    assertEquals(
      errHeaders.get('content-type'),
      'application/vnd.api+json; charset=utf-8',
    );
  });
});

Deno.test('server errors', async (t) => {
  await t.step(
    'WART: unconvertible filter values are 500, not 400',
    async () => {
      const { doc, status } = await getDoc(
        'articles?filter%5BpublishedAt%5D=isnull',
      );
      assertEquals(status, 500);
      assertEquals(doc.errors[0].status, '500');
      assertEquals(doc.errors[0].title, 'Internal Server Error');
      assertFalse('code' in doc.errors[0]);
    },
  );
});

/**
 * Core client contract: a resource handle's reads and writes against a real
 * toolkit-backed API, end to end (query building -> fetch -> hydration ->
 * error parsing).
 */
import { assert, assertEquals, assertRejects } from '@std/assert';
import { createJsonApiClient, JsonApiRequestError } from '../src/index.ts';
import {
  BASE_URL,
  type ContractArticle,
  PUBLISHED_ARTICLES,
  TOTAL_ARTICLES,
} from './helpers.ts';

const articles = createJsonApiClient({ baseUrl: BASE_URL })
  .resource<ContractArticle>('articles');

Deno.test('list', async (t) => {
  await t.step('hydrates data and returns pagination', async () => {
    const { data, pagination } = await articles.list(
      (q) => q.filter('published', true).page(1, 5),
    );
    assertEquals(data.length, 5);
    assertEquals(pagination?.totalResources, PUBLISHED_ARTICLES);
    assertEquals(data[0].title, 'Article 01');
  });

  await t.step('accepts a pre-built query string for interop', async () => {
    const { data, pagination } = await articles.list({
      params: 'page%5Bsize%5D=1',
    });
    assertEquals(data.length, 1);
    assertEquals(pagination?.totalResources, TOTAL_ARTICLES);
  });

  await t.step('unpaginated request has no pagination', async () => {
    const { data, pagination } = await articles.list();
    assertEquals(data.length, TOTAL_ARTICLES);
    assertEquals(pagination, undefined);
  });
});

Deno.test('get', async (t) => {
  await t.step('hydrates an included relationship', async () => {
    const article = await articles.get(1, (q) => q.include('author'));
    assertEquals(article.id, '1');
    assertEquals(article.author.name, 'Astrid Berg');
  });

  await t.step('throws JsonApiRequestError with the status', async () => {
    const error = await assertRejects(
      () => articles.get(999999),
      JsonApiRequestError,
    );
    assertEquals(error.status, 404);
  });
});

Deno.test('writes', async (t) => {
  await t.step('create, update, remove round trip', async () => {
    const created = await articles.create({
      title: 'Client contract test article',
      published: false,
      authorId: 2,
      tags: ['contract-client'],
    });
    assertEquals(created.title, 'Client contract test article');
    assert(created.id);

    const updated = await articles.update(created.id, { viewCount: 7 });
    assertEquals(updated.viewCount, 7);
    assertEquals(updated.title, 'Client contract test article');

    await articles.remove(created.id);
    await assertRejects(() => articles.get(created.id), JsonApiRequestError);
  });

  await t.step(
    'validation failure surfaces via hasCode and fieldErrors',
    async () => {
      const error = await assertRejects(
        () => articles.create({ body: 'no title' }),
        JsonApiRequestError,
      );
      assertEquals(error.hasCode('REQUIRED_FIELD_MISSING'), true);
      assertEquals(Object.keys(error.fieldErrors()), ['title']);
    },
  );
});

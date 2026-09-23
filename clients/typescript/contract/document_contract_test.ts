/**
 * Document shape and hydration contract: resource objects, includes,
 * sparse fieldsets, null-stripping.
 */
import { assert, assertEquals, assertFalse } from '@std/assert';
import { hydrateResponse, JsonApiQueryBuilder } from '../src/index.ts';
import {
  type ContractArticle,
  getDoc,
  type List,
  type Single,
} from './helpers.ts';
import { Article } from '../../../samples/ContractApi/api-types.gen.ts';

Deno.test('document shape', async (t) => {
  await t.step('primary resources use the controller type string', async () => {
    const { status, doc } = await getDoc<Single>('articles/3');
    assertEquals(status, 200);
    assertEquals(doc.data.type, 'articles');
    assertEquals(doc.data.id, '3'); // ids are strings on the wire
    assertEquals(doc.data.links?.self?.endsWith('/articles/3'), true);
  });

  await t.step(
    'attributes: camelCase, ISO dates, FK ids leak as attributes',
    async () => {
      const { doc } = await getDoc<Single>('articles/3');
      assertEquals(doc.data.attributes.title, 'Article 03');
      assertEquals(doc.data.attributes.publishedAt, '2025-01-03T12:00:00Z');
      assertEquals(doc.data.attributes.viewCount, 30);
      // The CLR foreign key property is serialized as a plain attribute
      assertEquals(doc.data.attributes.authorId, 3);
    },
  );

  await t.step(
    'primitive collections are attributes (JSON column detection)',
    async () => {
      const { doc } = await getDoc<Single>('articles/3');
      assertEquals(doc.data.attributes.tags, ['tech', 'news']);
      // empty collections serialize as [], not stripped
      const { doc: doc23 } = await getDoc<Single>('articles/23');
      assertEquals(doc23.data.attributes.tags, []);
    },
  );

  await t.step('null attributes are stripped from responses', async () => {
    // article 25: body is null; publishedAt is set (odd id)
    const { doc } = await getDoc<Single>('articles/25');
    assertFalse('body' in doc.data.attributes);
    // article 2: publishedAt is null (even id)
    const { doc: doc2 } = await getDoc<Single>('articles/2');
    assertFalse('publishedAt' in doc2.data.attributes);
    assertEquals(doc2.data.attributes.published, false);
    // authors/2 has null email
    const { doc: author } = await getDoc<Single>('authors/2');
    assertEquals(author.data.attributes, { name: 'Bjarne Moen' });
  });

  await t.step(
    'included resources use the camelCased CLR class name, not the controller type',
    async () => {
      const { doc } = await getDoc<Single>('articles/3?include=author');
      assertEquals(doc.data.type, 'articles');
      assertEquals(doc.data.relationships?.author.data, {
        id: '3',
        type: 'author',
      });
      assertEquals(doc.included?.length, 1);
      assertEquals(doc.included?.[0].type, 'author'); // singular CLR name
      assertEquals(doc.included?.[0].attributes.name, 'Carmen Diaz');
    },
  );

  await t.step(
    'nested include drops ALL relationship linkage from primary data',
    async () => {
      // include=comments.author: included is populated, but data has no
      // relationships object at all, so the linkage is unrecoverable.
      const { doc } = await getDoc<Single>(
        'articles/3?include=comments.author',
      );
      assertFalse('relationships' in doc.data);
      const includedTypes = doc.included?.map((r) => r.type)
        .sort();
      assertEquals(includedTypes, ['author', 'author', 'comment', 'comment']);
      // included comments DO carry their own relationships
      const comment = doc.included?.find((r) => r.type === 'comment');
      const authorRef = comment?.relationships?.author.data;
      assert(
        authorRef && !Array.isArray(authorRef) && authorRef.type === 'author',
      );
      // same on collections
      const { doc: list } = await getDoc<List>(
        'articles?include=comments.author&page%5Bsize%5D=2',
      );
      assertFalse('relationships' in list.data[0]);
    },
  );
});

Deno.test('sparse fieldsets', async (t) => {
  await t.step(
    'fields[type] works for the primary resource via the builder',
    async () => {
      const qs = new JsonApiQueryBuilder<ContractArticle>()
        .fields(Article, ['title', 'publishedAt'])
        .page(1, 2)
        .build();
      const { doc } = await getDoc<List>(`articles?${qs}`);
      assertEquals(Object.keys(doc.data[0].attributes), [
        'title',
        'publishedAt',
      ]);
    },
  );

  await t.step(
    'included resources need the CLR-derived type name, not the wire type',
    async () => {
      // fields[author] (CLR name) trims the included author...
      const { doc } = await getDoc<Single>(
        'articles/3?include=author&fields%5Bauthor%5D=name',
      );
      assertEquals(Object.keys(doc.included?.[0].attributes ?? {}), ['name']);
      // ...fields[authors] (what a JSON:API client would guess) does nothing
      const { doc: doc2 } = await getDoc<Single>(
        'articles/3?include=author&fields%5Bauthors%5D=name',
      );
      assert(Object.keys(doc2.included?.[0].attributes ?? {}).length > 1);
    },
  );

  await t.step(
    'id and type are always present regardless of fieldset',
    async () => {
      const { doc } = await getDoc<List>(
        'articles?fields%5Barticles%5D=title&page%5Bsize%5D=1',
      );
      assertEquals(doc.data[0].id, '1');
      assertEquals(doc.data[0].type, 'articles');
    },
  );
});

Deno.test('hydration', async (t) => {
  await t.step(
    'single-level include hydrates to a flat nested object',
    async () => {
      const { doc } = await getDoc<Single>('articles/3?include=author');
      const { data } = hydrateResponse<ContractArticle>(doc);
      assertEquals(data.title, 'Article 03');
      assertEquals(data.author.name, 'Carmen Diaz');
      assertFalse('type' in data);
      // WART: un-included relationships are absent from the wire entirely,
      // so the hydrator cannot emit [] or null for them without a resource
      // descriptor (arity is not on the wire)
      assertEquals(data.comments, undefined);
    },
  );

  await t.step(
    'nested include hydrates to NOTHING because linkage is missing',
    async () => {
      const { doc } = await getDoc<Single>(
        'articles/3?include=comments.author',
      );
      const { data } = hydrateResponse<ContractArticle>(doc);
      // included has 4 resources, but without data.relationships the
      // hydrator cannot attach any of them
      assertEquals(data.comments, undefined);
      assertEquals(data.author, undefined);
    },
  );

  await t.step('collection hydration returns data and pagination', async () => {
    const { doc } = await getDoc<List>(
      'articles?include=author&page%5Bsize%5D=2',
    );
    const { data, pagination } = hydrateResponse<ContractArticle>(doc);
    assertEquals(data.length, 2);
    assertEquals(data[0].author.name, 'Astrid Berg');
    assertEquals(pagination?.totalResources, 25);
  });
});

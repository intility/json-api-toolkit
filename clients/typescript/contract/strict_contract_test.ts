/**
 * Strict query validation contract (JsonApiOptions.StrictQueryValidation).
 * Every silently-dropped query shape from query_contract_test.ts becomes a
 * 400 with a descriptive code on the strict instance.
 */
import { assertEquals } from '@std/assert';
import { JsonApiQueryBuilder } from '../src/index.ts';
import {
  type ContractArticle,
  getDoc,
  STRICT_BASE_URL,
  total,
} from './helpers.ts';

function strictGet(qs: string) {
  return getDoc(`articles?${qs}`, STRICT_BASE_URL);
}

Deno.test('strict query validation: rejected shapes', async (t) => {
  await t.step('and group is 400 UNSUPPORTED_FILTER_GROUP', async () => {
    const qs = new JsonApiQueryBuilder<ContractArticle>()
      .and((b) => b.filter('published', 'eq', false))
      .build();
    const { doc, status } = await strictGet(qs);
    assertEquals(status, 400);
    assertEquals(doc.errors[0].code, 'UNSUPPORTED_FILTER_GROUP');
  });

  await t.step('nested group is 400 UNSUPPORTED_FILTER_GROUP', async () => {
    const qs = new JsonApiQueryBuilder<ContractArticle>()
      .or((b) =>
        b.filter('title', 'eq', 'Article 01').and((bb) =>
          bb.filter('published', 'eq', true)
        )
      )
      .build();
    const { doc, status } = await strictGet(qs);
    assertEquals(status, 400);
    assertEquals(doc.errors[0].code, 'UNSUPPORTED_FILTER_GROUP');
  });

  await t.step('unknown filter field is 400 INVALID_FILTER_FIELD', async () => {
    const { doc, status } = await strictGet('filter%5Bbogus%5D=x');
    assertEquals(status, 400);
    assertEquals(doc.errors[0].code, 'INVALID_FILTER_FIELD');
  });

  await t.step(
    '2-arg isnull wart is 400 INVALID_FILTER_VALUE, not 500',
    async () => {
      const qs = new JsonApiQueryBuilder<ContractArticle>()
        .filter('publishedAt', 'isnull')
        .build();
      const { doc, status } = await strictGet(qs);
      assertEquals(status, 400);
      assertEquals(doc.errors[0].code, 'INVALID_FILTER_VALUE');
    },
  );

  await t.step(
    'Date value wart is 400 INVALID_FILTER_VALUE, not 500',
    async () => {
      const qs = new JsonApiQueryBuilder<ContractArticle>()
        .filter('publishedAt', 'gt', new Date('2025-01-20T00:00:00Z'))
        .build();
      const { doc, status } = await strictGet(qs);
      assertEquals(status, 400);
      assertEquals(doc.errors[0].code, 'INVALID_FILTER_VALUE');
    },
  );

  await t.step('unknown operator is 400 INVALID_FILTER_OPERATOR', async () => {
    const { doc, status } = await strictGet(
      'filter%5Btitle%5D%5Bcontains%5D=x',
    );
    assertEquals(status, 400);
    assertEquals(doc.errors[0].code, 'INVALID_FILTER_OPERATOR');
  });

  await t.step('unknown sort field is 400 INVALID_SORT_FIELD', async () => {
    const { doc, status } = await strictGet('sort=bogus');
    assertEquals(status, 400);
    assertEquals(doc.errors[0].code, 'INVALID_SORT_FIELD');
  });

  await t.step('dot-path sort is 400 INVALID_SORT_FIELD', async () => {
    const qs = new JsonApiQueryBuilder<ContractArticle>()
      .sort('author.name')
      .build();
    const { doc, status } = await strictGet(qs);
    assertEquals(status, 400);
    assertEquals(doc.errors[0].code, 'INVALID_SORT_FIELD');
  });

  await t.step(
    'bracket include-filter without include is 400 FILTER_NOT_ALLOWED',
    async () => {
      const { doc, status } = await strictGet(
        'filter%5Bauthor%5D%5Bname%5D%5Blike%5D=Astrid',
      );
      assertEquals(status, 400);
      assertEquals(doc.errors[0].code, 'FILTER_NOT_ALLOWED');
    },
  );
});

Deno.test('strict query validation: valid queries unaffected', async (t) => {
  await t.step('or group, isnull, sort and include still work', async () => {
    const qs = new JsonApiQueryBuilder<ContractArticle>()
      .or((b) =>
        b.filter('viewCount', 'gt', 230).filter('title', 'like', 'Article 01')
      )
      .sort('-viewCount')
      .include('author')
      .page(1, 10)
      .build();
    const { doc, status } = await strictGet(qs);
    assertEquals(status, 200);
    assertEquals(total(doc), 3);
    assertEquals(doc.data[0].id, '25');

    const isnull = new JsonApiQueryBuilder<ContractArticle>()
      .filter('publishedAt', 'isnull', true)
      .page(1, 1)
      .build();
    const { doc: nulls } = await strictGet(isnull);
    assertEquals(total(nulls), 12);
  });
});

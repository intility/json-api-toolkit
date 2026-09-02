/**
 * Filter and sort contract: what the current query builder emits and what
 * the backend actually does with it, including the silent drops the v1
 * redesign exists to eliminate.
 */
import { assertEquals } from '@std/assert';
import { JsonApiQueryBuilder } from '../src/index.ts';
import {
  type ContractArticle,
  getDoc,
  type List,
  PUBLISHED_ARTICLES,
  total,
  TOTAL_ARTICLES,
  UNPUBLISHED_ARTICLES,
} from './helpers.ts';

function list(qb: JsonApiQueryBuilder<ContractArticle>) {
  return getDoc<List>(
    `articles?${qb.page(1, 1).fields('articles', ['title']).build()}`,
  );
}

Deno.test('simple filters', async (t) => {
  await t.step('2-arg filter is implicit eq', async () => {
    const { doc } = await list(
      new JsonApiQueryBuilder<ContractArticle>().filter('published', true),
    );
    assertEquals(total(doc), PUBLISHED_ARTICLES);
  });

  await t.step('like means contains, no wildcards needed', async () => {
    // matches "Article 10".."Article 19" but not "Article 01"
    const { doc } = await list(
      new JsonApiQueryBuilder<ContractArticle>().filter(
        'title',
        'like',
        'Article 1',
      ),
    );
    assertEquals(total(doc), 10);
  });

  await t.step('in joins array values with commas', async () => {
    const { doc } = await list(
      new JsonApiQueryBuilder<ContractArticle>().filter('viewCount', 'in', [
        10,
        30,
        50,
      ]),
    );
    assertEquals(total(doc), 3);
  });

  await t.step('gt on an ISO date string works', async () => {
    const { doc } = await list(
      new JsonApiQueryBuilder<ContractArticle>().filter(
        'publishedAt',
        'gt',
        '2025-01-20T00:00:00Z',
      ),
    );
    assertEquals(total(doc), 3); // articles 21, 23, 25
  });

  await t.step(
    'isnull requires the 3-arg form with a dummy value',
    async () => {
      const { doc } = await list(
        new JsonApiQueryBuilder<ContractArticle>().filter(
          'publishedAt',
          'isnull',
          true,
        ),
      );
      assertEquals(total(doc), UNPUBLISHED_ARTICLES);
    },
  );

  await t.step(
    'WART: 2-arg isnull serializes the operator as the value and blows up',
    async () => {
      // filter[publishedAt]=isnull -> unconvertible DateTime -> 500
      const qb = new JsonApiQueryBuilder<ContractArticle>().filter(
        'publishedAt',
        'isnull',
      );
      assertEquals(qb.build(), 'filter%5BpublishedAt%5D=isnull');
      const { status } = await getDoc(`articles?${qb.build()}`);
      assertEquals(status, 500);
    },
  );

  await t.step(
    'WART: Date values serialize via String(), not ISO, and blow up',
    async () => {
      const qb = new JsonApiQueryBuilder<ContractArticle>().filter(
        'publishedAt',
        'gt',
        new Date('2025-01-20T00:00:00Z'),
      );
      assertEquals(qb.build().includes('2025-01-20T00'), false);
      const { status } = await getDoc(`articles?${qb.build()}`);
      assertEquals(status, 500);
    },
  );

  await t.step('WART: unknown filter fields are silently ignored', async () => {
    const { doc, status } = await getDoc<List>(
      'articles?filter%5Bbogus%5D=x&page%5Bsize%5D=1',
    );
    assertEquals(status, 200);
    assertEquals(total(doc), TOTAL_ARTICLES);
  });
});

Deno.test('filter groups', async (t) => {
  await t.step('or group applies one level of OR', async () => {
    const { doc } = await list(
      new JsonApiQueryBuilder<ContractArticle>().or((b) =>
        b.filter('viewCount', 'gt', 230).filter('title', 'like', 'Article 01')
      ),
    );
    assertEquals(total(doc), 3); // 24, 25 by viewCount + 01 by title
  });

  await t.step('not group means NOT(a AND b)', async () => {
    const { doc } = await list(
      new JsonApiQueryBuilder<ContractArticle>().not((b) =>
        b.filter('published', 'eq', true).filter('viewCount', 'gt', 100)
      ),
    );
    // excludes published articles with viewCount > 100: 11,13,...,25 -> 8 articles
    assertEquals(total(doc), TOTAL_ARTICLES - 8);
  });

  await t.step(
    'WART: and group serializes to filter[and] and is silently dropped',
    async () => {
      const { doc, status } = await list(
        new JsonApiQueryBuilder<ContractArticle>().and((b) =>
          b.filter('published', 'eq', false)
        ),
      );
      assertEquals(status, 200);
      assertEquals(total(doc), TOTAL_ARTICLES); // filter had no effect
    },
  );

  await t.step(
    'WART: nested groups are silently dropped, siblings still apply',
    async () => {
      const { doc } = await list(
        new JsonApiQueryBuilder<ContractArticle>().or((b) =>
          b.filter('title', 'eq', 'Article 01').and((bb) =>
            bb.filter('published', 'eq', true)
          )
        ),
      );
      // nested and-branch vanishes; only title=Article 01 remains in the OR
      assertEquals(total(doc), 1);
    },
  );
});

Deno.test('dot-path filters', async (t) => {
  await t.step(
    'filter through a to-one relationship works with include',
    async () => {
      const qs = new JsonApiQueryBuilder<ContractArticle>()
        .filter('author.name', 'like', 'Astrid')
        .include('author')
        .page(1, 1)
        .build();
      const { doc } = await getDoc<List>(`articles?${qs}`);
      assertEquals(total(doc), 9); // seed: authors round-robin, Astrid owns 9
      assertEquals(doc.included?.[0].attributes.name, 'Astrid Berg');
    },
  );

  await t.step('dot-path filter also applies WITHOUT include', async () => {
    const { doc } = await list(
      new JsonApiQueryBuilder<ContractArticle>().filter(
        'author.name',
        'like',
        'Astrid',
      ),
    );
    assertEquals(total(doc), 9);
  });
});

Deno.test('sorting', async (t) => {
  await t.step('descending sort with - prefix', async () => {
    const { doc } = await list(
      new JsonApiQueryBuilder<ContractArticle>().sort('-viewCount'),
    );
    assertEquals(doc.data[0].id, '25');
  });

  await t.step('multi-field sort', async () => {
    const { doc } = await list(
      new JsonApiQueryBuilder<ContractArticle>().sort(
        'published',
        '-viewCount',
      ),
    );
    // unpublished (false) first, highest viewCount among them: article 24
    assertEquals(doc.data[0].id, '24');
  });

  await t.step(
    'WART: dot-path sort compiles in the builder but is silently ignored',
    async () => {
      const { doc, status } = await list(
        new JsonApiQueryBuilder<ContractArticle>().sort('author.name'),
      );
      assertEquals(status, 200);
      assertEquals(doc.data[0].id, '1'); // insertion order, sort had no effect
    },
  );
});

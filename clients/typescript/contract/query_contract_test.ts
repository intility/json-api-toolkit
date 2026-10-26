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
    'filterNull() emits filter[field][isnull]',
    async () => {
      const { doc } = await list(
        new JsonApiQueryBuilder<ContractArticle>().filterNull('publishedAt'),
      );
      assertEquals(total(doc), UNPUBLISHED_ARTICLES);
    },
  );

  await t.step(
    'WART: 2-arg isnull serializes the operator as the value and blows up',
    async () => {
      // filter[publishedAt]=isnull -> unconvertible DateTime -> 500.
      // TypeScript cannot reject "isnull" here without also rejecting every
      // other free-form string value passed to the 2-arg form; use
      // filterNull() instead.
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
    'a Date value serializes to ISO 8601 and works, same as an ISO string',
    async () => {
      const { doc } = await list(
        new JsonApiQueryBuilder<ContractArticle>().filter(
          'publishedAt',
          'gt',
          new Date('2025-01-20T00:00:00Z'),
        ),
      );
      assertEquals(total(doc), 3); // articles 21, 23, 25, same as the ISO-string test above
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
      // The builder can no longer construct filter[and] at all (removed,
      // see DESIGN.md); hand-build the wire shape to prove the backend
      // still silently drops it if some other caller sends it.
      const params = new URLSearchParams({
        'filter[and][0][published]': 'false',
        'page[size]': '1',
        'fields[articles]': 'title',
      });
      const { doc, status } = await getDoc<List>(`articles?${params}`);
      assertEquals(status, 200);
      assertEquals(total(doc), TOTAL_ARTICLES); // filter had no effect
    },
  );

  await t.step(
    'WART: nested groups are silently dropped, siblings still apply',
    async () => {
      // The builder can no longer construct nesting at all (compile error,
      // see DESIGN.md); hand-build the wire shape to prove the backend
      // still silently drops it if some other caller sends it.
      const params = new URLSearchParams({
        'filter[or][0][title]': 'Article 01',
        'filter[or][1][and][0][published]': 'true',
        'page[size]': '1',
        'fields[articles]': 'title',
      });
      const { doc } = await getDoc<List>(`articles?${params}`);
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

  await t.step(
    'deep dot-path filter through a to-many relationship (Any())',
    async () => {
      // "comments.author.name": articles.comments (to-many) .author (to-one)
      // .name. Two dots, only reachable through the type's escape hatch
      // (DeepAttributeKeys) since it goes through a to-many relationship.
      const { doc } = await list(
        new JsonApiQueryBuilder<ContractArticle>().filter(
          'comments.author.name',
          'like',
          'Astrid',
        ),
      );
      assertEquals(total(doc), 7); // seed: 7 articles have a comment by Astrid
    },
  );
});

Deno.test('included-relationship filters (filterIncluded)', async (t) => {
  await t.step(
    "trims a to-many relationship's included entries, primary resources unaffected",
    async () => {
      const qs = new JsonApiQueryBuilder<ContractArticle>()
        .filterIncluded('comments', 'text', 'like', 'Comment 1')
        .include('comments')
        .page(1, 3)
        .build();
      const { doc } = await getDoc<List>(`articles?${qs}`);
      assertEquals(total(doc), TOTAL_ARTICLES); // filter never touches `data`
      const texts = doc.included?.map((r) => r.attributes.text);
      assertEquals(texts?.length, 3); // one matching comment per article
      assertEquals(texts?.every((t) => String(t).includes('Comment 1')), true);
    },
  );

  await t.step(
    'WART: filterIncluded() on a to-one relationship has no effect',
    async () => {
      const qs = new JsonApiQueryBuilder<ContractArticle>()
        .filterIncluded('author', 'name', 'like', 'Astrid')
        .include('author')
        .page(1, 3)
        .build();
      const { doc } = await getDoc<List>(`articles?${qs}`);
      const names = new Set(doc.included?.map((r) => r.attributes.name));
      // every author still comes back, not just Astrid's; EF's filtered
      // include only applies to collection navigations, not to-one refs.
      assertEquals(names.size > 1, true);
    },
  );
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
    'WART: dot-path sort is silently ignored',
    async () => {
      // The builder can no longer construct this at all (sort() is
      // restricted to DirectAttributeKeys<T>, see DESIGN.md); hand-build
      // the wire shape to prove the backend still silently ignores it if
      // some other caller sends it.
      const params = new URLSearchParams({
        sort: 'author.name',
        'page[size]': '1',
        'fields[articles]': 'title',
      });
      const { doc, status } = await getDoc<List>(`articles?${params}`);
      assertEquals(status, 200);
      assertEquals(doc.data[0].id, '1'); // insertion order, sort had no effect
    },
  );
});

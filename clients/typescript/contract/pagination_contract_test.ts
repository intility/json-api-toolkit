/**
 * Pagination contract: clamping in default mode, hard errors in strict
 * mode, and the broken pagination links that must never be followed.
 */
import { assert, assertEquals, assertFalse } from '@std/assert';
import {
  BASE_URL,
  getDoc,
  STRICT_BASE_URL,
  total,
  TOTAL_ARTICLES,
} from './helpers.ts';

Deno.test('default pagination', async (t) => {
  await t.step(
    'WART: no page params returns the entire collection, unpaginated',
    async () => {
      const { doc } = await getDoc('articles');
      assertEquals(doc.data.length, TOTAL_ARTICLES);
      assertEquals(doc.meta, undefined); // no pagination meta at all
      assertEquals(Object.keys(doc.links), ['self']); // no first/last/next
    },
  );

  await t.step(
    'any page param triggers pagination with DefaultPageSize 10',
    async () => {
      const { doc } = await getDoc('articles?page%5Bnumber%5D=1');
      assertEquals(doc.data.length, 10);
      assertEquals(doc.meta.pagination, {
        totalResources: TOTAL_ARTICLES,
        totalPages: 3,
        currentPage: 1,
        pageSize: 10,
      });
    },
  );

  await t.step('WART: page 0 and negative pages clamp to page 1', async () => {
    const { doc } = await getDoc('articles?page%5Bnumber%5D=0');
    assertEquals(doc.meta.pagination.currentPage, 1);
    const { doc: neg } = await getDoc('articles?page%5Bnumber%5D=-5');
    assertEquals(neg.meta.pagination.currentPage, 1);
  });

  await t.step(
    'WART: overflowing page numbers clamp to the last page',
    async () => {
      const { doc, status } = await getDoc('articles?page%5Bnumber%5D=999');
      assertEquals(status, 200);
      assertEquals(doc.meta.pagination.currentPage, 3);
      assertEquals(doc.data[0].id, '21');
    },
  );

  await t.step(
    'WART: oversized page size clamps to MaxPageSize (100)',
    async () => {
      const { doc } = await getDoc('articles?page%5Bsize%5D=1000');
      assertEquals(doc.meta.pagination.pageSize, 100);
    },
  );
});

Deno.test('pagination links', async (t) => {
  await t.step('self link preserves the full query string', async () => {
    const path = 'articles?filter%5Bpublished%5D=true&page%5Bsize%5D=2';
    const { doc } = await getDoc(path);
    assertEquals(doc.links.self, `${BASE_URL}/${path}`);
  });

  await t.step(
    'WART: first/last/prev/next drop filter, sort, include and fields',
    async () => {
      const { doc } = await getDoc(
        'articles?filter%5Bpublished%5D=true&sort=-viewCount&page%5Bsize%5D=2',
      );
      // links are rebuilt from the bare path with unencoded brackets
      assertEquals(
        doc.links.next,
        `${BASE_URL}/articles?page[number]=2&page[size]=2`,
      );
      assertEquals(
        doc.links.last,
        `${BASE_URL}/articles?page[number]=7&page[size]=2`,
      );
      assertFalse(doc.links.next.includes('filter'));

      // following next therefore returns UNFILTERED, UNSORTED data
      const next = new URL(doc.links.next);
      const { doc: page2 } = await getDoc(`articles${next.search}`);
      assertEquals(total(page2), TOTAL_ARTICLES);
    },
  );
});

Deno.test('strict pagination (StrictPagination = true)', async (t) => {
  await t.step('valid pages behave like default mode', async () => {
    const { doc, status } = await getDoc(
      'articles?page%5Bnumber%5D=2',
      STRICT_BASE_URL,
    );
    assertEquals(status, 200);
    assertEquals(doc.meta.pagination.currentPage, 2);
  });

  await t.step(
    'overflowing page number is 404 INVALID_PAGE_NUMBER',
    async () => {
      const { doc, status } = await getDoc(
        'articles?page%5Bnumber%5D=999',
        STRICT_BASE_URL,
      );
      assertEquals(status, 404);
      assertEquals(doc.errors[0].code, 'INVALID_PAGE_NUMBER');
      assertEquals(doc.errors[0].source, { parameter: 'page[number]' });
      assertEquals(doc.errors[0].meta, {
        value: 999,
        totalPages: 3,
        totalResources: TOTAL_ARTICLES,
      });
    },
  );

  await t.step('page 0 is 400 INVALID_PAGE_NUMBER (not clamped)', async () => {
    const { doc, status } = await getDoc(
      'articles?page%5Bnumber%5D=0',
      STRICT_BASE_URL,
    );
    assertEquals(status, 400);
    assertEquals(doc.errors[0].code, 'INVALID_PAGE_NUMBER');
  });

  await t.step(
    'oversized page size is 400 PAGE_SIZE_EXCEEDED (not clamped)',
    async () => {
      const { doc, status } = await getDoc(
        'articles?page%5Bsize%5D=1000',
        STRICT_BASE_URL,
      );
      assertEquals(status, 400);
      assertEquals(doc.errors[0].code, 'PAGE_SIZE_EXCEEDED');
      assert(doc.errors[0].meta.max === 100);
    },
  );
});

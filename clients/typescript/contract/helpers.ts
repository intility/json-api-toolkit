// deno-lint-ignore-file no-explicit-any
/**
 * Shared plumbing for the contract test suite.
 *
 * The suite runs against samples/ContractApi and pins the toolkit's ACTUAL
 * wire behavior, warts included. It assumes a freshly seeded server (see
 * samples/ContractApi/Data.cs); restart the sample between local runs.
 */

export const BASE_URL = Deno.env.get('CONTRACT_API_URL') ??
  'http://localhost:5198';
export const STRICT_BASE_URL = Deno.env.get('CONTRACT_API_STRICT_URL') ??
  'http://localhost:5199';

// Seed facts (mirrors samples/ContractApi/Data.cs)
export const TOTAL_ARTICLES = 25;
export const PUBLISHED_ARTICLES = 13; // odd ids 1..25
export const UNPUBLISHED_ARTICLES = 12; // even ids, publishedAt is null

/**
 * Resource types as today's consumers write them: non-nullable everywhere,
 * because the current AttributeKeys/RelationshipKeys helpers drop nullable
 * and optional properties (one of the type lies the v1 redesign fixes).
 */
export type ContractAuthor = {
  id: string;
  name: string;
  email: string;
};

export type ContractComment = {
  id: string;
  text: string;
  createdAt: string;
  articleId: number;
  authorId: number;
  author: ContractAuthor;
};

export type ContractArticle = {
  id: string;
  title: string;
  body: string;
  published: boolean;
  publishedAt: string;
  viewCount: number;
  tags: string[];
  authorId: number;
  author: ContractAuthor;
  comments: ContractComment[];
};

export interface WireResult {
  status: number;
  doc: any;
  headers: Headers;
}

export async function request(
  method: string,
  path: string,
  opts: { body?: unknown; contentType?: string; base?: string } = {},
): Promise<WireResult> {
  const res = await fetch(`${opts.base ?? BASE_URL}/${path}`, {
    method,
    headers: opts.body !== undefined
      ? { 'Content-Type': opts.contentType ?? 'application/vnd.api+json' }
      : undefined,
    body: opts.body !== undefined ? JSON.stringify(opts.body) : undefined,
  });
  const text = await res.text();
  let doc: any = null;
  if (text) {
    try {
      doc = JSON.parse(text);
    } catch {
      doc = text;
    }
  }
  return { status: res.status, doc, headers: res.headers };
}

export function getDoc(path: string, base?: string): Promise<WireResult> {
  return request('GET', path, { base });
}

/** Convenience: totalResources from a collection response. */
export function total(doc: any): number {
  return doc?.meta?.pagination?.totalResources;
}

/**
 * Shared plumbing for the contract test suite.
 *
 * The suite runs against samples/ContractApi and pins the toolkit's ACTUAL
 * wire behavior, warts included. It assumes a freshly seeded server (see
 * samples/ContractApi/Data.cs); restart the sample between local runs.
 */
import type {
  JsonApiArrayResponse,
  JsonApiErrorResponse,
  JsonApiSingleResponse,
} from '../src/index.ts';

export const BASE_URL = Deno.env.get('CONTRACT_API_URL') ??
  'http://localhost:5198';
export const STRICT_BASE_URL = Deno.env.get('CONTRACT_API_STRICT_URL') ??
  'http://localhost:5199';

// Seed facts (mirrors samples/ContractApi/Data.cs)
export const TOTAL_ARTICLES = 25;
export const PUBLISHED_ARTICLES = 13; // odd ids 1..25
export const UNPUBLISHED_ARTICLES = 12; // even ids, publishedAt is null

/** Wire document shapes, named short because every test step names one. */
export type Single = JsonApiSingleResponse;
export type List = JsonApiArrayResponse;
export type Errors = JsonApiErrorResponse;

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

export interface WireResult<T> {
  status: number;
  /** Parsed JSON body as `T`; `null` when the body is empty or not JSON. */
  doc: T;
  headers: Headers;
}

/**
 * Raw request. `T` is the caller's claim about the body shape; the suite
 * asserts on the wire, so a wrong claim fails loudly at the assertion.
 */
export async function request<T = unknown>(
  method: string,
  path: string,
  opts: { body?: unknown; contentType?: string; base?: string } = {},
): Promise<WireResult<T>> {
  const res = await fetch(`${opts.base ?? BASE_URL}/${path}`, {
    method,
    headers: opts.body !== undefined
      ? { 'Content-Type': opts.contentType ?? 'application/vnd.api+json' }
      : undefined,
    body: opts.body !== undefined ? JSON.stringify(opts.body) : undefined,
  });
  const text = await res.text();
  let doc: unknown = null;
  if (text) {
    try {
      doc = JSON.parse(text);
    } catch {
      doc = text;
    }
  }
  return { status: res.status, doc: doc as T, headers: res.headers };
}

export function getDoc<T = unknown>(
  path: string,
  base?: string,
): Promise<WireResult<T>> {
  return request<T>('GET', path, { base });
}

/** Convenience: totalResources from a collection response. */
export function total(doc: List): number | undefined {
  return doc.meta?.pagination?.totalResources;
}

/**
 * Type-level probes for the key helpers. There is nothing to run; a wrong
 * classification fails `deno check`. Each probe pairs a positive assignment
 * with a `@ts-expect-error` negative so both directions are pinned.
 */
import type { AttributeKeys, RelationshipKeys } from './query-builder.ts';

type Author = { id: string; name: string; email: string | null };
type Article = {
  id: string;
  title: string;
  publishedAt: string | null;
  tags: string[];
  author: Author | null;
  editor?: Author;
  comments: { id: string; text: string; author: Author }[];
};

// Nullable and optional relationships are relationships.
const author: RelationshipKeys<Article> = 'author';
const editor: RelationshipKeys<Article> = 'editor';
const comments: RelationshipKeys<Article> = 'comments';
// @ts-expect-error a primitive array is an attribute, not a relationship
const tags: RelationshipKeys<Article> = 'tags';

// Nullable attributes and primitive arrays are attributes.
const publishedAt: AttributeKeys<Article> = 'publishedAt';
const tagsAttr: AttributeKeys<Article> = 'tags';
// Nested attributes reach through nullable to-one relationships.
const authorEmail: AttributeKeys<Article> = 'author.email';
// @ts-expect-error id is never a filterable attribute
const id: AttributeKeys<Article> = 'id';
// @ts-expect-error to-many relationships do not expose nested attributes
const commentText: AttributeKeys<Article> = 'comments.text';

// Deep paths (2+ dots) are a checked-first-segment escape hatch: the
// backend walks dot-paths through to-many relationships via Any(), which
// the type helpers above don't model past one level.
const deepPath: AttributeKeys<Article> = 'comments.author.name';
// @ts-expect-error the first segment of a deep path must be a real relationship
const deepPathBogusRelationship: AttributeKeys<Article> = 'bogus.author.name';

Deno.test('type probes compile', () => {
  void [author, editor, comments, tags, publishedAt, tagsAttr, authorEmail];
  void [id, commentText];
  void [deepPath, deepPathBogusRelationship];
});

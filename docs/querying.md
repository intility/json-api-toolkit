# Querying

JsonApiToolkit parses standard JSON:API query parameters and applies them to your `IQueryable` automatically when you call `JsonApiQueryAsync`.

## Filtering

`filter[field]=value` for simple equality, or `filter[field][operator]=value` for operators.

| Operator | Meaning | Example |
|----------|---------|---------|
| `eq` | equal | `filter[price][eq]=10` |
| `ne` | not equal | `filter[status][ne]=archived` |
| `gt` / `ge` | greater than (or equal) | `filter[price][gt]=10` |
| `lt` / `le` | less than (or equal) | `filter[price][le]=50` |
| `like` | contains | `filter[title][like]=Hobbit` |
| `in` / `nin` | (not) in list | `filter[genre][in]=fiction,fantasy` |
| `isnull` / `isnotnull` | null check | `filter[description][isnull]=true` |

### Logical groups

Combine filters with AND, OR, NOT blocks:

```
filter[and][0][price][gt]=10&filter[and][1][genre]=fiction
filter[or][0][title][like]=Hobbit&filter[or][1][author]=Tolkien
filter[not][0][status]=archived
```

### Filtering across relationships

Two distinct shapes do different things:

- **Dot notation** filters the **primary resource** by a related field:
  `filter[author.country][eq]=UK` returns only books whose author's country is UK.
- **Bracket syntax** filters **included resources**:
  `include=reviews&filter[reviews][status][eq]=approved` returns all books, but each book's `reviews` only contains approved ones.

> [!NOTE]
> Filtered includes (bracket syntax) work up to two levels deep (`parent.child`). Filters at three or more levels return 400 Bad Request. This limit is not configurable.

## Sorting

`sort=field` ascending, `sort=-field` descending, comma-separated for multi-field:

```
sort=title,-publishedDate
```

## Pagination

```
page[number]=2&page[size]=10
```

By default, invalid values are silently clamped (`page[number]=0` → 1, oversized → `MaxPageSize`). Enable `StrictPagination` to return errors instead. See [Security](security.md#strict-pagination).

## Includes

`include=author,reviews` for top-level relationships, dot-separated for nesting:

```
include=author,publisher.country
```

Without `[AllowedIncludes]`, every relationship is includable. See [Security](security.md#allowedincludes) to restrict which.

## Sparse fieldsets

`fields[type]=field1,field2` returns only those attributes per resource type. `id` and `type` are always returned; relationships are unaffected.

```
fields[books]=title,publishedDate
fields[books]=title&fields[author]=name&include=author
```

By default, sparse fieldsets only filter at serialization time; EF Core still loads full entities. Set `EnableDatabaseProjection = true` to push the projection into the SQL `SELECT`. See [Performance](performance.md).

## Putting it together

```
GET /api/books?filter[title][like]=Hobbit&sort=-publishedDate&page[size]=10&include=author,reviews
```

This filters by title, sorts newest first, returns 10 results, and eager-loads `author` and `reviews`.

## Limits

Query complexity limits (max filters, max include depth, max page size, etc.) are configurable via `JsonApiOptions`. See [Security](security.md#query-complexity-limits) for the full table and behavior.

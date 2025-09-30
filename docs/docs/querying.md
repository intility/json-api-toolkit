# Querying

JsonApiToolkit provides robust support for JSON:API querying, including filtering, sorting, pagination, and including related resources.

## Supported Query Parameters

- **Filtering (`filter`):**  
  Use the `filter` parameter to narrow down results. You can specify simple filters or advanced filters with operators.  
  
  **Simple Filters:**
  - `GET /api/books?filter[title]=The Hobbit`
  
  **Advanced Filters with Operators:**
  - `eq` (equal): `GET /api/books?filter[price][eq]=10`
  - `ne` (not equal): `GET /api/books?filter[status][ne]=archived`
  - `gt` (greater than): `GET /api/books?filter[price][gt]=10`
  - `ge` (greater than or equal): `GET /api/books?filter[price][ge]=10`
  - `lt` (less than): `GET /api/books?filter[price][lt]=50`
  - `le` (less than or equal): `GET /api/books?filter[price][le]=50`
  - `like` (contains): `GET /api/books?filter[title][like]=Hobbit`
  - `in` (in list): `GET /api/books?filter[genre][in]=fiction,fantasy,mystery`
  - `nin` (not in list): `GET /api/books?filter[status][nin]=archived,deleted`
  - `isnull` (is null): `GET /api/books?filter[description][isnull]=true`
  - `isnotnull` (is not null): `GET /api/books?filter[description][isnotnull]=true`
  
  **Logical Groups:**
  - AND groups: `GET /api/books?filter[and][0][price][gt]=10&filter[and][1][genre]=fiction`
  - OR groups: `GET /api/books?filter[or][0][title][like]=Hobbit&filter[or][1][author]=Tolkien`
  - NOT groups: `GET /api/books?filter[not][0][status]=archived`

- **Sorting (`sort`):**  
  The `sort` parameter allows multiple sort criteria. Prefixing a field with a minus (`-`) indicates descending order.
  - Example: `GET /api/books?sort=title,-publishedDate`

- **Pagination (`page[number]` and `page[size]`):**  
  Control how many results are returned and which page to view.
  - Example: `GET /api/books?page[number]=2&page[size]=5`

- **Inclusion (`include`):**  
  Specify which related resources should be included in the response.
  - Example: `GET /api/books?include=author,reviews`
  
- **Filtering on Includes (Advanced):**
  Filter included resources using dot notation. This feature applies filters directly to the included relationships at the database level.
  - Example: `GET /api/books?include=reviews&filter[reviews.status][eq]=approved`
  - Complex filters: `GET /api/books?include=reviews&filter[or][0][reviews.rating][gte]=4&filter[or][1][reviews.featured][eq]=true`
  - Nested includes: `GET /api/vulnerabilities?include=cve,cve.cvecomments&filter[cvecomments.companyCode][eq]=AA`

> [!NOTE]
> Filtered includes currently support up to 2-level nesting (e.g., `parent.child`). Deeper nesting will fall back to unfiltered includes.

## How It Works

JsonApiToolkit automatically parses these parameters through its built-in query parser and applies them to your Entity Framework queries. This is accomplished by extension methods such as:

- `ApplyFilters`
- `ApplySorting`
- `ApplyPagination`
- `ApplyJsonApiParameters`

These methods allow you to take a raw IQueryable and layer on the JSON:API query conventions seamlessly.

## Example

A typical query URL might look like:

```
GET /api/books?filter[title][like]=Hobbit&sort=-publishedDate&page[number]=1&page[size]=10&include=author,reviews
```

With this request, the toolkit will:
- Filter books to those whose title contains "Hobbit".
- Sort the results with the newest published books first.
- Return the first 10 results.
- Include related author and reviews data in the response.

**Note:** Filters without dot notation apply only to the main resource type (books in this example). Filters with dot notation (e.g., `filter[reviews.status][eq]=approved`) filter the included resources themselves.

## Limitations

- **Include filter validation**: Filters with dot notation can only be applied to relationships that are explicitly included in the request. Use the `AllowedIncludesAttribute` to control which relationships can be filtered.
- **Complex nested filtering**: Maximum filter depth is 3 levels (e.g., `entity.relationship.property`).

## Attribute Mapping

The primary ID property is automatically excluded from attributes since it's already present in the resource's `"id"` field.



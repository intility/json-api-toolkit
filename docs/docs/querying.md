# Querying

JsonApiToolkit provides robust support for JSON:API querying, including filtering, sorting, pagination, and including related resources.

## Supported Query Parameters

- **Filtering (`filter`):**  
  Use the `filter` parameter to narrow down results. You can specify simple filters or advanced filters with operators.  
  Example:
  - Simple filter: `GET /api/books?filter[title]=The Hobbit`
  - Advanced filter: `GET /api/books?filter[price][gt]=10`

- **Sorting (`sort`):**  
  The `sort` parameter allows multiple sort criteria. Prefixing a field with a minus (`-`) indicates descending order.
  - Example: `GET /api/books?sort=title,-publishedDate`

- **Pagination (`page[number]` and `page[size]`):**  
  Control how many results are returned and which page to view.
  - Example: `GET /api/books?page[number]=2&page[size]=5`

- **Inclusion (`include`):**  
  Specify which related resources should be included in the response.
  - Example: `GET /api/books?include=author,reviews`

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

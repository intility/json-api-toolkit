# Building Custom Queries

The `BuildJsonApiQueryAsync` method provides access to the processed query **before execution**, enabling custom operations like CSV exports, aggregations, and projections.

## Overview

```csharp
protected async Task<JsonApiQueryResult<T>> BuildJsonApiQueryAsync<T>(
    IQueryable<T> queryable,
    string resourceType,
    bool includeCount = true)
    where T : class
```

**Returns:**

| Property | Type | Description |
|----------|------|-------------|
| `Query` | `IQueryable<T>` | The processed query with filters, includes, and sorting applied. **Pagination is NOT applied.** |
| `Parameters` | `QueryParameters` | The parsed query parameters from the request. |
| `TotalCount` | `int` | Total matching records. Returns 0 if `includeCount` is false. |

## When to Use

Use `BuildJsonApiQueryAsync` when you need to:

- **Export data** (CSV, Excel, JSON file) - you need all matching records, not paginated results
- **Aggregate data** - apply GROUP BY after filtering
- **Custom projections** - select specific columns or transform the data
- **Stream results** - process large datasets without loading everything into memory
- **Combine with other queries** - use the filtered query as a subquery

For standard JSON:API responses with pagination, use `JsonApiQueryAsync` instead.

## Examples

### CSV Export

```csharp
[HttpGet("export")]
public async Task<IActionResult> ExportBooks()
{
    var result = await BuildJsonApiQueryAsync(_context.Books, "books");
    var books = await result.Query.ToListAsync();

    var csv = new StringBuilder();
    csv.AppendLine("Id,Title,Author,PublishedDate");

    foreach (var book in books)
    {
        csv.AppendLine($"{book.Id},{book.Title},{book.Author},{book.PublishedDate:yyyy-MM-dd}");
    }

    return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "books.csv");
}
```

**Request:**
```
GET /api/books/export?filter[publishedDate][gt]=2020-01-01&sort=title&include=author
```

All matching books are exported (no pagination), with filters, sorting, and includes applied.

### Custom Projection

```csharp
[HttpGet("titles-only")]
public async Task<IActionResult> GetTitlesOnly()
{
    var result = await BuildJsonApiQueryAsync(_context.Books, "books");

    // Project to minimal DTO
    var titles = await result.Query
        .Select(b => new { b.Id, b.Title })
        .ToListAsync();

    return Ok(new
    {
        count = result.TotalCount,
        titles
    });
}
```

### Streaming Large Datasets

```csharp
[HttpGet("stream")]
public async IAsyncEnumerable<BookDto> StreamBooks()
{
    var result = await BuildJsonApiQueryAsync(_context.Books, "books", includeCount: false);

    await foreach (var book in result.Query.AsAsyncEnumerable())
    {
        yield return new BookDto
        {
            Id = book.Id,
            Title = book.Title,
            Author = book.Author?.Name
        };
    }
}
```

### Combining with Other Queries

```csharp
[HttpGet("with-sales")]
public async Task<IActionResult> GetBooksWithSales()
{
    var result = await BuildJsonApiQueryAsync(_context.Books, "books");

    // Join with sales data from another source
    var booksWithSales = await result.Query
        .Join(
            _context.Sales,
            book => book.Id,
            sale => sale.BookId,
            (book, sale) => new { book, sale })
        .GroupBy(x => x.book)
        .Select(g => new
        {
            Book = g.Key.Title,
            TotalSales = g.Sum(x => x.sale.Quantity),
            Revenue = g.Sum(x => x.sale.Amount)
        })
        .ToListAsync();

    return Ok(booksWithSales);
}
```

---

## Statistics and Aggregations

When building statistics endpoints, you need to apply filters **before** aggregating. Otherwise, filters won't work on your DTOs.

### The Problem

```csharp
[HttpGet("genre-stats")]
public async Task<IActionResult> GetGenreStatsAsync()
{
    var stats = _context.Books
        .GroupBy(b => b.Genre)
        .Select(g => new { Genre = g.Key, Count = g.Count() });

    // ❌ This FAILS - the DTO doesn't have PublishedDate
    return await JsonApiQueryAsync(stats, "genre_stats");
}
```

**Request:** `GET /api/books/genre-stats?filter[publishedDate][gt]=2020-01-01`

The filter tries to apply to the anonymous DTO, but it doesn't have `PublishedDate`. The filter is silently skipped.

### Solution: Filter First, Then Aggregate

Use `BuildJsonApiQueryAsync` to apply filters to the source entity, then aggregate:

```csharp
[HttpGet("genre-stats")]
public async Task<IActionResult> GetGenreStatsAsync()
{
    // Apply filters to Book entity BEFORE aggregation
    var result = await BuildJsonApiQueryAsync(_context.Books, "books", includeCount: false);

    // Aggregate the already-filtered query
    var stats = await result.Query
        .GroupBy(b => b.Genre)
        .Select(g => new
        {
            Genre = g.Key,
            Count = g.Count(),
            AveragePrice = g.Average(b => b.Price)
        })
        .ToListAsync();

    return Ok(stats);
}
```

Now `filter[publishedDate][gt]=2020-01-01` correctly filters books before counting.

### Simple Aggregations with ApplyFiltersOnly

For simple aggregations where you don't need includes or sorting, use `ApplyFiltersOnly()`:

```csharp
[HttpGet("genre-stats")]
public async Task<IActionResult> GetGenreStatsAsync()
{
    // Only apply filters (no includes, no sorting)
    var query = ApplyFiltersOnly(_context.Books);

    var stats = await query
        .GroupBy(b => b.Genre)
        .Select(g => new { Genre = g.Key, Count = g.Count() })
        .ToListAsync();

    return Ok(stats);
}
```

### With Business Logic Filters

Combine user filters with required business logic:

```csharp
[HttpGet("publisher-stats")]
public async Task<IActionResult> GetPublisherStatsAsync()
{
    // Start with business logic filter
    var query = _context.Books.Where(b => b.Status == BookStatus.Published);

    // Apply user filters from query params
    query = ApplyFiltersOnly(query);

    // Aggregate
    var stats = await query
        .GroupBy(b => b.Publisher)
        .Select(g => new
        {
            Publisher = g.Key,
            TotalBooks = g.Count(),
            AveragePrice = g.Average(b => b.Price)
        })
        .ToListAsync();

    return Ok(stats);
}
```

### Why Return Plain JSON for Statistics?

Statistics and aggregations aren't "resources" with stable IDs or relationships - they're computed views. Return plain JSON instead of JSON:API documents.

---

## Performance Considerations

### Skip Count When Not Needed

If you don't need the total count, set `includeCount: false` to skip the COUNT query:

```csharp
// Skips COUNT query - better for large datasets
var result = await BuildJsonApiQueryAsync(query, "books", includeCount: false);
```

### Use AsNoTracking for Read-Only Operations

For export/read-only operations, use `AsNoTracking()` on your queryable:

```csharp
var result = await BuildJsonApiQueryAsync(
    _context.Books.AsNoTracking(),
    "books"
);
```

### Consider Streaming for Large Exports

For very large datasets, use streaming instead of `ToListAsync()`:

```csharp
await foreach (var item in result.Query.AsAsyncEnumerable())
{
    // Process one item at a time
}
```

---

## Method Comparison

| Method | Filters | Includes | Sorting | Pagination | Returns |
|--------|---------|----------|---------|------------|---------|
| `JsonApiQueryAsync` | Yes | Yes | Yes | Yes | JSON:API document |
| `BuildJsonApiQueryAsync` | Yes | Yes | Yes | **No** | `JsonApiQueryResult<T>` |
| `ApplyFiltersOnly` | Yes | No | No | No | `IQueryable<T>` |

**When to use each:**

- **`JsonApiQueryAsync`** - Standard API responses with pagination
- **`BuildJsonApiQueryAsync`** - Exports, projections, when you need includes/sorting
- **`ApplyFiltersOnly`** - Simple aggregations where you only need filtering

## Query Parameters Applied

`BuildJsonApiQueryAsync` applies the following query parameters:

| Parameter | Applied |
|-----------|---------|
| `filter` | Yes - filters main entity |
| `filter[relation.field]` | Yes - filtered includes |
| `include` | Yes - eager loads relationships |
| `sort` | Yes - orders results |
| `page[number]`, `page[size]` | Parsed but **NOT applied** |

The pagination parameters are still available in `result.Parameters.Pagination` if you need them for custom pagination logic.

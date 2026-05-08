# Building Custom Queries

`BuildJsonApiQueryAsync` exposes the processed query *before execution*, so you can apply your own logic on top: exports, projections, streaming, aggregations.

For standard JSON:API responses, use `JsonApiQueryAsync` instead.

## Signature

```csharp
protected Task<JsonApiQueryResult<T>> BuildJsonApiQueryAsync<T>(
    IQueryable<T> queryable,
    string resourceType,
    bool includeCount = true) where T : class
```

`JsonApiQueryResult<T>` exposes:

- `Query`: the `IQueryable<T>` with filters, includes, and sorting applied. **Pagination is not applied.**
- `Parameters`: the parsed `QueryParameters`, including pagination if you want to apply it manually.
- `TotalCount`: total matching records, or `0` if `includeCount: false`.

## Custom queries: export example

The pattern is the same for CSV, Excel, JSON file, projection, or streaming: take `result.Query`, do whatever you want with it.

```csharp
[HttpGet("export")]
public async Task<IActionResult> ExportBooks()
{
    var result = await BuildJsonApiQueryAsync(_db.Books.AsNoTracking(), ResourceType);

    var books = await result.Query.ToListAsync();
    // serialize however you want: CSV, Excel, NDJSON, etc.
    return File(BuildCsv(books), "text/csv", "books.csv");
}
```

For very large exports, stream instead of `ToListAsync()`:

```csharp
await foreach (var book in result.Query.AsAsyncEnumerable())
{
    // one entity at a time
}
```

For projections, chain `Select` after `result.Query`:

```csharp
var titles = await result.Query.Select(b => new { b.Id, b.Title }).ToListAsync();
```

Pass `includeCount: false` to skip the COUNT query when you don't need it.

---

## Statistics and aggregations

Aggregation endpoints have a subtle trap: if you `GroupBy().Select(...)` first and hand the resulting query to `JsonApiQueryAsync`, user filters target the projected DTO, not the source entity. Any filter referencing a column the DTO doesn't expose is silently skipped, so requests look like they succeed but ignore the filter.

```csharp
// Filter is silently skipped: the anonymous DTO has no PublishedDate property.
var stats = _db.Books
    .GroupBy(b => b.Genre)
    .Select(g => new { Genre = g.Key, Count = g.Count() });

return await JsonApiQueryAsync(stats, ResourceType);
```

Apply filters to the source entity *first*, then aggregate:

```csharp
[HttpGet("genre-stats")]
public async Task<IActionResult> GetGenreStats()
{
    var result = await BuildJsonApiQueryAsync(_db.Books, ResourceType, includeCount: false);

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

Now `?filter[publishedDate][gt]=2020-01-01` filters books before the GROUP BY.

### `ApplyFiltersOnly` for simple cases

If you don't need includes or sorting, `ApplyFiltersOnly` is shorter:

```csharp
var query = ApplyFiltersOnly(_db.Books);

var stats = await query
    .GroupBy(b => b.Genre)
    .Select(g => new { Genre = g.Key, Count = g.Count() })
    .ToListAsync();
```

You can compose it with your own `Where` clauses:

```csharp
// business logic first, then user filters
var query = _db.Books.Where(b => b.Status == BookStatus.Published);
query = ApplyFiltersOnly(query);
```

Statistics aren't really "resources" with stable IDs and relationships, so return plain `Ok(...)` rather than a JSON:API document.

## Method cheat sheet

| Method | Filters | Includes | Sorting | Pagination | Returns |
|--------|---------|----------|---------|------------|---------|
| `JsonApiQueryAsync` | yes | yes | yes | yes | JSON:API document |
| `BuildJsonApiQueryAsync` | yes | yes | yes | no | `JsonApiQueryResult<T>` |
| `ApplyFiltersOnly` | yes | no | no | no | `IQueryable<T>` |

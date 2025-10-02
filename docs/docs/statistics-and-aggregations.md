# Working with Statistics and Aggregations

When working with aggregated data (statistics, counts, summaries), apply filters **before** aggregation. Once you've aggregated to a DTO, the original entity properties are no longer available for filtering.

## The Problem

```csharp
public class BookGenreStatsDto
{
    public string Genre { get; set; } = null!;
    public int BookCount { get; set; }
}

[HttpGet("genre-stats")]
public async Task<IActionResult> GetGenreStatsAsync()
{
    var query = _context.Books.AsQueryable();

    // Aggregate to DTO
    var stats = query
        .GroupBy(b => b.Genre)
        .Select(g => new BookGenreStatsDto
        {
            Genre = g.Key,
            BookCount = g.Count()
        });

    // ❌ This FAILS - BookGenreStatsDto doesn't have PublishedDate
    return await JsonApiQueryAsync(stats, "genre_stats");
}
```

**Query:**
```
GET /api/books/genre-stats?filter[PublishedDate][gt]=2020-01-01
```

**Problem:** The filter tries to apply to `BookGenreStatsDto`, but it doesn't have a `PublishedDate` property. The original `Book` entity does, but it's already been aggregated away.

**Result:**
```
[WRN] Property 'PublishedDate' not found on BookGenreStatsDto, skipping filter
```

This means the filter is silently ignored, and you get unfiltered statistics.

## Solution: Apply Filters Before Aggregation, Return Plain JSON

Use `ApplyFiltersOnly()` to filter the source entity **before** aggregating, then return plain JSON:

```csharp
public class BookGenreStatsDto
{
    public string Genre { get; set; } = null!;
    public int BookCount { get; set; }
}

[HttpGet("genre-stats")]
public async Task<IActionResult> GetGenreStatsAsync()
{
    // Apply filters to Book entity BEFORE aggregation
    var query = ApplyFiltersOnly(_context.Books);

    // Aggregate the already-filtered data
    IQueryable<BookGenreStatsDto> stats = query
        .GroupBy(b => new { b.Genre })
        .Select(g => new BookGenreStatsDto
        {
            Genre = g.Key.Genre,
            BookCount = g.Count()
        });

    // Return plain JSON for statistics
    return Ok(await stats.ToListAsync());
}
```

**Now this works:**
```
GET /api/books/genre-stats?filter[PublishedDate][gt]=2020-01-01
```

The filter applies to `Book` before grouping, so only books published after 2020 are counted.

### Why Plain JSON for Statistics?

Statistics and aggregations aren't "resources" with stable IDs or relationships - they're computed views.

## Common Patterns for Statistics

### Simple Aggregation
```csharp
[HttpGet("genre-stats")]
public async Task<IActionResult> GetGenreStatsAsync()
{
    var query = ApplyFiltersOnly(_context.Books);
    var stats = query.GroupBy(b => b.Genre)
                     .Select(g => new { Genre = g.Key, Count = g.Count() });

    return Ok(await stats.ToListAsync());
}
```

### With Service Layer
```csharp
[HttpGet("author-stats")]
public async Task<IActionResult> GetAuthorStatsAsync()
{
    var filteredBooks = ApplyFiltersOnly(_context.Books);
    var stats = _bookService.CalculateAuthorStats(filteredBooks);

    return Ok(await stats.ToListAsync());
}
```

### With Business Logic Filters
```csharp
[HttpGet("publisher-stats")]
public async Task<IActionResult> GetPublisherStatsAsync()
{
    // Start with base query
    var query = _context.Books.AsQueryable();

    // Apply required business logic filter (only published books)
    query = query.Where(b => b.Status == BookStatus.Published);

    // Apply user filters from query params
    query = ApplyFiltersOnly(query);

    // Aggregate
    var stats = query
        .GroupBy(b => b.Publisher)
        .Select(g => new PublisherStatsDto
        {
            Publisher = g.Key,
            TotalBooks = g.Count(),
            AveragePrice = g.Average(b => b.Price)
        });

    return Ok(await stats.ToListAsync());
}
```



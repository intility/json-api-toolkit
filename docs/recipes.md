# Recipes

Common scenarios with JsonApiToolkit. For setup, see [Getting Started](getting-started.md).

## Single resource by id

```csharp
[HttpGet("{id}")]
public async Task<IActionResult> GetBook(int id)
{
    var book = await _db.Books.FirstOrDefaultAsync(b => b.Id == id)
        ?? throw new JsonApiNotFoundException($"Book {id} not found");

    return JsonApiOk(book, ResourceType);
}
```

`JsonApiOk` wraps an already-loaded entity into a JSON:API document. Use it when you've fetched data yourself; use `JsonApiQueryAsync` when you want filtering/sorting/pagination applied automatically.

## Create a resource

```csharp
[HttpPost]
public async Task<IActionResult> CreateBook([FromBody] CreateBookRequest request)
{
    if (string.IsNullOrWhiteSpace(request.Title))
        throw new JsonApiBadRequestException("Title is required");

    var book = new Book { Title = request.Title, Author = request.Author };
    _db.Books.Add(book);
    await _db.SaveChangesAsync();

    return JsonApiCreated(book, ResourceType, book.Id.ToString());
}
```

## Update or delete

```csharp
[HttpPut("{id}")]
public async Task<IActionResult> UpdateBook(int id, [FromBody] UpdateBookRequest request)
{
    var book = await _db.Books.FindAsync(id)
        ?? throw new JsonApiNotFoundException($"Book {id} not found");

    book.Title = request.Title ?? book.Title;
    await _db.SaveChangesAsync();

    return JsonApiOk(book, ResourceType);
}

[HttpDelete("{id}")]
public async Task<IActionResult> DeleteBook(int id)
{
    var book = await _db.Books.FindAsync(id)
        ?? throw new JsonApiNotFoundException($"Book {id} not found");

    _db.Books.Remove(book);
    await _db.SaveChangesAsync();

    return JsonApiNoContent();
}
```

Always throw the `JsonApi*Exception` types instead of returning error `ActionResult`s. The toolkit's exception filter turns them into JSON:API error responses with the right status code.

## Layer business logic before query processing

```csharp
[HttpGet]
public async Task<IActionResult> GetPublishedAsync()
{
    var query = _db.Books.Where(b => b.IsPublished);
    return await JsonApiQueryAsync(query, ResourceType);
}
```

Apply your own `Where` clauses to the `IQueryable` before handing it to `JsonApiQueryAsync`. The toolkit layers user filters, includes, sorting, and pagination on top.

## Custom DTO with manual mapping

```csharp
[HttpGet("{id}/summary")]
public async Task<IActionResult> GetBookSummary(int id)
{
    var book = await _db.Books.Include(b => b.Author).FirstOrDefaultAsync(b => b.Id == id)
        ?? throw new JsonApiNotFoundException($"Book {id} not found");

    var summary = new BookSummary
    {
        Id = book.Id,
        Title = book.Title,
        AuthorName = book.Author?.Name
    };

    return JsonApiOk(summary, "bookSummary");
}
```

The resource type (second argument) is whatever string you want clients to see; it doesn't have to match the entity name. Define a separate `const` when an action returns a different resource type than the controller's default.

## See also

- Restrict includes with `[AllowedIncludes]`: [Security](security.md#allowedincludes).
- Filter included resources: [Querying](querying.md#filtering-across-relationships).
- Export without pagination, projections, aggregations: [Building Custom Queries](build-query.md).

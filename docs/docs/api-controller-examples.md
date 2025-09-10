# API Controller Examples

This guide provides practical examples of implementing JSON:API controllers using JsonApiToolkit.

## Basic Controller Setup

```csharp
using JsonApiToolkit.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class BooksController : JsonApiController
{
    private readonly AppDbContext _context;

    public BooksController(AppDbContext context)
    {
        _context = context;
    }

    // Controller methods here...
}
```

## GET Collection with Full Query Support

```csharp
[HttpGet]
public async Task<IActionResult> GetBooks()
{
    IQueryable<Book> books = _context.Books;
    return await JsonApiOkAsync(books, "book");
}
```

This automatically supports:
- Filtering: `GET /api/books?filter[title][like]=Hobbit`
- Sorting: `GET /api/books?sort=-publishedDate,title`  
- Pagination: `GET /api/books?page[number]=2&page[size]=10`
- Includes: `GET /api/books?include=author,reviews`

## GET Single Resource

```csharp
[HttpGet("{id}")]
public async Task<IActionResult> GetBook(int id)
{
    var book = await _context.Books
        .FirstOrDefaultAsync(b => b.Id == id);
        
    if (book == null)
        return JsonApiNotFound($"Book with ID {id} not found");
        
    return JsonApiOk(book, "book");
}
```

## POST Create Resource

```csharp
[HttpPost]
public async Task<IActionResult> CreateBook([FromBody] CreateBookRequest request)
{
    if (string.IsNullOrWhiteSpace(request.Title))
        throw new JsonApiBadRequestException("Book title cannot be empty");

    var book = new Book
    {
        Title = request.Title,
        Author = request.Author,
        PublishedDate = request.PublishedDate
    };

    _context.Books.Add(book);
    await _context.SaveChangesAsync();

    return JsonApiCreated(book, "book", book.Id.ToString());
}
```

## PUT Update Resource

```csharp
[HttpPut("{id}")]
public async Task<IActionResult> UpdateBook(int id, [FromBody] UpdateBookRequest request)
{
    var book = await _context.Books.FindAsync(id);
    if (book == null)
        throw new JsonApiNotFoundException($"Book with ID {id} not found");

    book.Title = request.Title ?? book.Title;
    book.Author = request.Author ?? book.Author;
    book.PublishedDate = request.PublishedDate ?? book.PublishedDate;

    await _context.SaveChangesAsync();

    return JsonApiOk(book, "book");
}
```

## DELETE Resource

```csharp
[HttpDelete("{id}")]
public async Task<IActionResult> DeleteBook(int id)
{
    var book = await _context.Books.FindAsync(id);
    if (book == null)
        throw new JsonApiNotFoundException($"Book with ID {id} not found");

    _context.Books.Remove(book);
    await _context.SaveChangesAsync();

    return JsonApiNoContent();
}
```

## Advanced Filtering Example

```csharp
[HttpGet("search")]
public async Task<IActionResult> SearchBooks()
{
    // The filtering is handled automatically by JsonApiOkAsync
    // But you can also apply custom logic before the standard processing
    
    var books = _context.Books
        .Where(b => b.IsPublished); // Custom business logic
        
    return await JsonApiOkAsync(books, "book");
}
```

**Supports complex queries like:**
```
GET /api/books/search?filter[and][0][price][gt]=10&filter[and][1][or][0][genre]=fiction&filter[and][1][or][1][genre]=fantasy&sort=-rating,title&include=author
```

## Custom Response with Manual Mapping

```csharp
[HttpGet("{id}/summary")]
public IActionResult GetBookSummary(int id)
{
    var book = _context.Books
        .Include(b => b.Author)
        .FirstOrDefault(b => b.Id == id);
        
    if (book == null)
        return JsonApiNotFound();

    // Manual mapping for custom response structure
    var summary = new BookSummary 
    { 
        Id = book.Id,
        Title = book.Title,
        AuthorName = book.Author?.Name,
        PageCount = book.Pages
    };
    
    return JsonApiOk(summary, "bookSummary");
}
```

## Error Handling Examples

```csharp
[HttpPost("{id}/reserve")]
public async Task<IActionResult> ReserveBook(int id)
{
    var book = await _context.Books.FindAsync(id);
    if (book == null)
        throw new JsonApiNotFoundException($"Book with ID {id} not found");
        
    if (book.IsReserved)
        throw new JsonApiConflictException($"Book '{book.Title}' is already reserved");
        
    if (!User.Identity?.IsAuthenticated == true)
        throw new JsonApiUnauthorizedException("You must be logged in to reserve books");
        
    // Business logic here...
    
    return JsonApiOk(book, "book");
}
```

## Entity Configuration

For the examples above, here's the entity setup:

```csharp
public class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public DateTime PublishedDate { get; set; }
    public decimal Price { get; set; }
    public bool IsPublished { get; set; }
    public bool IsReserved { get; set; }
    
    // Navigation properties for relationships
    public List<Review> Reviews { get; set; } = [];
    public Author AuthorDetails { get; set; } = null!;
}
```

## Security with AllowedIncludes

Control which relationships can be included to prevent exposure of sensitive data:

```csharp
[HttpGet("users")]
[AllowedIncludes("profile", "posts.*", "settings")]
public async Task<IActionResult> GetUsers()
{
    return await JsonApiOkAsync(_context.Users, "user");
}

[HttpGet("sensitive-data")]
[AllowedIncludes("publicInfo")]
public async Task<IActionResult> GetSensitiveData()
{
    return await JsonApiOkAsync(_context.SensitiveEntities, "sensitiveEntity");
}

[HttpGet("public-only")]
[AllowedIncludes()] // No includes allowed
public async Task<IActionResult> GetPublicOnly()
{
    return await JsonApiOkAsync(_context.PublicData, "publicData");
}
```

**Supported requests:**
- `GET /api/users?include=profile` ✅ Allowed
- `GET /api/users?include=posts.comments` ✅ Allowed (wildcard)
- `GET /api/users?include=posts.comments.author` ❌ Forbidden (too deep)
- `GET /api/sensitive-data?include=secrets` ❌ Forbidden

## Pro Tips

1. **Always use exception types** instead of returning error ActionResults - the filter handles conversion automatically
2. **Use JsonApiOkAsync for collections** - it provides full query parameter support
3. **Use JsonApiOk for single resources** - simpler and faster for individual entities
4. **Use AllowedIncludes** - restrict relationship access for security and performance


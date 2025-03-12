# API Controller Example

This example demonstrates how to implement a .NET API controller that leverages JsonApiToolkit for standardized JSON:API responses. In this example, we create a `BooksController` that supports common HTTP operations: GET (single and collection), POST, PATCH, and DELETE.

> [!NOTE]
> This example assumes you have set up [JsonApiToolkit](../README.md) in your project and configured your dependency injection accordingly.

---

## Prerequisites

Before you begin, ensure you have:

- Registered JsonApiToolkit in your DI container using `builder.Services.AddJsonApiToolkit();`
- A DbContext (e.g., `MyDbContext`) with a `Books` DbSet.
- A simple `Book` model, for example:

```csharp
public class Book
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    // Additional properties can be added here.
}
```

## Controller Implementation

```csharp
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using JsonApiToolkit.Controllers;

namespace MyApi.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/[controller]")]
public class BooksController : JsonApiController
{
    private readonly MyDbContext _dbContext;
    private const string ResourceType = "book";

    public BooksController(MyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // GET: api/books
    [HttpGet]
    public async Task<IActionResult> GetBooks()
    {
        // Retrieves an IQueryable of books and applies JSON:API query parameters
        IQueryable<Book> query = _dbContext.Books;

        // JsonApiOkAsync applies filtering, sorting, and pagination automatically.
        return await JsonApiOkAsync(query, ResourceType);
    }

    // GET: api/books/{id}
    [HttpGet("{id}")]
    public IActionResult GetBook(Guid id)
    {
        var book = _dbContext.Books.FirstOrDefault(b => b.Id == id);
        if (book == null)
        {
            return JsonApiNotFound("Book not found.");
        }
        return JsonApiOk(book, ResourceType);
    }

    // POST: api/books
    [HttpPost]
    public IActionResult CreateBook([FromBody] Book newBook)
    {
        if (newBook == null)
            return JsonApiBadRequest("Invalid book data.");

        _dbContext.Books.Add(newBook);
        _dbContext.SaveChanges();

        // Creates a JSON:API compliant 201 Created response with a self-link.
        return JsonApiCreated(newBook, ResourceType, newBook.Id);
    }

    // PATCH: api/books/{id}
    [HttpPatch("{id}")]
    public IActionResult UpdateBook(string id, [FromBody] Book updatedBook)
    {
        var book = _dbContext.Books.AsNoTracking().FirstOrDefault(b => b.Id == id);
        if (book == null)
        {
            return JsonApiNotFound("Book not found.");
        }

        // For simplicity, assume updatedBook contains the latest values.
        // In a real-world scenario, use a proper patching mechanism.
        updatedBook.Id = id; // Ensure the ID remains unchanged.
        _dbContext.Books.Update(updatedBook);
        _dbContext.SaveChanges();

        return JsonApiOk(updatedBook, ResourceType);
    }

    // DELETE: api/books/{id}
    [HttpDelete("{id}")]
    public IActionResult DeleteBook(string id)
    {
        var book = _dbContext.Books.FirstOrDefault(b => b.Id == id);
        if (book == null)
        {
            return JsonApiNotFound("Book not found.");
        }

        _dbContext.Books.Remove(book);
        _dbContext.SaveChanges();

        // Returns a 204 No Content response indicating successful deletion.
        return JsonApiNoContent();
    }
}

```

## Explanation
- **GET Collection (GetBooks):**
    Retrieves an `IQueryable<Book>` from the database and uses `JsonApiOkAsync()` to automatically apply JSON:API query parameters such as filtering, sorting, and pagination.

- **GET Single (GetBook):**
    Fetches a single book by ID. If the book exists, it returns a JSON:API document with `JsonApiOk()`.
    Otherwise, it returns a standard 404 error using `JsonApiNotFound()`.

- **POST (CreateBook):**
    Accepts a new book in the request body, adds it to the database, and returns a 201 Created response along with
    a self-link via `JsonApiCreated().`

- **PATCH (UpdateBook):**
    Retrieves an existing book, applies updates, and saves changes. The updated resource is then returned with
    `JsonApiOk()`. In production, consider using patch documents for partial updates.

- **DELETE (DeleteBook):**
    Removes the specified book from the database. On successful deletion, it returns a 204 No Content response
    using `JsonApiNoContent()`.


## Adding Relationships

If you want to include relationships in your responses, you need to apply inclusions to your queries. Currently, JsonApiToolkit does not support automatic inclusion of related resources. However, you can manually include related entities using Entity Framework's `Include()` method. Create a helper method to handle this logic:

```csharp
using Microsoft.EntityFrameworkCore;

namespace SecCenterBackend.Helpers
{
    /// <summary>
    /// Extension methods for applying JSON:API relationship includes to an IQueryable.
    /// </summary>
    public static class IQueryableExtensions
    {
        /// <summary>
        /// Applies each include (as a string) to the IQueryable.
        /// The include strings must match the navigation property names defined on the entity.
        /// </summary>
        /// <typeparam name="T">The type of the entity.</typeparam>
        /// <param name="source">The IQueryable to augment.</param>
        /// <param name="includes">A collection of include strings (e.g. "User", "Category").</param>
        /// <returns>The IQueryable with the include statements applied.</returns>
        public static IQueryable<T> ApplyJsonApiIncludes<T>(
            this IQueryable<T> source,
            IEnumerable<string> includes
        )
            where T : class
        {
            if (includes == null)
                return source;

            foreach (var include in includes)
                source = source.Include(include);

            return source;
        }
    }
}
```

You can then use this method in your controller actions to include related resources:

```csharp
// GET: api/books
[HttpGet]
public async Task<IActionResult> GetBooks()
{
    // Retrieve JSON:API query parameters
    QueryParameters queryParams = GetJsonApiQueryParameters();
    // Retrieves an IQueryable of books and applies JSON:API query parameters
    IQueryable<Book> query = _dbContext.Books;
    // Apply includes to the query if requested
    if (queryParams.Include?.Count > 0)
        query = query.ApplyJsonApiIncludes(queryParams.Include);
    // JsonApiOkAsync applies filtering, sorting, and pagination automatically.
    return await JsonApiOkAsync(query, ResourceType);
}
```


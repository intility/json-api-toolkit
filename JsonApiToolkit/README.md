# JsonApiToolkit

JsonApiToolkit is a lightweight, easy-to-use library for building JSON:API compliant APIs in ASP.NET Core. It simplifies the implementation of JSON:API specification features such as resource relationships, filtering, sorting, pagination, and sparse fieldsets.


## Quick Start

Install the JsonApiToolkit package from NuGet:

```sh
dotnet add package JsonApiToolkit
```

Add the JsonApiToolkit services to your `Program.cs` or `Startup.cs`:

```csharp
builder.Services.AddJsonApiToolkit();
```

Extend your controller from `JsonApiController` and use the provided helper methods:

```csharp
namespace YourProject.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/todos")]
public class TodosController(AppDbContext context) : JsonApiController
{
    private readonly AppDbContext _context = context;
    private const string ResourceType = "todos";

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var queryParams = GetJsonApiQueryParameters();

        var query = _context.Todos!.AsQueryable();

        var todo = await query.FirstOrDefaultAsync(t => t.Id == id);

        if (todo == null)
            return JsonApiNotFound();

        return JsonApiOk(todo, ResourceType);
    }
}
```

# Getting Started

This guide walks you through installing and configuring JsonApiToolkit in your .NET application.

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download) or later.
- An ASP.NET Core project.

## Installation

```bash
dotnet add package Intility.JsonApiToolkit
```

## Setup

1. **Register services:**
   In your `Program.cs` or `Startup.cs`, add the toolkit services to your dependency injection container:

   ```csharp
   builder.Services.AddJsonApiToolkit();
   ```

   You can also configure options for query limits and pagination:

   ```csharp
   builder.Services.AddJsonApiToolkit(options => {
       options.MaxFilters = 100;                  // Default: 50
       options.MaxFilterGroups = 20;              // Default: 10
       options.MaxFilterDepth = 5;                // Default: 3
       options.MaxPageSize = 200;                 // Default: 100
       options.DefaultPageSize = 25;              // Default: 10
       options.StrictPagination = true;           // Default: false
       options.EnableDatabaseProjection = true;   // Default: false
   });
   ```

   > [!TIP]
   > See the [Security](security.md#query-complexity-limits) documentation for security options and [Performance](performance.md) for database projection.

2. **Inheritance:**
   Derive your API controllers from the provided `JsonApiController` to leverage helper methods that return JSON:API compliant responses.

   ```csharp
   public class BooksController : JsonApiController
   {
       // Your endpoint implementations here
   }
   ```

3. **Configuration:**
    The toolkit automatically configures JSON serialization settings (camelCase properties, ignoring nulls, etc.) and adds the JSON:API media type to the supported output formatters.

> [!NOTE]
> Now your API is ready to return responses that fully comply with the JSON:API specification!


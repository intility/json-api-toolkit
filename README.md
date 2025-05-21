[![CI/CD Pipeline](https://github.com/intility/Intility.JsonApiToolkit/actions/workflows/ci-cd.yml/badge.svg)](https://github.com/intility/Intility.JsonApiToolkit/actions/workflows/ci-cd.yml)
[![Build Docs](https://github.com/intility/Intility.JsonApiToolkit/actions/workflows/build-docs.yml/badge.svg)](https://github.com/intility/Intility.JsonApiToolkit/actions/workflows/build-docs.yml)

# Intility.JsonApiToolkit

JsonApiToolkit is a lightweight toolkit for implementing the [JSON:API specification](https://jsonapi.org/) in .NET applications. 

## Installation

To install this package from Intility's GitHub Packages, add this to your NuGet.config file:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="github" value="https://nuget.pkg.github.com/Intility/index.json" />
  </packageSources>
  <packageSourceCredentials>
    <github>
      <add key="Username" value="YOUR_GITHUB_USERNAME" />
      <add key="ClearTextPassword" value="YOUR_GITHUB_PAT" />
    </github>
  </packageSourceCredentials>
</configuration>
```

Then install the package via NuGet:

```bash
dotnet add package Intility.JsonApiToolkit
```

## Setup

1. **Register services:**
   In your `Program.cs` or `Startup.cs`, add the toolkit services to your dependency injection container:

   ```csharp
   builder.Services.AddJsonApiToolkit();
   ```

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

## GitHub Actions
To get fetch the package in your GitHub Actions workflow, add the following to your workflow file:

```yaml
- name: Add Intility NuGet Package Source
  run: |
    dotnet nuget add source https://nuget.pkg.github.com/Intility/index.json \
    --name Intility \
    --username ${{ github.actor }} \
    --password ${{ secrets.GITHUB_TOKEN }} \
    --store-password-in-clear-text
```

> [!IMPORTANT]
> Your Github token must have the `read:packages` scope to access the package.

## Endpoint Example

```csharp   
// GET: api/books
[HttpGet]
public async Task<IActionResult> GetBooks()
{
    var query = _dbContext.Books.AsQueryable();

    // JsonApiOkAsync applies filtering, sorting, includes, and pagination automatically.
    return await JsonApiOkAsync(query, "book");
}
```

[More examples (WIP)](https://congenial-telegram-prep6vq.pages.github.io/docs/api-controller-examples.html)

## Documentation
For complete documentation and detailed usage instructions, please visit our 
[documentation page.](https://intility.github.io/Intility.JsonApiToolkit/)

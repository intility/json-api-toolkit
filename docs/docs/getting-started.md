# Getting Started

This guide walks you through installing and configuring JsonApiToolkit in your .NET application.

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download) or later.
- An ASP.NET Core project.

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

   You can also configure options for query limits and pagination:

   ```csharp
   builder.Services.AddJsonApiToolkit(options => {
       options.MaxFilters = 100;                  // Default: 50
       options.MaxFilterGroups = 20;              // Default: 10
       options.MaxFilterDepth = 5;                // Default: 3
       options.MaxPageSize = 200;                 // Default: 100
       options.DefaultPageSize = 25;              // Default: 10
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

## Dependabot
To enable Dependabot to fetch the package, add the registry to your `dependabot.yml` file. Example:

```yaml
version: 2

registries:
  intility:
    type: "nuget-feed"
    url: "https://nuget.pkg.github.com/Intility/index.json"
    username: "x-access-token"
    password: ${{ secrets.INTILITY_NUGET_DEPENDABOT }}

updates:
  - package-ecosystem: "nuget"
    directory: "/"
    registries: "*"
    schedule:
      interval: "weekly"
    open-pull-requests-limit: 5
    commit-message:
      prefix: "🤖"
    labels:
      - "dependencies"
```

> [!IMPORTANT]
> You need to create a token with `read:packages` that you add to
> your dependabot secrets. 

# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is **Intility.JsonApiToolkit**, a .NET library that implements the JSON:API specification for ASP.NET Core applications. It provides controllers, models, parsers, and utilities to build JSON:API compliant REST APIs.

## Technology Stack

- **Target Framework**: .NET 9.0
- **Dependencies**: 
  - Microsoft.AspNetCore.Mvc
  - Microsoft.EntityFrameworkCore
  - Microsoft.AspNetCore.JsonPatch
  - Microsoft.Extensions.DependencyInjection.Abstractions
  - Intility.Logging.AspNetCore
- **Test Framework**: xUnit with Moq for mocking
- **Documentation**: DocFX for API documentation

## Development Commands

### Building and Testing
```bash
# Restore dependencies
dotnet restore

# Build the solution (Release configuration)
dotnet build --no-restore --configuration Release

# Run tests
dotnet test --no-build --configuration Release --verbosity normal

# Build and pack for distribution
dotnet pack JsonApiToolkit/JsonApiToolkit.csproj -p:PackageVersion=VERSION -c Release
```

### Code Formatting
```bash
# Format code with CSharpier
dotnet tool restore
dotnet csharpier format .

# Check formatting (CI uses this)
dotnet csharpier check .
```

### Documentation
Documentation is built using DocFX and deployed to GitHub Pages. The documentation source is in `/docs/` and the built site goes to `/docs/_site/`.

### Debugging
Enable detailed logging for query processing and troubleshooting:
- Set `"JsonApiToolkit": "Debug"` in appsettings.json
- See `/docs/docs/debugging.md` for comprehensive debugging guide

## Architecture

### Core Components

1. **JsonApiController** (`Controllers/JsonApiController.cs`)
   - Base controller for JSON:API endpoints
   - Provides methods: `JsonApiOk()`, `JsonApiQueryAsync()`, `JsonApiCreated()`, `JsonApiNotFound()`, etc.
   - Handles query parameter parsing and response formatting
   - Automatically applies filtering, sorting, pagination, and includes (filtering applies to main entity and included resources; includes load related resources)

2. **JsonApiMapper** (`Mapping/JsonApiMapper.cs`)
   - Core mapper for converting entities to JSON:API resource structures
   - Methods: `ToResourceObject()`, `ToDocument()`, `ToCollectionDocument()`
   - Handles attributes, relationships, and included resources

3. **Query Processing Pipeline** (`Extensions/Querying/`)
   - `JsonApiQueryParser`: Parses JSON:API query parameters
   - `FilterExpressionBuilder`: Builds LINQ expressions from filters
   - `FilterHandler`: Applies filter expressions to queryables
   - `SortingHandler`: Applies sorting to queryables
   - `PaginationHandler`: Applies pagination to queryables
   - `QueryHelpers`: Helper methods for property name mapping and type conversion
   - `IncludeFilterParser`: Separates filters targeting included resources from main entity filters
   - `FilteredIncludeBuilder`: Applies filtered includes using EF Core's filtered Include functionality

4. **Models** (`Models/`)
   - Document structures: `JsonApiDocument<T>`, `JsonApiCollectionDocument<T>`
   - Resources: `ResourceObject`, `ResourceIdentifier`, `Relationship`
   - Query parameters: `QueryParameters`, `FilterParameter`, `SortParameter`
   - Errors: `JsonApiError`, `JsonApiErrorResponse`

5. **Attributes** (`Attributes/`)
   - `AllowedIncludesAttribute`: Restricts which relationships can be included in responses
   
6. **Validation** (`Validation/`)
   - `IncludePatternValidator`: Validates include patterns with wildcard support
   - `IncludeValidator`: Validates include paths against entity relationships
   - `IncludePattern`: Model representing validated include patterns

7. **Services** (`Services/`)
   - `IJsonApiQueryParser`: Interface for query parameter parsing
   - `JsonApiQueryParserService`: Service implementation for parsing JSON:API query strings

8. **Helpers** (`Helpers/`)
   - `EfIncludePathHelper`: Utilities for building EF Core Include expressions
   - `ReflectionMethodCache`: Cached reflection method lookups with defensive checks and clear error messages

### Key Patterns

- **Convention-based mapping**: Properties are automatically mapped from C# PascalCase to JSON camelCase
- **Query parameter parsing**: Standard JSON:API query syntax (`filter[field]=value`, `sort=field,-field2`, `page[number]=1&page[size]=10`, `include=relationship`)
- **Async-first**: Main controller method `JsonApiQueryAsync()` is async and works with `IQueryable<T>`
- **Entity Framework integration**: Uses EF Core's `Include()` and query building capabilities
- **Filter expressions**: Complex filtering with operators (eq, ne, gt, lt, contains, etc.), logical grouping, enum support, and filtering on included resources via dot notation (e.g., `filter[author.name]=John` with `include=author`)
- **JSON column detection**: Collections and complex objects without ID properties are automatically mapped as JSON attributes instead of relationships (useful for EF Core owned entities stored as JSON columns)
- **Pagination safety**: Invalid page numbers are automatically clamped to valid ranges (page 1 for negative/zero, last page for overflow)
- **Include whitelisting**: Use `AllowedIncludesAttribute` on controller actions to restrict which relationships can be included, preventing unauthorized data exposure
- **Graceful error handling**: Malformed query parameters are logged and skipped rather than throwing exceptions

### Service Registration

Use `AddJsonApiToolkit()` extension method in `Program.cs`:
- Configures JSON serialization (camelCase, ignore nulls, handle cycles)
- Adds JSON:API media type support (`application/vnd.api+json`)
- Registers filters and services

### Error Handling System

JsonApiToolkit provides a comprehensive error handling system with standardized exception types:

**Built-in Exception Types** (`Models/Errors/JsonApiErrorTypes.cs`):
- `JsonApiBadRequestException` (400) - Validation or malformed input
- `JsonApiUnauthorizedException` (401) - Not authenticated  
- `JsonApiForbiddenException` (403) - Not authorized
- `JsonApiNotFoundException` (404) - Resource not found
- `JsonApiConflictException` (409) - Unique constraint or conflict
- `JsonApiTooManyRequestsException` (429) - Rate limiting exceeded

**JsonApiExceptionFilter** automatically:
- Converts exceptions to proper HTTP status codes and JSON:API error responses
- Logs handled errors without stack traces (400, 404, etc.)
- Logs unhandled exceptions with full stack traces (500)

**Usage**: Throw specific exceptions in controllers/services instead of returning error ActionResults.

### Testing Structure

Tests are organized by component:
- `Controllers/`: Controller behavior tests
- `Extensions/`: Query extension tests (filtering, sorting, pagination, includes)
- `Mapping/`: Entity mapping tests
- `Models/`: Model validation tests
- `Parsing/`: Query parser tests (including malformed input handling)
- `Helpers/`: Helper class tests (ReflectionMethodCache, etc.)
- `Integration/`: Full pipeline integration tests
- `Filters/`: Exception filter tests
- `Attributes/`: Attribute behavior tests (AllowedIncludes, etc.)

## Development Guidelines

### Adding New Features

1. **Query Operations**: Extend `FilterExpressionBuilder` and add corresponding operators to `FilterOperator` enum
2. **New Controllers**: Inherit from `JsonApiController` and use provided helper methods
3. **Entity Mapping**: Use `EntityMapper` for automatic detection of relationships vs attributes based on ID properties
4. **Testing**: Follow existing patterns with xUnit and Moq, test both success and error scenarios

### Common Patterns

- Controllers should inherit from `JsonApiController`
- Use `JsonApiQueryAsync(queryable, "resourceType")` for collections with full query processing
- Use `JsonApiOk(entity, "resourceType")` for already-loaded entities or collections
- Entity types should have an `Id` property (auto-detected by `EntityMapper.GetIdProperty()`)
- Use `QueryParameters queryParams = GetJsonApiQueryParameters()` to access parsed query parameters
- For manual mapping, use `JsonApiMapper.ToDocument()` or `ToCollectionDocument()`
- Enable debug logging with `"JsonApiToolkit": "Debug"` in appsettings.json for detailed query processing insights

## Package Publication

The project publishes to GitHub Packages. Use semantic versioning for releases. The CI/CD pipeline automatically builds, tests, and publishes on GitHub releases.

## Refactoring Roadmap

The project has a structured refactoring plan in `.claude/`:
- `REFACTORING_ROADMAP.md` - Phase-by-phase plan with checklists
- `CODEBASE_ANALYSIS.md` - Analysis of issues found in the codebase
- `REPO_IMPROVEMENTS.md` - Repository setup tasks (CI/CD, branch protection)

Use `/roadmap` command to get current status and next tasks.
# Debug Logging Guide for JsonApiToolkit

JsonApiToolkit now includes comprehensive debug logging throughout the query processing pipeline. This guide explains how to activate and configure debug logging in applications using JsonApiToolkit.

## Logging Framework

JsonApiToolkit uses the standard Microsoft.Extensions.Logging framework with the `Intility.Logging.AspNetCore` package for enhanced logging capabilities.

## Configuration

### 1. Basic Setup

To enable debug logging for JsonApiToolkit, configure your logging in `Program.cs` or `appsettings.json`:

#### Option A: Configure in appsettings.json

Add the following to your `appsettings.json` or `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "JsonApiToolkit": "Debug"
    }
  }
}
```

#### Option B: Configure in Program.cs

```csharp
builder.Logging.AddFilter("JsonApiToolkit", LogLevel.Debug);
```

### 2. Specific Component Logging

For more granular control, you can configure logging for specific JsonApiToolkit components:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "JsonApiToolkit.Controllers.JsonApiController": "Debug",
      "JsonApiToolkit.Services.JsonApiQueryParserService": "Debug",
      "JsonApiToolkit.Extensions.Querying.FilterExpressionBuilder": "Debug",
      "JsonApiToolkit.Extensions.Querying.FilterHandler": "Debug",
      "JsonApiToolkit.Extensions.Querying.SortingHandler": "Debug",
      "JsonApiToolkit.Mapping.JsonApiMapper": "Debug"
    }
  }
}
```

### 3. Production Safety

For production environments, set JsonApiToolkit logging to `Warning` or `Error` to avoid performance impact:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "JsonApiToolkit": "Warning"
    }
  }
}
```

## What Gets Logged

When debug logging is enabled, JsonApiToolkit logs detailed information about:

### Query Processing Pipeline
- **Request parsing**: Query parameters parsed from HTTP requests
- **Filter processing**: Filter criteria application and expression building
- **Sorting**: Sort parameter application
- **Pagination**: Page calculation and application
- **Includes**: Relationship loading and mapping

### Filter Expression Building
- **Filter separation**: Main entity vs. included resource filters
- **Expression construction**: LINQ expression building process
- **Property mapping**: Field name to CLR property mapping
- **Operator handling**: Different filter operators (eq, ne, gt, lt, in, etc.)
- **Nested navigation**: Dot notation property access

### Entity Mapping
- **Resource object creation**: Entity to JSON:API resource mapping
- **Attribute extraction**: Property mapping to JSON:API attributes
- **Relationship processing**: Related entity handling
- **Document structure**: JSON:API document assembly

### Performance Insights
- **Query execution**: Database query execution timing
- **Result counts**: Number of entities processed
- **Include processing**: Relationship loading details

## Example Log Output

With debug logging enabled, you'll see detailed logs like:

```
[DBG] Starting JSON:API query processing for resource type 'books'
[DBG] Parsed query parameters: Filters=2, Sorts=1, Includes=1, Pagination=True
[DBG] Mapped 1 include paths to CLR properties: Author
[DBG] Separated filters: MainFilters=1, IncludeFilters=1
[DBG] Applying 1 main entity filters
[DBG] Building filter expression for 1 filters and 0 nested groups with logical operator And
[DBG] Processing filter: Field='title', Operator=Like, Value='API'
[DBG] Successfully built filter expression for field 'title'
[DBG] Using regular includes strategy - applying includes before sorting
[DBG] Applying 1 regular includes
[DBG] Applying 1 sort parameters after includes
[DBG] Executing count query to get total resource count
[DBG] Total count after filtering: 5
[DBG] Applying pagination: Page=1, Size=10
[DBG] Executing final query to retrieve results
[DBG] Retrieved 5 results from database
[DBG] Mapping results to JSON:API document structure
[DBG] Creating JSON:API collection document for entities of type Book with resource type 'books'
[DBG] Successfully completed JSON:API query processing for resource type 'books' with 5 resources and 5 included resources
```

## Performance Considerations

Debug logging adds overhead to request processing. Consider:

1. **Development**: Enable debug logging for troubleshooting
2. **Staging**: Use `Information` or `Warning` level
3. **Production**: Use `Warning` or `Error` level only
4. **Performance testing**: Disable debug logging to get accurate metrics

## Troubleshooting Common Issues

### Filter Problems
Look for logs containing:
- "Property 'fieldName' not found" - Field name doesn't match entity property
- "Failed to convert filter value" - Type conversion issues
- "Filter expression builder returned null" - Invalid filter configuration

### Include Problems
Look for logs containing:
- "Property 'relationship' not found during nested navigation" - Invalid include path
- "Mapped X include paths" - Verify expected relationships are included

### Performance Issues
Look for:
- High result counts without pagination
- Complex filter expressions with many nested groups
- Multiple database queries for includes

## Integration with Intility.Logging.AspNetCore

JsonApiToolkit integrates seamlessly with `Intility.Logging.AspNetCore`. The structured logging provides:

- **Request correlation**: All logs for a request are correlated
- **Structured data**: Filter counts, entity types, and processing steps are logged as structured data
- **Performance metrics**: Query execution timing and result counts
- **Error context**: Detailed context when errors occur

## Best Practices

1. **Use environment-specific configuration** to avoid debug logging in production
2. **Monitor log volume** as debug logging can be verbose
3. **Use structured logging filters** to focus on specific components
4. **Combine with application monitoring** tools for comprehensive observability
5. **Review logs regularly** during development to optimize query patterns

## Disable Logging

To completely disable JsonApiToolkit logging:

```json
{
  "Logging": {
    "LogLevel": {
      "JsonApiToolkit": "None"
    }
  }
}
```

Or in code:

```csharp
builder.Logging.AddFilter("JsonApiToolkit", LogLevel.None);
```

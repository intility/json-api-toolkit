# Debugging Guide

This guide explains how to enable debug logging for JsonApiToolkit to troubleshoot query processing, filtering, and EF Core expression generation.

## Enable Debug Logging

Add the following configuration to your `appsettings.json` or `appsettings.Development.json`:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "JsonApiToolkit": "Debug"
      }
    }
  }
}
```

For Microsoft.Extensions.Logging, use:

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

## User-Friendly Logging

JsonApiToolkit provides helpful **Information** and **Warning** level logs that guide you when things go wrong:

### Common Issues and Log Messages

**Property Not Found in Filters:**
```
WARN: Property 'userName' not found on entity type 'User'. Available properties: Id, Name, Email. Check your filter field names
```

**Invalid Include Paths:**
```
WARN: Some includes could not be mapped for User: profile, invalidRelation. Check property names and navigation relationships
```

**Query Parameter Syntax Issues:**
```
WARN: Filter parameters detected but no valid filters parsed. Check filter syntax: filter[fieldName][operator]=value
WARN: Sort parameter detected but no valid sorts parsed. Check sort syntax: sort=field1,-field2
```

**Type Conversion Errors:**
```
ERROR: Failed to convert filter value 'invalid-date' to type 'DateTime'. Expected format examples: DateTime: '2023-12-25T10:30:00Z'
```

**Performance Warnings:**
```
WARN: Large number of filters detected (15). This may impact performance. Consider simplifying the query
WARN: Large result set detected (5000 records). Consider adding pagination or more specific filters
```

**Empty Results:**
```
INFO: Query returned 0 results for User. This might be due to filters or include conditions. Check your filter values and relationship data
```

## What Gets Logged

**Information Level:**
- Include processing status and helpful hints
- Empty result explanations
- Query result summaries

**Warning Level:**
- Invalid property names with suggestions
- Parameter parsing issues with examples
- Performance concerns
- Include mapping problems

**Debug Level (detailed):**
- Query parameter parsing details
- Filter expression building steps
- Include path mapping
- EF Core query execution
- Pagination calculations

## Log Categories

- `JsonApiToolkit.Controllers.JsonApiController` - Main query processing and user-friendly messages
- `JsonApiToolkit.Services.JsonApiQueryParserService` - Parameter parsing warnings
- `JsonApiToolkit.Extensions.Querying` - Filter processing and property resolution
- `JsonApiToolkit.Mapping.JsonApiMapper` - Entity-to-JSON mapping

## Performance Impact

- **Information/Warning logs**: Minimal overhead, safe for production
- **Debug logs**: More detailed, recommended for development only
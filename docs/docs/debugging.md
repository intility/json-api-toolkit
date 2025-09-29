# Debugging Guide

## Enable Logging

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Override": {
        "JsonApiToolkit": "Debug",
        "Microsoft.EntityFrameworkCore.Database.Command": "Information"
      }
    }
  }
}
```

Or for Microsoft.Extensions.Logging:

```json
{
  "Logging": {
    "LogLevel": {
      "JsonApiToolkit": "Debug",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

## What Gets Logged

**Information:**
- Empty result explanations
- Complex queries (>20 filters)

**Warning:**
- Invalid property/field names
- Parameter parsing issues
- Large unpaginated results (>1000)
- Include mapping problems

**Debug:**
- Query summaries (filters/sorts/includes/pagination)
- Include strategy (SingleQuery/SplitQuery)
- Execution summaries (counts)

**EF Core SQL (Information level):**
- Actual SQL queries executed

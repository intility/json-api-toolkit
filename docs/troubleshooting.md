# Troubleshooting

## Enable logging

For Serilog:

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

For `Microsoft.Extensions.Logging`:

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

The toolkit emits:

- **Information**: empty result explanations, complex queries (>20 filters), execution summaries.
- **Warning**: invalid property/field names, parameter parsing issues, large unpaginated results (>1000), include mapping problems.
- **Debug**: query summaries, include strategy (single vs split query), filter/sort/include/pagination details.

EF Core's `Microsoft.EntityFrameworkCore.Database.Command` logger at `Information` shows the actual SQL.

## Common pitfalls

### Filter looks wrong but returns 200 with no error

Malformed filter, sort, and include parameters are logged at warning level and silently skipped, not thrown. Check your logs at `Warning` level for `JsonApiToolkit`. You'll see exactly what was rejected. To turn these into 400 responses, enable `StrictQueryValidation` (see [Security](security.md#strict-query-validation)).

### Filter on an aggregation DTO is ignored

If you pass an anonymous projection or DTO to `JsonApiQueryAsync`, filters target the DTO's properties, not the source entity. A filter referencing a column the DTO doesn't expose is silently skipped. Use `BuildJsonApiQueryAsync` (or `ApplyFiltersOnly`) to filter the source `IQueryable` *before* aggregating. See [Building Custom Queries](build-query.md).

### Filtered include only works two levels deep

`filter[parent][field]=x` and `filter[parent.child][field]=x` work. A filter at three or more levels returns 400 Bad Request with `JsonApiBadRequestException`. The limit is hard-coded for query plan stability and isn't configurable. Restructure the request to filter the primary resource (dot notation) instead.

### Page number out of range returns clamped data instead of an error

By default, invalid page numbers are silently clamped: `page[number]=0` becomes 1, `page[number]=99999` becomes the last page. Enable `StrictPagination` (see [Security](security.md#strict-pagination)) to return 400/404 instead.

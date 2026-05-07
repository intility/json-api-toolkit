# CLAUDE.md

## Code Formatting

```bash
dotnet tool restore
dotnet csharpier format .
```

## Debugging

Set `"JsonApiToolkit": "Debug"` in appsettings.json to enable detailed query processing logs.

## Non-Obvious Behaviors

- **JSON column detection**: Collections and complex objects without an `Id` property are mapped as JSON attributes, not relationships. This handles EF Core owned entities stored as JSON columns.
- **Pagination clamping**: Invalid page numbers are silently clamped (negative/zero -> page 1, overflow -> last page).
- **Malformed query params**: Bad filter/sort/include syntax is logged and skipped, not thrown as exceptions.
- **Filtered includes**: Dot notation in filters (e.g. `filter[author.name]=John`) applies to included resources when `include=author` is also set.
- **Include allowlisting**: `AllowedIncludesAttribute` on controller actions restricts which relationships can be requested via `include=`, preventing unauthorized data exposure.
- **Sparse fieldsets**: `fields[type]=field1,field2` works for both primary and included resources. `id` and `type` are always returned.

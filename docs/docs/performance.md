# Performance

## Database Projection

By default, `JsonApiQueryAsync` loads full entities from the database and applies sparse fieldset
filtering in memory. For entities with many columns or large JSON blob properties, this means
unnecessary data is transferred from the database even when the client only needs a few fields.

**Database projection** solves this: when `fields[type]` is specified and projection is enabled,
the toolkit generates a `Select()` expression at runtime that tells EF Core to fetch only the
requested columns.

### Enabling projection

```csharp
services.AddJsonApiToolkit(options =>
{
    options.EnableDatabaseProjection = true;
});
```

Projection is disabled by default. No other code changes are needed in controllers.

### What gets projected

When a request includes `fields[type]=field1,field2`:

- Only the requested scalar attributes are fetched from the database.
- The `id` column is always included.
- Navigation properties are included only when referenced by `include=`.
  Navigation properties **not** in `include=` are excluded from the projection, so EF Core
  skips the corresponding JOINs entirely.

### Example

```
GET /api/articles?fields[articles]=title&include=author
```

EF Core generates SQL equivalent to:

```sql
SELECT a.Id, a.Title, au.Id, au.Name, au.Email
FROM Articles a
LEFT JOIN Authors au ON a.AuthorId = au.Id
```

Without projection, all columns from `Articles` would be selected.

### JSON blob columns

Owned entities stored as JSON columns (EF Core 7+) are particularly expensive because the
entire JSON blob is always deserialized. With projection, if the blob's properties are not in
`fields[type]`, the column is excluded from the `SELECT` entirely.

### Performance expectations

| Scenario | Without projection | With projection |
|----------|--------------------|-----------------|
| Entity with 50 columns, client needs 5 | Load all 50 | Load 5 |
| Entity with JSON blob, blob not in fields | Deserialize blob | Skip blob column |
| Include not requested | JOIN still executed | JOIN skipped |

### Fallback behavior

If projection fails for any reason (e.g., unsupported EF Core provider behavior), the toolkit
logs a warning and falls back to loading the full entity. No error is returned to the client.

### Limitations

- **NativeAOT**: Not compatible. Projection uses `Reflection.Emit` to generate types at runtime,
  which is unavailable in NativeAOT builds. Keep `EnableDatabaseProjection = false` (the default)
  when targeting NativeAOT.
- **Included entity projection**: Related entities loaded via `include=` are still fetched in full.
  Only the primary resource's columns are projected.
- **EF Core provider**: Requires a SQL-translating EF Core provider (SQL Server, PostgreSQL,
  SQLite, etc.). The in-memory provider evaluates projections in memory, so there is no SQL
  benefit in unit tests, but behavior is identical.

### Caching

Projection types are generated once per unique (source type, field set) combination and cached
for the lifetime of the application. Subsequent requests with the same `fields[type]` reuse the
cached type and expression with no additional overhead.

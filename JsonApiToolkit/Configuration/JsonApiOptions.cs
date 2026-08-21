namespace JsonApiToolkit.Configuration;

/// <summary>
/// Configuration options for JsonApiToolkit query processing and security limits.
/// </summary>
public class JsonApiOptions
{
    /// <summary>
    /// Maximum number of individual filter conditions allowed in a query.
    /// Default: 50. Set to int.MaxValue to disable.
    /// </summary>
    public int MaxFilters { get; set; } = 50;

    /// <summary>
    /// Maximum number of logical filter groups (OR/NOT blocks) allowed.
    /// Default: 10. Set to int.MaxValue to disable.
    /// </summary>
    public int MaxFilterGroups { get; set; } = 10;

    /// <summary>
    /// Maximum nesting depth for filter groups.
    /// Default: 3. Set to int.MaxValue to disable.
    /// </summary>
    public int MaxFilterDepth { get; set; } = 3;

    /// <summary>
    /// Maximum length for a single filter value string.
    /// Default: 1000. Set to int.MaxValue to disable.
    /// </summary>
    public int MaxFilterValueLength { get; set; } = 1000;

    /// <summary>
    /// Maximum include depth (e.g., "author.posts.comments" = depth 3).
    /// Default: 3. Set to int.MaxValue to disable.
    /// </summary>
    public int MaxIncludeDepth { get; set; } = 3;

    /// <summary>
    /// Maximum page size allowed. Requests exceeding this are clamped.
    /// Default: 100.
    /// </summary>
    public int MaxPageSize { get; set; } = 100;

    /// <summary>
    /// Default page size when not specified in request.
    /// Default: 10.
    /// </summary>
    public int DefaultPageSize { get; set; } = 10;

    /// <summary>
    /// When true, returns 400 Bad Request for invalid pagination values instead of
    /// silently clamping. Invalid page numbers (less than 1) and page sizes (less than 1 or
    /// exceeding MaxPageSize) will return errors. Default: false (clamp for backwards compatibility).
    /// </summary>
    public bool StrictPagination { get; set; }

    /// <summary>
    /// When true, returns 400 Bad Request instead of silently ignoring invalid query
    /// parameters: unknown filter fields, unconvertible filter values, unknown filter
    /// operators, unknown sort fields, malformed filter keys, unsupported filter group
    /// shapes (filter[and] and nested groups), and bracket-syntax include filters whose
    /// relationship is not included. Default: false (log and skip for backwards compatibility).
    /// </summary>
    public bool StrictQueryValidation { get; set; }

    /// <summary>
    /// When true, pagination links (first/last/prev/next) preserve the request's full
    /// query string (filter, sort, include, fields) with only the page parameters
    /// replaced. Default: false (links are rebuilt from the bare path and drop all
    /// other query parameters, for backwards compatibility).
    /// </summary>
    public bool PreserveQueryInPaginationLinks { get; set; }

    /// <summary>
    /// When true, applies database-level column filtering via EF Core Select() projection
    /// when fields[type] is specified in the request. Only fetches requested columns from
    /// the database instead of loading full entities and filtering in memory.
    /// Not compatible with NativeAOT compilation. Default: false.
    /// </summary>
    public bool EnableDatabaseProjection { get; set; } = false;
}

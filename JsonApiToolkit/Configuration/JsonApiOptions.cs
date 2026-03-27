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
    /// When true, applies database-level column filtering via EF Core Select() projection
    /// when fields[type] is specified in the request. Only fetches requested columns from
    /// the database instead of loading full entities and filtering in memory.
    /// Not compatible with NativeAOT compilation. Default: false.
    /// </summary>
    public bool EnableDatabaseProjection { get; set; } = false;
}

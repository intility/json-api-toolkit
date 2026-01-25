namespace JsonApiToolkit.Models.Querying;

/// <summary>
/// Result of BuildJsonApiQueryAsync containing the processed query and metadata.
/// </summary>
/// <typeparam name="T">The entity type being queried.</typeparam>
public class JsonApiQueryResult<T>
    where T : class
{
    /// <summary>
    /// The processed IQueryable with filters, includes, and sorting applied.
    /// Pagination is NOT applied - use this for custom operations like exports or aggregations.
    /// </summary>
    public required IQueryable<T> Query { get; init; }

    /// <summary>
    /// The parsed query parameters from the request.
    /// </summary>
    public required QueryParameters Parameters { get; init; }

    /// <summary>
    /// Total count of matching records. Returns 0 if includeCount was false.
    /// </summary>
    public int TotalCount { get; init; }
}

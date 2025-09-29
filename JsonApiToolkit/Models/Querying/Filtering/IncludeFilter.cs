namespace JsonApiToolkit.Models.Querying.Filtering;

/// <summary>
/// Filter applied to an included relationship (e.g., filter[author.name]=John).
/// </summary>
public class IncludeFilter
{
    /// <summary>
    /// Relationship path (e.g., "author").
    /// </summary>
    public string RelationshipPath { get; set; } = string.Empty;

    /// <summary>
    /// Field path within the relationship (e.g., "name").
    /// </summary>
    public string FieldPath { get; set; } = string.Empty;

    /// <summary>
    /// Filter condition to apply.
    /// </summary>
    public FilterParameter Filter { get; set; } = new();

    /// <summary>
    /// Full path combining relationship and field (e.g., "author.name").
    /// </summary>
    public string FullPath =>
        string.IsNullOrEmpty(FieldPath) ? RelationshipPath : $"{RelationshipPath}.{FieldPath}";
}

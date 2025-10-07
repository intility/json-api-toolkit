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
    /// Filter group containing all conditions for this relationship path.
    /// Preserves logical operator structure (AND/OR/NOT).
    /// </summary>
    public FilterGroup FilterGroup { get; set; } = new();
}

namespace JsonApiToolkit.Models.Querying.Filtering;

/// <summary>
/// Represents a filter that should be applied to an included relationship in a JSON:API query.
/// </summary>
/// <remarks>
/// Include filters allow filtering of related resources that are being included in the response.
/// For example, when including comments on a post, you can filter to only include comments from a specific author.
/// </remarks>
public class IncludeFilter
{
    /// <summary>
    /// The relationship path to filter on, using JSON property names.
    /// </summary>
    /// <remarks>
    /// This is the navigation property path from the main entity to the relationship being filtered.
    /// For example: "cveComments" for a direct relationship, or "cveComments.author" for a nested relationship.
    /// </remarks>
    public string RelationshipPath { get; set; } = string.Empty;

    /// <summary>
    /// The field path within the related entity to filter on.
    /// </summary>
    /// <remarks>
    /// This is the property path within the related entity that should be filtered.
    /// Can be a simple property name like "companyCode" or a nested path like "author.department".
    /// </remarks>
    public string FieldPath { get; set; } = string.Empty;

    /// <summary>
    /// The filter parameter containing the operator and value for the filter condition.
    /// </summary>
    public FilterParameter Filter { get; set; } = new();

    /// <summary>
    /// The full field path from the original filter parameter.
    /// </summary>
    /// <remarks>
    /// This combines RelationshipPath and FieldPath with a dot separator.
    /// For example: "cveComments.companyCode"
    /// </remarks>
    public string FullPath =>
        string.IsNullOrEmpty(FieldPath) ? RelationshipPath : $"{RelationshipPath}.{FieldPath}";
}

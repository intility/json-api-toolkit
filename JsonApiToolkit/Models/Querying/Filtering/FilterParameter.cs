namespace JsonApiToolkit.Models.Querying.Filtering;

/// <summary>
/// Single filter condition with field, operator, and value.
/// </summary>
public class FilterParameter
{
    /// <summary>
    /// Field to filter on.
    /// </summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// Comparison operator.
    /// </summary>
    public FilterOperator Operator { get; set; } = FilterOperator.Eq;

    /// <summary>
    /// Value to compare against.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if this filter should be applied to included relationships (filtered includes)
    /// rather than filtering the primary resource.
    /// When true: filters what gets included (e.g., filter[rel][field][op]=value bracket syntax)
    /// When false: filters the primary resource, optionally navigating through relationships (dot notation)
    /// </summary>
    public bool IsIncludeFilter { get; set; } = false;
}

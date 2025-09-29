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
}

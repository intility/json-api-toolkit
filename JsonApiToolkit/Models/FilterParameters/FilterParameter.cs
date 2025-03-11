namespace JsonApiToolkit.Models.FilterParameters;

/// <summary>
/// Represents a filter operator for comparing filter parameters.
/// </summary>
public class FilterParameter
{
    /// <summary>
    /// The field to filter.
    /// </summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// The operator to use for comparison.
    /// </summary>
    public FilterOperator Operator { get; set; } = FilterOperator.Eq;

    /// <summary>
    /// The value to compare.
    /// </summary>
    public string Value { get; set; } = string.Empty;
}

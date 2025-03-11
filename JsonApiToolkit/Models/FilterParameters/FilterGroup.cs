namespace JsonApiToolkit.Models.FilterParameters;

/// <summary>
/// Represents a group of filter parameters.
/// </summary>
public class FilterGroup
{
    /// <summary>
    /// The logical operator to use for combining filters.
    /// </summary>
    public LogicalOperator LogicalOperator { get; set; } = LogicalOperator.And;

    /// <summary>
    /// The filters to apply.
    /// </summary>
    public List<FilterParameter> Filters { get; set; } = [];

    /// <summary>
    /// The groups of filters to apply.
    /// </summary>
    public List<FilterGroup> Groups { get; set; } = [];
}

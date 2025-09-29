namespace JsonApiToolkit.Models.Querying.Filtering;

/// <summary>
/// Group of filter conditions combined with a logical operator (AND/OR/NOT).
/// Supports nested groups for complex expressions.
/// </summary>
public class FilterGroup
{
    /// <summary>
    /// Logical operator for combining filters and groups.
    /// </summary>
    public LogicalOperator LogicalOperator { get; set; } = LogicalOperator.And;

    /// <summary>
    /// Individual filter conditions.
    /// </summary>
    public List<FilterParameter> Filters { get; set; } = [];

    /// <summary>
    /// Nested filter groups for complex expressions.
    /// </summary>
    public List<FilterGroup> Groups { get; set; } = [];
}

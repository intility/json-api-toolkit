namespace JsonApiToolkit.Models.Querying.Filtering;

/// <summary>
/// Represents a group of filter conditions combined with a logical operator.
/// </summary>
/// <remarks>
/// <para>
/// Provides a hierarchical structure for complex filtering scenarios with nested conditions.
/// Each group contains a list of individual filter parameters and can also contain nested
/// groups for representing complex logical expressions.
/// </para>
/// <para>
/// Filter groups can be combined with different logical operators (AND, OR, NOT) to build
/// sophisticated query conditions that map to SQL WHERE clauses.
/// </para>
/// </remarks>
public class FilterGroup
{
    /// <summary>
    /// The logical operator to apply when combining filter conditions within this group.
    /// </summary>
    /// <remarks>
    /// Defaults to AND, meaning all conditions in the group must be satisfied.
    /// Other options include OR (any condition may be satisfied) and NOT (negate the group).
    /// </remarks>
    public LogicalOperator LogicalOperator { get; set; } = LogicalOperator.And;

    /// <summary>
    /// The list of individual filter conditions contained in this group.
    /// </summary>
    /// <remarks>
    /// Each FilterParameter represents a single condition on a specific field.
    /// These conditions are combined using the logical operator specified for the group.
    /// </remarks>
    public List<FilterParameter> Filters { get; set; } = [];

    /// <summary>
    /// Nested filter groups that can be used to create complex logical expressions.
    /// </summary>
    /// <remarks>
    /// Allows for hierarchical grouping of conditions, similar to parentheses in logical expressions.
    /// Each nested group has its own logical operator that applies to its own conditions and sub-groups.
    /// </remarks>
    public List<FilterGroup> Groups { get; set; } = [];
}

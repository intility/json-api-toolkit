namespace JsonApiToolkit.Models.Querying.Filtering;

/// <summary>
/// Logical operators for combining filter conditions.
/// </summary>
public enum LogicalOperator
{
    /// <summary>All conditions must be true.</summary>
    And,

    /// <summary>At least one condition must be true.</summary>
    Or,

    /// <summary>Negates the condition.</summary>
    Not,
}

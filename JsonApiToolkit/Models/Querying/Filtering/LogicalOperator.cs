namespace JsonApiToolkit.Models.Querying.Filtering;

/// <summary>
/// Defines logical operators for combining multiple filter conditions.
/// </summary>
/// <remarks>
/// These operators determine how multiple conditions within a filter group are combined.
/// They correspond to the SQL logical operators AND, OR, and NOT, and are used to build
/// complex filtering expressions.
/// </remarks>
public enum LogicalOperator
{
    /// <summary>
    /// Logical AND (default).
    /// </summary>
    And,

    /// <summary>
    /// Logical OR.
    /// </summary>
    Or,

    /// <summary>
    /// Logical NOT.
    /// </summary>
    Not,
}

namespace JsonApiToolkit.Models.Querying.Filtering;

/// <summary>
/// Represents a logical operator for combining filter parameters.
/// </summary>
/// <remarks>
/// Enum values include:
/// <list type="bullet">
///   <item>
///     <description><c>And</c> - Logical AND (default).</description>
///   </item>
///   <item>
///     <description><c>Or</c> - Logical OR.</description>
///   </item>
///   <item>
///     <description><c>Not</c> - Logical NOT.</description>
///   </item>
/// </list>
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

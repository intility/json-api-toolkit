namespace JsonApiToolkit.Models.Querying.Filtering;

/// <summary>
/// Defines comparison operators used in JSON:API filter expressions.
/// </summary>
/// <remarks>
/// These operators map to SQL comparison operators and determine how field values
/// are compared against filter values. They support a wide range of comparison types
/// from basic equality to range checks and pattern matching.
/// </remarks>
public enum FilterOperator
{
    /// <summary>
    /// Equal (default).
    /// </summary>
    Eq,

    /// <summary>
    /// Not equal.
    /// </summary>
    Ne,

    /// <summary>
    /// Greater than.
    /// </summary>
    Gt,

    /// <summary>
    /// Greater than or equal.
    /// </summary>
    Ge,

    /// <summary>
    /// Less than.
    /// </summary>
    Lt,

    /// <summary>
    /// Less than or equal.
    /// </summary>

    Le,

    /// <summary>
    /// Contains.
    /// </summary>
    Like,

    /// <summary>
    /// In list.
    /// </summary>
    In,

    /// <summary>
    /// Not in list.
    /// </summary>
    Nin,

    /// <summary>
    /// Is null.
    /// </summary>
    IsNull,

    /// <summary>
    /// Is not null.
    /// </summary>
    IsNotNull,
}

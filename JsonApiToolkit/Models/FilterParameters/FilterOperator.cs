namespace JsonApiToolkit.Models.FilterParameters;

/// <summary>
/// Specifies the filter operator used to define comparison operations in filtering expressions.
/// </summary>
/// <remarks>
/// Enum values include:
/// <list type="bullet">
///   <item>
///     <description><c>Eq</c> - Equal (default).</description>
///   </item>
///   <item>
///     <description><c>Ne</c> - Not equal.</description>
///   </item>
///   <item>
///     <description><c>Gt</c> - Greater than.</description>
///   </item>
///   <item>
///     <description><c>Ge</c> - Greater than or equal.</description>
///   </item>
///   <item>
///     <description><c>Lt</c> - Less than.</description>
///   </item>
///   <item>
///     <description><c>Le</c> - Less than or equal.</description>
///   </item>
///   <item>
///     <description><c>Like</c> - Contains.</description>
///   </item>
///   <item>
///     <description><c>In</c> - In list.</description>
///   </item>
///   <item>
///     <description><c>Nin</c> - Not in list.</description>
///   </item>
///   <item>
///     <description><c>IsNull</c> - Is null.</description>
///   </item>
///   <item>
///     <description><c>IsNotNull</c> - Is not null.</description>
///   </item>
/// </list>
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

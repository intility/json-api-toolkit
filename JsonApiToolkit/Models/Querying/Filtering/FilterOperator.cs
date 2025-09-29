namespace JsonApiToolkit.Models.Querying.Filtering;

/// <summary>
/// Comparison operators for filter expressions.
/// </summary>
public enum FilterOperator
{
    /// <summary>Equal to.</summary>
    Eq,
    /// <summary>Not equal to.</summary>
    Ne,
    /// <summary>Greater than.</summary>
    Gt,
    /// <summary>Greater than or equal to.</summary>
    Ge,
    /// <summary>Less than.</summary>
    Lt,
    /// <summary>Less than or equal to.</summary>
    Le,
    /// <summary>String contains (case-insensitive).</summary>
    Like,
    /// <summary>In list of values.</summary>
    In,
    /// <summary>Not in list of values.</summary>
    Nin,
    /// <summary>Is null.</summary>
    IsNull,
    /// <summary>Is not null.</summary>
    IsNotNull,
}

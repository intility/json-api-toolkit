namespace JsonApiToolkit.Models.Querying.Filtering;

/// <summary>
/// Represents a single filter condition specifying a field, operator, and comparison value.
/// </summary>
/// <remarks>
/// <para>
/// Filter parameters are the basic building blocks of the filtering system. Each parameter
/// represents a condition like "age >= 18" or "name LIKE 'Smith'".
/// </para>
/// <para>
/// Filter parameters are typically combined in filter groups using logical operators.
/// </para>
/// </remarks>
public class FilterParameter
{
    /// <summary>
    /// The name of the entity field to filter on.
    /// </summary>
    /// <remarks>
    /// Can refer to direct entity properties or, in some cases, nested properties using dot notation
    /// (e.g., "user.address.city"). The field name is typically the JSON property name (camelCase).
    /// </remarks>
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// The comparison operator to apply in the filter condition.
    /// </summary>
    /// <remarks>
    /// Defines how the field value should be compared against the filter value.
    /// Default is equality (Eq). Other options include greater than, less than,
    /// pattern matching, and existence checks.
    /// </remarks>
    public FilterOperator Operator { get; set; } = FilterOperator.Eq;

    /// <summary>
    /// The value to compare against the field value.
    /// </summary>
    /// <remarks>
    /// The string representation of the comparison value. Will be converted to the appropriate
    /// type based on the field's type during filter application. For 'in' and 'nin' operators,
    /// this can be a comma-separated list of values.
    /// </remarks>
    public string Value { get; set; } = string.Empty;
}

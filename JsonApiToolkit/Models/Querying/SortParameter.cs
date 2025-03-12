namespace JsonApiToolkit.Models.Querying;

/// <summary>
/// Represents a sort criterion specifying a field and direction for ordering results.
/// </summary>
/// <remarks>
/// Each sort parameter defines how a collection should be sorted by a specific field.
/// Multiple sort parameters can be combined in priority order to define complex sorting.
/// </remarks>
public class SortParameter
{
    /// <summary>
    /// The name of the field to sort by.
    /// </summary>
    /// <remarks>
    /// Corresponds to the field name in the sort query parameter.
    /// Field names typically match JSON property names (camelCase).
    /// </remarks>
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether to sort in descending order.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <list type="bullet">
    /// <item>
    /// <description>When true, sorts in descending order (high to low).</description>
    /// </item>
    /// <item>
    /// <description>When false, sorts in ascending order (low to high).</description>
    /// </item>
    /// </list>
    /// </para>
    /// <para>
    /// In JSON:API query parameters, descending sort is indicated by a minus prefix on the field name.
    /// </para>
    /// </remarks>
    public bool IsDescending { get; set; }
}

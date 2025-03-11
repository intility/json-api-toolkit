namespace JsonApiToolkit.Models.Querying;

/// <summary>
/// Represents a sort parameter.
/// </summary>
public class SortParameter
{
    /// <summary>
    /// The field to sort by.
    /// </summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// Whether to sort in descending order.
    /// </summary>
    public bool IsDescending { get; set; }
}

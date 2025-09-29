namespace JsonApiToolkit.Models.Querying;

/// <summary>
/// Sort criterion with field and direction.
/// </summary>
public class SortParameter
{
    /// <summary>
    /// Field to sort by.
    /// </summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// Sort in descending order (true) or ascending (false).
    /// </summary>
    public bool IsDescending { get; set; }
}

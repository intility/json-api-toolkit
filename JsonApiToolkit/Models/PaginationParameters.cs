namespace JsonApiToolkit.Models;

/// <summary>
/// Represents pagination parameters.
/// </summary>
public class PaginationParameters
{
    /// <summary>
    /// The page number.
    /// </summary>
    public int Number { get; set; } = 1;

    /// <summary>
    /// The page size.
    /// </summary>
    public int Size { get; set; } = 10;
}

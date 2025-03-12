using System.Text.Json.Serialization;
using JsonApiToolkit.Models.Documents;
using JsonApiToolkit.Models.Metadata;

namespace JsonApiToolkit.Models.Resources;

/// <summary>
/// Represents a relationship between resources in a JSON:API document.
/// </summary>
/// <remarks>
/// <para>
/// Relationships in JSON:API can be to-one (a single resource identifier) or to-many
/// (an array of resource identifiers). This class supports both types through its Data property.
/// </para>
/// <para>
/// Relationships may also include links to related resources and relationship manipulation endpoints.
/// </para>
/// </remarks>
public class Relationship
{
    /// <summary>
    /// Resource linkage defining the related resource(s).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Can be:
    /// <list type="bullet">
    /// <item>
    /// <description>null (for empty to-one relationships)</description>
    /// </item>
    /// <item>
    /// <description>A single ResourceIdentifier object (for to-one relationships)</description>
    /// </item>
    /// <item>
    /// <description>A collection of ResourceIdentifier objects (for to-many relationships)</description>
    /// </item>
    /// </list>
    /// </para>
    /// <para>
    /// Resource identifiers contain only the type and id of the related resources.
    /// </para>
    /// </remarks>
    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Data { get; set; }

    /// <summary>
    /// Links related to this relationship.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Typically includes:
    /// <list type="bullet">
    /// <item>
    /// <description>"self": Link to the relationship itself (for manipulation)</description>
    /// </item>
    /// <item>
    /// <description>"related": Link to the related resource(s)</description>
    /// </item>
    /// </list>
    /// </para>
    /// <para>
    /// These links allow clients to navigate and manipulate relationships without constructing URLs manually.
    /// </para>
    /// </remarks>
    [JsonPropertyName("links")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Links? Links { get; set; }
}

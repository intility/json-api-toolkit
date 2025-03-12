using System.Text.Json.Serialization;
using JsonApiToolkit.Models.Documents;
using JsonApiToolkit.Models.Metadata;

namespace JsonApiToolkit.Models.Resources;

/// <summary>
/// Represents a resource object in a JSON:API document, containing a resource's identity, attributes, and relationships.
/// </summary>
/// <remarks>
/// <para>
/// Resource objects are the primary data structures in JSON:API responses. They encapsulate
/// the identity, attributes, and relationships of domain entities in a standardized format.
/// </para>
/// <para>
/// Every resource object must contain at least a type and id. Attributes and relationships are optional.
/// </para>
/// </remarks>
public class ResourceObject
{
    /// <summary>
    /// The unique identifier of the resource within its type.
    /// </summary>
    /// <remarks>
    /// Combined with the type, forms a globally unique identifier for the resource.
    /// Must be a string, even if the underlying ID is numeric.
    /// </remarks>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The JSON:API resource type.
    /// </summary>
    /// <remarks>
    /// Identifies the type of resource. Typically corresponds to an entity type in the system
    /// and follows the JSON:API naming convention (usually camelCase).
    /// </remarks>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// A dictionary of resource attributes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Contains all the resource's attributes as key-value pairs. Keys are attribute names (camelCase)
    /// and values are the attribute values, which may be of any JSON-compatible type.
    /// </para>
    /// <para>
    /// Attributes represent information directly associated with the resource, rather than relationships
    /// to other resources.
    /// </para>
    /// </remarks>
    [JsonPropertyName("attributes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Attributes { get; set; }

    /// <summary>
    /// A dictionary of resource relationships.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Contains all the resource's relationships as key-value pairs. Keys are relationship names (camelCase)
    /// and values are Relationship objects that define the linkage to other resources.
    /// </para>
    /// <para>
    /// Relationships represent connections between resources and can be to-one or to-many.
    /// </para>
    /// </remarks>
    [JsonPropertyName("relationships")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, Relationship>? Relationships { get; set; }

    /// <inheritdoc cref="JsonApiDocument{T}.Links"/>
    [JsonPropertyName("links")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Links? Links { get; set; }
}

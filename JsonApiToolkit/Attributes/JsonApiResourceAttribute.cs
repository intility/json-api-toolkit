namespace JsonApiToolkit.Attributes;

/// <summary>
/// Declares the JSON:API wire type string for a resource and marks it for
/// TypeScript generation by the jsonapi-typegen tool.
/// </summary>
/// <param name="typeName">The JSON:API wire type string.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class JsonApiResourceAttribute(string typeName) : Attribute
{
    /// <summary>
    /// The JSON:API "type" string used on the wire (e.g. "todos").
    /// </summary>
    public string TypeName { get; } = typeName;
}

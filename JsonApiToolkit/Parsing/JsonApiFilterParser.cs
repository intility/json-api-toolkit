using JsonApiToolkit.Models.Querying.Filtering;
using Microsoft.AspNetCore.Http;

namespace JsonApiToolkit.Parsing;

/// <summary>
/// Parses JSON:API filter query string parameters into structured filter objects.
/// </summary>
/// <remarks>
/// Handles complex filtering syntax, logical operators, and nested filter groups.
/// Supports both simple filters and advanced filter syntax with operators, logical groups, and nested conditions.
/// </remarks>
public static class JsonApiFilterParser
{
    /// <summary>
    /// The separator used to split complex filter query parameters.
    /// </summary>
    public static readonly string[] s_separator = ["]["];

    /// <summary>
    /// Parses a filter operator string to the corresponding FilterOperator enum value.
    /// </summary>
    /// <param name="operatorStr">The operator string to parse</param>
    /// <returns>The corresponding FilterOperator enum value, defaults to Eq if not recognized</returns>
    private static FilterOperator ParseFilterOperator(string operatorStr)
    {
        return operatorStr.ToLowerInvariant() switch
        {
            "eq" => FilterOperator.Eq,
            "ne" => FilterOperator.Ne,
            "gt" => FilterOperator.Gt,
            "ge" => FilterOperator.Ge,
            "lt" => FilterOperator.Lt,
            "le" => FilterOperator.Le,
            "like" => FilterOperator.Like,
            "in" => FilterOperator.In,
            "nin" => FilterOperator.Nin,
            "isnull" => FilterOperator.IsNull,
            "isnotnull" => FilterOperator.IsNotNull,
            _ => FilterOperator.Eq,
        };
    }

    /// <summary>
    /// Parses a complex filter query parameter into a structured filter parameter and adds it to a filter group.
    /// </summary>
    /// <param name="key">The query parameter key (e.g., "filter[name][eq]")</param>
    /// <param name="value">The query parameter value</param>
    /// <param name="group">The filter group to add the parsed filter to</param>
    /// <remarks>
    /// <para>
    /// Handles filter syntax in the format "filter[field][operator]" where:
    /// <list type="bullet">
    /// <item>
    /// <description>field is the property name to filter on</description>
    /// </item>
    /// <item>
    /// <description>operator is one of: eq, ne, gt, ge, lt, le, like, in, nin, isnull, isnotnull</description>
    /// </item>
    /// </list>
    /// </para>
    /// <para>
    /// Extracts the field name and operator from the key and creates a FilterParameter with the
    /// appropriate field, operator, and value, then adds it to the provided filter group.
    /// </para>
    /// </remarks>
    public static void ParseComplexFilter(string key, string value, FilterGroup group)
    {
        string[] keyParts = key.Substring(7, key.Length - 8).Split("][");
        if (keyParts.Length != 2)
            return;

        string field = keyParts[0];
        string operatorStr = keyParts[1].ToLowerInvariant();

        var parameter = new FilterParameter
        {
            Field = field,
            Value = value,
            Operator = ParseFilterOperator(operatorStr),
        };

        group.Filters.Add(parameter);
    }

    /// <summary>
    /// Parses a logical grouping of filters (AND, OR, NOT) from query parameters.
    /// </summary>
    /// <param name="request">The HTTP request containing the query parameters.</param>
    /// <param name="groupName">
    /// The name of the logical group. For example, "or" or "not". This name indicates how the filters within
    /// this group should be combined logically.
    /// </param>
    /// <param name="op">
    /// The logical operator (AND, OR, NOT) to apply to the entire group of filters.
    /// </param>
    /// <param name="parentGroup">
    /// The parent filter group to which this new logical group will be added.
    /// </param>
    /// <remarks>
    /// <para>
    /// This method handles special naming patterns in filter query parameters used to group multiple filter
    /// conditions together. The syntax uses indices (e.g., [0], [1]) to differentiate individual conditions
    /// within a logical group.
    /// </para>
    /// <para>
    /// For example, consider the following query parameters:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       filter[or][0][name]=Alice
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       filter[or][1][age][gt]=18
    ///     </description>
    ///   </item>
    /// </list>
    /// <para>
    /// In this example:
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       The group name "or" indicates that the two conditions should be combined using a logical OR.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       The index [0] identifies the first condition (name equals "Alice"), and [1] identifies the second condition
    ///       (age greater than 18).
    ///     </description>
    ///   </item>
    /// </list>
    /// </para>
    /// <para>
    /// The indices are essential because they allow multiple filter conditions to be grouped under the same logical operator.
    /// Without these indices, the parser would not know how many conditions belong to the group nor how to separate them.
    /// </para>
    /// <para>
    /// You can also have multiple logical groups in the same request. For example:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       filter[or][0][name]=Alice
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       filter[or][1][name]=Bob
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       filter[not][0][status]=inactive
    ///     </description>
    ///   </item>
    /// </list>
    /// <para>
    /// In this example:
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       The "or" group requires that either the name is "Alice" or "Bob".
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       The "not" group excludes resources where the status is "inactive".
    ///     </description>
    ///   </item>
    /// </list>
    /// </para>
    /// </remarks>
    public static void ParseLogicalGroup(
        HttpRequest request,
        string groupName,
        LogicalOperator op,
        FilterGroup parentGroup
    )
    {
        var orKeys = request.Query.Keys.Where(k => k.StartsWith($"filter[{groupName}][")).ToList();

        if (orKeys.Count == 0)
            return;

        var newGroup = new FilterGroup { LogicalOperator = op };

        var indexGroups = orKeys
            .Select(k => new
            {
                Key = k,
                Index = int.Parse(k.Substring($"filter[{groupName}][".Length).Split(']')[0]),
            })
            .GroupBy(x => x.Index);

        foreach (var indexGroup in indexGroups)
        {
            // Create a new FilterParameter for each filter in the group
            foreach (var item in indexGroup)
            {
                var condition = new FilterParameter();

                string restOfKey = item.Key.Substring(
                    $"filter[{groupName}][{indexGroup.Key}][".Length
                );

                if (restOfKey.Contains(s_separator[0]))
                {
                    string[] parts = restOfKey.Split(s_separator, StringSplitOptions.None);
                    condition.Field = parts[0];
                    condition.Operator = ParseFilterOperator(parts[1].TrimEnd(']'));
                }
                else
                {
                    condition.Field = restOfKey.TrimEnd(']');
                    condition.Operator = FilterOperator.Eq;
                }

                condition.Value = request.Query[item.Key].ToString();

                // Add each condition to the group
                newGroup.Filters.Add(condition);
            }
        }

        if (newGroup.Filters.Count > 0)
            parentGroup.Groups.Add(newGroup);
    }
}

using JsonApiToolkit.Models.Querying.Filtering;
using Microsoft.AspNetCore.Http;

namespace JsonApiToolkit.Parsing;

/// <summary>
/// Parses filter query parameters with complex syntax, operators, and logical groups.
/// </summary>
public static class JsonApiFilterParser
{
    /// <summary>
    /// Separator used for parsing filter syntax.
    /// </summary>
    public static readonly string[] s_separator = ["]["];

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
    /// Parses filter[field][operator]=value syntax.
    /// </summary>
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
    /// Parses filter[or][0][field]=value or filter[not][0][field]=value syntax.
    /// </summary>
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

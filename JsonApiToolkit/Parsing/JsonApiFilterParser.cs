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
    /// Parses filter syntax supporting both:
    /// - Primary filter: filter[field][operator]=value or filter[rel.field][operator]=value (dot notation)
    /// - Include filter: filter[rel][field][operator]=value (bracket syntax for filtering included relationships)
    /// </summary>
    public static void ParseComplexFilter(string key, string value, FilterGroup group)
    {
        string[] keyParts = key.Substring(7, key.Length - 8).Split("][");

        // Standard primary filter: filter[field][operator]=value
        if (keyParts.Length == 2)
        {
            string field = keyParts[0];
            string operatorStr = keyParts[1].ToLowerInvariant();

            var parameter = new FilterParameter
            {
                Field = field,
                Value = value,
                Operator = ParseFilterOperator(operatorStr),
                IsIncludeFilter = false, // Dot notation = primary filter
            };

            group.Filters.Add(parameter);
            return;
        }

        // Include filter syntax: filter[rel][field][operator]=value (3 parts)
        if (keyParts.Length == 3)
        {
            string relationship = keyParts[0];
            string field = keyParts[1];
            string operatorStr = keyParts[2].ToLowerInvariant();

            var parameter = new FilterParameter
            {
                Field = $"{relationship}.{field}", // Combine for downstream processing
                Value = value,
                Operator = ParseFilterOperator(operatorStr),
                IsIncludeFilter = true, // Bracket syntax = include filter
            };

            group.Filters.Add(parameter);
        }
    }

    /// <summary>
    /// Parses filter[or][0][field]=value or filter[not][0][field]=value syntax.
    /// Supports both:
    /// - Primary filter: filter[or][0][rel.field][op]=value (dot notation)
    /// - Include filter: filter[or][0][rel][field][op]=value (bracket syntax)
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

                string[] parts = restOfKey.Split(s_separator, StringSplitOptions.None);

                if (parts.Length == 2)
                {
                    // Standard: filter[or][0][field][op]=value or filter[or][0][rel.field][op]=value
                    condition.Field = parts[0];
                    condition.Operator = ParseFilterOperator(parts[1].TrimEnd(']'));
                    condition.IsIncludeFilter = false; // Dot notation = primary filter
                }
                else if (parts.Length == 3)
                {
                    // Include filter: filter[or][0][rel][field][op]=value
                    string relationship = parts[0];
                    string field = parts[1];
                    condition.Field = $"{relationship}.{field}";
                    condition.Operator = ParseFilterOperator(parts[2].TrimEnd(']'));
                    condition.IsIncludeFilter = true; // Bracket syntax = include filter
                }
                else if (parts.Length == 1)
                {
                    // Simple: filter[or][0][field]=value (implicit eq)
                    condition.Field = restOfKey.TrimEnd(']');
                    condition.Operator = FilterOperator.Eq;
                    condition.IsIncludeFilter = false;
                }
                else
                {
                    // Unsupported format, skip
                    continue;
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

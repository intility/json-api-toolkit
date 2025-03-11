using JsonApiToolkit.Models.FilterParameters;
using Microsoft.AspNetCore.Http;

namespace JsonApiToolkit.Parsing;

/// <summary>
/// Parses JSON:API filter query parameters into filter groups.
/// </summary>
public static class JsonApiFilterParser
{
    /// <summary>
    /// Parses complex filter query parameters into a filter group.
    /// </summary>
    /// <param name="key">The query parameter key.</param>
    /// <param name="value">The query parameter value.</param>
    /// <param name="group">The filter group to add the filter to.</param>
    public static void ParseComplexFilter(string key, string value, FilterGroup group)
    {
        // Parse filter[field][operator]=value
        var keyParts = key.Substring(7, key.Length - 8).Split("][");
        if (keyParts.Length != 2)
            return;

        var field = keyParts[0];
        var operatorStr = keyParts[1].ToLowerInvariant();

        var parameter = new FilterParameter { Field = field, Value = value };

        // Map operator string to enum
        parameter.Operator = operatorStr switch
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

        group.Filters.Add(parameter);
    }

    /// <summary>
    /// Parses logical group filter query parameters into a filter group.
    /// </summary>
    /// <param name="request">The HTTP request.</param>
    /// <param name="groupName">The name of the group.</param>
    /// <param name="op">The logical operator for the group.</param>
    /// <param name="parentGroup">The parent filter group to add the new group to.</param>
    public static void ParseLogicalGroup(
        HttpRequest request,
        string groupName,
        LogicalOperator op,
        FilterGroup parentGroup
    )
    {
        // Look for patterns like filter[or][0][field]=value
        var orKeys = request.Query.Keys.Where(k => k.StartsWith($"filter[{groupName}][")).ToList();

        if (orKeys.Count == 0)
            return;

        var newGroup = new FilterGroup { LogicalOperator = op };

        // Group keys by the index in the array
        var indexGroups = orKeys
            .Select(k => new
            {
                Key = k,
                Index = int.Parse(k.Substring($"filter[{groupName}][".Length).Split(']')[0]),
            })
            .GroupBy(x => x.Index);

        foreach (var indexGroup in indexGroups)
        {
            var condition = new FilterParameter();

            foreach (var item in indexGroup)
            {
                // Extract the field and optional operator
                var restOfKey = item.Key.Substring(
                    $"filter[{groupName}][{indexGroup.Key}][".Length
                );

                if (restOfKey.Contains("][")) // Has operator
                {
                    var parts = restOfKey.Split(new[] { "][" }, StringSplitOptions.None);
                    condition.Field = parts[0];
                    condition.Operator = parts[1].TrimEnd(']').ToLowerInvariant() switch
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
                else // No operator
                {
                    condition.Field = restOfKey.TrimEnd(']');
                    condition.Operator = FilterOperator.Eq;
                }

                condition.Value = request.Query[item.Key].ToString();
            }

            newGroup.Filters.Add(condition);
        }

        if (newGroup.Filters.Count > 0)
        {
            parentGroup.Groups.Add(newGroup);
        }
    }
}

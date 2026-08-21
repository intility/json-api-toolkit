using JsonApiToolkit.Models.Errors;
using JsonApiToolkit.Models.Querying.Filtering;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

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

    /// <summary>
    /// Minimum length for a valid filter key: "filter[x]" = 9 characters.
    /// </summary>
    private const int MinFilterKeyLength = 9;

    private static readonly string[] s_validOperators =
    [
        "eq",
        "ne",
        "gt",
        "ge",
        "lt",
        "le",
        "like",
        "in",
        "nin",
        "isnull",
        "isnotnull",
    ];

    private static bool IsGroupName(string segment) =>
        segment.Equals("and", StringComparison.OrdinalIgnoreCase)
        || segment.Equals("or", StringComparison.OrdinalIgnoreCase)
        || segment.Equals("not", StringComparison.OrdinalIgnoreCase);

    private static JsonApiBadRequestException MalformedFilterKey(string key) =>
        new(
            $"Malformed filter parameter: '{key}'.",
            JsonApiErrorCodes.ValidationFailed,
            new ErrorSource { Parameter = key }
        );

    /// <summary>
    /// Validates that a filter key has the expected format: filter[...]
    /// </summary>
    private static bool IsValidFilterKey(string key) =>
        key.Length >= MinFilterKeyLength && key.StartsWith("filter[") && key.EndsWith("]");

    private static FilterOperator ParseFilterOperator(
        string operatorStr,
        bool strictValidation = false
    )
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
            _ => strictValidation
                ? throw JsonApiErrors.InvalidFilterOperator(operatorStr, s_validOperators)
                : FilterOperator.Eq,
        };
    }

    /// <summary>
    /// Parses filter syntax supporting both:
    /// - Primary filter: filter[field][operator]=value or filter[rel.field][operator]=value (dot notation)
    /// - Include filter: filter[rel][field][operator]=value (bracket syntax for filtering included relationships)
    /// </summary>
    public static void ParseComplexFilter(
        string key,
        string value,
        FilterGroup group,
        ILogger? logger = null,
        bool strictValidation = false
    )
    {
        if (!IsValidFilterKey(key))
        {
            if (strictValidation)
                throw MalformedFilterKey(key);

            logger?.LogWarning("Malformed filter key ignored: {Key}", key);
            return;
        }

        string[] keyParts = key[7..^1].Split("][");

        // Standard primary filter: filter[field][operator]=value
        if (keyParts.Length == 2)
        {
            string field = keyParts[0];
            string operatorStr = keyParts[1].ToLowerInvariant();

            var parameter = new FilterParameter
            {
                Field = field,
                Value = value,
                Operator = ParseFilterOperator(operatorStr, strictValidation),
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
                Operator = ParseFilterOperator(operatorStr, strictValidation),
                IsIncludeFilter = true, // Bracket syntax = include filter
            };

            group.Filters.Add(parameter);
            return;
        }

        // 4+ segments: not a supported filter shape (silently ignored by default)
        if (strictValidation)
            throw MalformedFilterKey(key);
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
        FilterGroup parentGroup,
        ILogger? logger = null,
        bool strictValidation = false
    )
    {
        string prefix = $"filter[{groupName}][";
        var groupKeys = request.Query.Keys.Where(k => k.StartsWith(prefix)).ToList();

        if (groupKeys.Count == 0)
            return;

        var newGroup = new FilterGroup { LogicalOperator = op };

        var indexGroups = groupKeys
            .Select(k => TryParseGroupIndex(k, prefix, logger, strictValidation))
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .GroupBy(x => x.Index);

        foreach (var indexGroup in indexGroups)
        {
            string itemPrefix = $"{prefix}{indexGroup.Key}][";

            foreach (var item in indexGroup)
            {
                // Validate key length before substring
                if (item.Key.Length <= itemPrefix.Length)
                {
                    if (strictValidation)
                        throw MalformedFilterKey(item.Key);

                    logger?.LogWarning(
                        "Malformed logical group filter key ignored: {Key}",
                        item.Key
                    );
                    continue;
                }

                string restOfKey = item.Key[itemPrefix.Length..];
                string[] parts = restOfKey.Split(s_separator, StringSplitOptions.None);

                var condition = new FilterParameter();

                if (parts.Length == 2)
                {
                    // Standard: filter[or][0][field][op]=value or filter[or][0][rel.field][op]=value
                    condition.Field = parts[0];
                    condition.Operator = ParseFilterOperator(
                        parts[1].TrimEnd(']'),
                        strictValidation
                    );
                    condition.IsIncludeFilter = false; // Dot notation = primary filter
                }
                else if (parts.Length == 3)
                {
                    // Include filter: filter[or][0][rel][field][op]=value
                    string relationship = parts[0];
                    string field = parts[1];

                    if (strictValidation && IsGroupName(relationship))
                    {
                        throw JsonApiErrors.UnsupportedFilterGroup(
                            $"Nested filter groups are not supported: '{item.Key}'.",
                            item.Key
                        );
                    }

                    condition.Field = $"{relationship}.{field}";
                    condition.Operator = ParseFilterOperator(
                        parts[2].TrimEnd(']'),
                        strictValidation
                    );
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
                    if (strictValidation)
                    {
                        throw JsonApiErrors.UnsupportedFilterGroup(
                            $"Unsupported filter group syntax: '{item.Key}'. "
                                + "Nested filter groups are not supported.",
                            item.Key
                        );
                    }

                    // Unsupported format, skip
                    logger?.LogWarning(
                        "Unsupported logical group filter format ignored: {Key}",
                        item.Key
                    );
                    continue;
                }

                condition.Value = request.Query[item.Key].ToString();
                newGroup.Filters.Add(condition);
            }
        }

        if (newGroup.Filters.Count > 0)
            parentGroup.Groups.Add(newGroup);
    }

    /// <summary>
    /// Safely extracts the group index from a logical group filter key.
    /// Returns null if the key is malformed.
    /// </summary>
    private static (string Key, int Index)? TryParseGroupIndex(
        string key,
        string prefix,
        ILogger? logger,
        bool strictValidation = false
    )
    {
        if (key.Length <= prefix.Length)
        {
            if (strictValidation)
                throw MalformedFilterKey(key);

            logger?.LogWarning("Malformed logical group filter key ignored: {Key}", key);
            return null;
        }

        string afterPrefix = key[prefix.Length..];
        int closeBracketIndex = afterPrefix.IndexOf(']');

        if (closeBracketIndex <= 0)
        {
            if (strictValidation)
                throw MalformedFilterKey(key);

            logger?.LogWarning("Malformed logical group filter key ignored: {Key}", key);
            return null;
        }

        string indexStr = afterPrefix[..closeBracketIndex];

        if (!int.TryParse(indexStr, out int index))
        {
            if (strictValidation)
                throw MalformedFilterKey(key);

            logger?.LogWarning(
                "Invalid group index '{IndexStr}' in filter key ignored: {Key}",
                indexStr,
                key
            );
            return null;
        }

        return (key, index);
    }
}

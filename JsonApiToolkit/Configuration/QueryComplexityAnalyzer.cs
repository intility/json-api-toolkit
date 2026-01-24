using JsonApiToolkit.Models.Errors;
using JsonApiToolkit.Models.Querying;
using JsonApiToolkit.Models.Querying.Filtering;

namespace JsonApiToolkit.Configuration;

/// <summary>
/// Analyzes query complexity and validates against configured limits.
/// </summary>
public static class QueryComplexityAnalyzer
{
    /// <summary>
    /// Validates query parameters against configured limits.
    /// Throws JsonApiBadRequestException if any limit is exceeded.
    /// </summary>
    public static void Validate(QueryParameters parameters, JsonApiOptions options)
    {
        ValidateFilters(parameters.Filter, options);
        ValidateIncludes(parameters.Include, options);
    }

    private static void ValidateFilters(FilterGroup? filterGroup, JsonApiOptions options)
    {
        if (filterGroup is null)
            return;

        // Count total filters
        int totalFilters = CountFilters(filterGroup);
        if (totalFilters > options.MaxFilters)
        {
            throw JsonApiErrors.QueryTooComplex(
                "filters",
                options.MaxFilters,
                totalFilters,
                "JsonApiOptions.MaxFilters"
            );
        }

        // Count filter groups
        int totalGroups = CountGroups(filterGroup);
        if (totalGroups > options.MaxFilterGroups)
        {
            throw JsonApiErrors.QueryTooComplex(
                "filter groups",
                options.MaxFilterGroups,
                totalGroups,
                "JsonApiOptions.MaxFilterGroups"
            );
        }

        // Check filter depth
        int maxDepth = GetMaxDepth(filterGroup);
        if (maxDepth > options.MaxFilterDepth)
        {
            throw JsonApiErrors.QueryTooComplex(
                "filter nesting depth",
                options.MaxFilterDepth,
                maxDepth,
                "JsonApiOptions.MaxFilterDepth"
            );
        }

        // Check filter value lengths
        ValidateFilterValueLengths(filterGroup, options.MaxFilterValueLength);
    }

    private static void ValidateIncludes(List<string>? includes, JsonApiOptions options)
    {
        if (includes is null || includes.Count == 0)
            return;

        foreach (var include in includes)
        {
            int depth = include.Count(c => c == '.') + 1;
            if (depth > options.MaxIncludeDepth)
            {
                throw new JsonApiBadRequestException(
                    $"Include path '{include}' has depth {depth}, but maximum allowed is {options.MaxIncludeDepth}. "
                        + "Reduce nesting or configure a higher limit via JsonApiOptions.MaxIncludeDepth.",
                    JsonApiErrorCodes.IncludeDepthExceeded,
                    new ErrorSource { Parameter = "include" },
                    new Dictionary<string, object>
                    {
                        ["includePath"] = include,
                        ["depth"] = depth,
                        ["limit"] = options.MaxIncludeDepth,
                        ["configKey"] = "JsonApiOptions.MaxIncludeDepth",
                    }
                );
            }
        }
    }

    /// <summary>
    /// Counts total number of filter conditions across all groups.
    /// </summary>
    public static int CountFilters(FilterGroup group)
    {
        int count = group.Filters.Count;
        foreach (var nested in group.Groups)
        {
            count += CountFilters(nested);
        }
        return count;
    }

    /// <summary>
    /// Counts total number of filter groups (excluding root).
    /// </summary>
    public static int CountGroups(FilterGroup group)
    {
        int count = group.Groups.Count;
        foreach (var nested in group.Groups)
        {
            count += CountGroups(nested);
        }
        return count;
    }

    /// <summary>
    /// Gets the maximum nesting depth of filter groups.
    /// </summary>
    public static int GetMaxDepth(FilterGroup group, int currentDepth = 1)
    {
        if (group.Groups.Count == 0)
            return currentDepth;

        int maxChildDepth = currentDepth;
        foreach (var nested in group.Groups)
        {
            int childDepth = GetMaxDepth(nested, currentDepth + 1);
            if (childDepth > maxChildDepth)
                maxChildDepth = childDepth;
        }
        return maxChildDepth;
    }

    private static void ValidateFilterValueLengths(FilterGroup group, int maxLength)
    {
        foreach (var filter in group.Filters)
        {
            if (filter.Value?.Length > maxLength)
            {
                throw new JsonApiBadRequestException(
                    $"Filter value for '{filter.Field}' is {filter.Value.Length} characters, "
                        + $"but maximum allowed is {maxLength}. "
                        + "Reduce value length or configure a higher limit via JsonApiOptions.MaxFilterValueLength.",
                    JsonApiErrorCodes.QueryTooComplex,
                    new ErrorSource { Parameter = $"filter[{filter.Field}]" },
                    new Dictionary<string, object>
                    {
                        ["field"] = filter.Field,
                        ["valueLength"] = filter.Value.Length,
                        ["limit"] = maxLength,
                        ["configKey"] = "JsonApiOptions.MaxFilterValueLength",
                    }
                );
            }
        }

        foreach (var nested in group.Groups)
        {
            ValidateFilterValueLengths(nested, maxLength);
        }
    }
}

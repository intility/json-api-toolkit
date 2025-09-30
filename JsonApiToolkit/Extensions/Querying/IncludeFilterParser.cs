using System.Text.RegularExpressions;
using JsonApiToolkit.Models.Errors;
using JsonApiToolkit.Models.Querying.Filtering;

namespace JsonApiToolkit.Extensions.Querying;

/// <summary>
/// Separates filters targeting included resources from main entity filters.
/// </summary>
public static class IncludeFilterParser
{
    private const int MaxIncludeFilterDepth = 3;
    private const int MaxIncludeFilters = 20;
    private const int MaxOrConditions = 10;

    /// <summary>
    /// Separates main filters from include filters.
    /// Validates that filtered relationships are included in the query.
    /// </summary>
    public static (
        FilterGroup? mainFilters,
        List<IncludeFilter> includeFilters
    ) SeparateIncludeFilters(FilterGroup? filters, List<string>? includePaths)
    {
        if (filters == null)
            return (null, new List<IncludeFilter>());

        var includeFilters = new List<IncludeFilter>();
        var normalizedIncludePaths = NormalizeIncludePaths(includePaths ?? new List<string>());

        var mainFilters = ExtractIncludeFilters(filters, normalizedIncludePaths, includeFilters);

        ValidateIncludeFilters(includeFilters, normalizedIncludePaths);

        return (mainFilters, includeFilters);
    }

    private static FilterGroup? ExtractIncludeFilters(
        FilterGroup group,
        HashSet<string> normalizedIncludePaths,
        List<IncludeFilter> includeFilters
    )
    {
        var newGroup = new FilterGroup { LogicalOperator = group.LogicalOperator };

        // Check OR conditions count
        if (group.LogicalOperator == LogicalOperator.Or && group.Filters.Count > MaxOrConditions)
        {
            throw new JsonApiBadRequestException(
                $"Too many OR conditions in filter group. Maximum allowed: {MaxOrConditions}"
            );
        }

        foreach (var filter in group.Filters)
        {
            if (
                IsIncludeFilter(
                    filter.Field,
                    normalizedIncludePaths,
                    out var relationshipPath,
                    out var fieldPath
                )
            )
            {
                includeFilters.Add(
                    new IncludeFilter
                    {
                        RelationshipPath = relationshipPath,
                        FieldPath = fieldPath,
                        Filter = filter,
                    }
                );
            }
            else
            {
                newGroup.Filters.Add(filter);
            }
        }

        foreach (var nestedGroup in group.Groups)
        {
            var processedNestedGroup = ExtractIncludeFilters(
                nestedGroup,
                normalizedIncludePaths,
                includeFilters
            );

            if (
                processedNestedGroup != null
                && (processedNestedGroup.Filters.Count > 0 || processedNestedGroup.Groups.Count > 0)
            )
            {
                newGroup.Groups.Add(processedNestedGroup);
            }
        }

        // Return null if the group is empty after extraction
        if (newGroup.Filters.Count == 0 && newGroup.Groups.Count == 0)
            return null;

        return newGroup;
    }

    private static bool IsIncludeFilter(
        string field,
        HashSet<string> normalizedIncludePaths,
        out string relationshipPath,
        out string fieldPath
    )
    {
        relationshipPath = string.Empty;
        fieldPath = string.Empty;

        if (!field.Contains('.'))
            return false;

        var parts = field.Split('.');

        // Check filter depth
        if (parts.Length > MaxIncludeFilterDepth + 1)
        {
            throw new JsonApiBadRequestException(
                $"Filter depth exceeds maximum allowed depth of {MaxIncludeFilterDepth} for field: {field}"
            );
        }

        // Try to match progressively longer relationship paths
        for (int i = parts.Length - 1; i >= 1; i--)
        {
            var potentialRelationship = string.Join(".", parts.Take(i));
            var normalizedPotential = ConvertKebabToCamelCase(potentialRelationship);

            // First check for exact match or prefix match
            var matchedPath = normalizedIncludePaths.FirstOrDefault(path =>
                path.Equals(normalizedPotential, StringComparison.OrdinalIgnoreCase)
                || path.StartsWith(normalizedPotential + ".", StringComparison.OrdinalIgnoreCase)
            );

            if (matchedPath != null)
            {
                relationshipPath = potentialRelationship;
                fieldPath = string.Join(".", parts.Skip(i));
                return true;
            }

            // Check if any include path ends with this potential relationship
            // This handles cases like: include=cve.cvecomments&filter[cvecomments.field]
            matchedPath = normalizedIncludePaths.FirstOrDefault(path =>
                path.EndsWith("." + normalizedPotential, StringComparison.OrdinalIgnoreCase)
            );

            if (matchedPath != null)
            {
                relationshipPath = matchedPath;
                fieldPath = string.Join(".", parts.Skip(i));
                return true;
            }
        }

        return false;
    }

    private static HashSet<string> NormalizeIncludePaths(List<string> includePaths)
    {
        var normalized = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in includePaths)
        {
            // Convert kebab-case to camelCase for comparison
            var normalizedPath = ConvertKebabToCamelCase(path);
            normalized.Add(normalizedPath);
        }

        return normalized;
    }

    private static string ConvertKebabToCamelCase(string kebabCase)
    {
        if (string.IsNullOrEmpty(kebabCase))
            return kebabCase;

        var parts = kebabCase.Split('.');
        var convertedParts = parts.Select(part =>
        {
            if (!part.Contains('-'))
                return part;

            var segments = part.Split('-');
            var result = segments[0].ToLowerInvariant();

            for (int i = 1; i < segments.Length; i++)
            {
                if (segments[i].Length > 0)
                {
                    result +=
                        char.ToUpperInvariant(segments[i][0])
                        + segments[i].Substring(1).ToLowerInvariant();
                }
            }

            return result;
        });

        return string.Join(".", convertedParts);
    }

    private static void ValidateIncludeFilters(
        List<IncludeFilter> includeFilters,
        HashSet<string> normalizedIncludePaths
    )
    {
        if (includeFilters.Count > MaxIncludeFilters)
        {
            throw new JsonApiBadRequestException(
                $"Too many include filters. Maximum allowed: {MaxIncludeFilters}"
            );
        }

        foreach (var includeFilter in includeFilters)
        {
            var normalizedRelationship = ConvertKebabToCamelCase(includeFilter.RelationshipPath);

            if (
                !normalizedIncludePaths.Any(path =>
                    path.Equals(normalizedRelationship, StringComparison.OrdinalIgnoreCase)
                    || path.StartsWith(
                        normalizedRelationship + ".",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
            )
            {
                throw new JsonApiBadRequestException(
                    $"Cannot filter on '{includeFilter.RelationshipPath}' - relationship must be included in the request"
                );
            }
        }
    }
}

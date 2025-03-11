using JsonApiToolkit.Models;
using JsonApiToolkit.Models.FilterParameters;
using Microsoft.AspNetCore.Http;

namespace JsonApiToolkit.Parsing;

/// <summary>
/// Parses JSON:API query parameters from HTTP requests.
/// </summary>
public static class JsonApiQueryParser
{
    /// <summary>
    /// Parses JSON:API query parameters from the HTTP request.
    /// </summary>
    /// <param name="request">The HTTP request.</param>
    /// <returns>The JSON:API query parameters.</returns>
    public static QueryParameters Parse(HttpRequest request)
    {
        var queryParams = new QueryParameters();

        // Parse pagination
        if (
            request.Query.TryGetValue("page[number]", out var pageNumber)
            && request.Query.TryGetValue("page[size]", out var pageSize)
        )
        {
            queryParams.Pagination = new PaginationParameters
            {
                Number = int.TryParse(pageNumber, out var num) ? Math.Max(1, num) : 1,
                Size = int.TryParse(pageSize, out var size) ? Math.Clamp(size, 1, 100) : 10,
            };
        }

        // Parse filters
        var filterGroup = new FilterGroup();

        // First look for simple filters: filter[field]=value
        foreach (var key in request.Query.Keys.Where(k => k.StartsWith("filter[")))
        {
            if (key.Contains("][")) // Complex filter like filter[field][operator]
            {
                JsonApiFilterParser.ParseComplexFilter(
                    key,
                    request.Query[key].ToString(),
                    filterGroup
                );
            }
            else // Simple filter like filter[field]
            {
                var field = key.Substring(7, key.Length - 8);
                filterGroup.Filters.Add(
                    new FilterParameter
                    {
                        Field = field,
                        Operator = FilterOperator.Eq,
                        Value = request.Query[key].ToString(),
                    }
                );
            }
        }

        // Check for OR conditions: filter[or][0][field]=value
        JsonApiFilterParser.ParseLogicalGroup(request, "or", LogicalOperator.Or, filterGroup);

        // Check for NOT conditions: filter[not][0][field]=value
        JsonApiFilterParser.ParseLogicalGroup(request, "not", LogicalOperator.Not, filterGroup);

        if (filterGroup.Filters.Count > 0 || filterGroup.Groups.Count > 0)
        {
            queryParams.Filter = filterGroup;
        }

        // Parse sort
        if (request.Query.TryGetValue("sort", out var sortValue))
        {
            var sortParams = new List<SortParameter>();
            foreach (var field in sortValue.ToString().Split(','))
            {
                if (string.IsNullOrWhiteSpace(field))
                    continue;

                bool isDescending = field.StartsWith(char.ToString('-'));
                string fieldName = isDescending ? field.Substring(1) : field;

                sortParams.Add(
                    new SortParameter { Field = fieldName, IsDescending = isDescending }
                );
            }

            if (sortParams.Count > 0)
            {
                queryParams.Sort = sortParams;
            }
        }

        // Parse include
        if (request.Query.TryGetValue("include", out var includeValue))
        {
            var includes = includeValue
                .ToString()
                .Split(',')
                .Where(i => !string.IsNullOrWhiteSpace(i))
                .Select(i => i.Trim()) // Trim whitespace
                .Select(i => char.ToUpper(i[0]) + i.Substring(1)) // Pascal case for property matching
                .ToList();

            if (includes.Count > 0)
            {
                queryParams.Include = includes;
            }
        }

        return queryParams;
    }
}

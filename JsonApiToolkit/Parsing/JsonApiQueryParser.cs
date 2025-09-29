using JsonApiToolkit.Extensions;
using JsonApiToolkit.Models.Querying;
using JsonApiToolkit.Models.Querying.Filtering;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace JsonApiToolkit.Parsing;

/// <summary>
/// Parses JSON:API query parameters: pagination, filtering, sorting, and includes.
/// </summary>
public static class JsonApiQueryParser
{
    private const int DEFAULT_PAGE_SIZE = 10;
    private const int MIN_PAGE_SIZE = 1;
    private const int MAX_PAGE_SIZE = 100;

    /// <summary>
    /// Parses JSON:API query parameters from an HTTP request.
    /// </summary>
    public static QueryParameters Parse(HttpRequest request)
    {
        var queryParams = new QueryParameters();

        // Check for pagination parameters - allow either or both to be specified
        bool hasPageNumber = request.Query.TryGetValue("page[number]", out StringValues pageNumber);
        bool hasPageSize = request.Query.TryGetValue("page[size]", out StringValues pageSize);

        if (hasPageNumber || hasPageSize)
        {
            queryParams.Pagination = new PaginationParameters
            {
                Number =
                    hasPageNumber && int.TryParse(pageNumber, out int num) ? Math.Max(1, num) : 1, // Default to page 1 if not specified or invalid
                Size =
                    hasPageSize && int.TryParse(pageSize, out int size)
                        ? Math.Clamp(size, MIN_PAGE_SIZE, MAX_PAGE_SIZE)
                        : DEFAULT_PAGE_SIZE, // Use default size if not specified or invalid
            };
        }

        var filterGroup = new FilterGroup();

        foreach (string? key in request.Query.Keys.Where(k => k.StartsWith("filter[")))
        {
            if (key.Contains(JsonApiFilterParser.s_separator[0]))
            {
                JsonApiFilterParser.ParseComplexFilter(
                    key,
                    request.Query[key].ToString(),
                    filterGroup
                );
            }
            else
            {
                string field = key.Substring(7, key.Length - 8);
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

        JsonApiFilterParser.ParseLogicalGroup(request, "or", LogicalOperator.Or, filterGroup);

        JsonApiFilterParser.ParseLogicalGroup(request, "not", LogicalOperator.Not, filterGroup);

        if (filterGroup.Filters.Count > 0 || filterGroup.Groups.Count > 0)
            queryParams.Filter = filterGroup;

        if (request.Query.TryGetValue("sort", out StringValues sortValue))
        {
            var sortParams = new List<SortParameter>();
            foreach (string field in sortValue.ToString().Split(','))
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

        if (request.Query.TryGetValue("include", out StringValues includeValue))
        {
            var includes = includeValue
                .ToString()
                .Split(',')
                .Where(i => !string.IsNullOrWhiteSpace(i))
                .Select(i => i.Trim())
                .ToList();

            if (includes.Count > 0)
            {
                queryParams.Include = includes;
            }
        }

        return queryParams;
    }
}

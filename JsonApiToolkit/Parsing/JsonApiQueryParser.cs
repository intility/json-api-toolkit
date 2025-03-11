using JsonApiToolkit.Models.Querying;
using JsonApiToolkit.Models.Querying.Filtering;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

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

        if (
            request.Query.TryGetValue("page[number]", out StringValues pageNumber)
            && request.Query.TryGetValue("page[size]", out StringValues pageSize)
        )
        {
            queryParams.Pagination = new PaginationParameters
            {
                Number = int.TryParse(pageNumber, out int num) ? Math.Max(1, num) : 1,
                Size = int.TryParse(pageSize, out int size) ? Math.Clamp(size, 1, 100) : 10,
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
                .Select(i => char.ToUpper(i[0]) + i.Substring(1))
                .ToList();

            if (includes.Count > 0)
            {
                queryParams.Include = includes;
            }
        }

        return queryParams;
    }
}

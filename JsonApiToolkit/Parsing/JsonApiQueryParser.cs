using JsonApiToolkit.Extensions;
using JsonApiToolkit.Models.Querying;
using JsonApiToolkit.Models.Querying.Filtering;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace JsonApiToolkit.Parsing;

/// <summary>
/// Parses JSON:API compliant query parameters from HTTP requests into structured query objects.
/// </summary>
/// <remarks>
/// <para>
/// Provides comprehensive parsing of all JSON:API query parameters including:
/// <list type="bullet">
///   <item>
///     <description>Pagination (page[number] and page[size])</description>
///   </item>
///   <item>
///     <description>Filtering (filter[field] and complex filters)</description>
///   </item>
///   <item>
///     <description>Sorting (sort=field,-descendingField)</description>
///   </item>
///   <item>
///     <description>Inclusion (include=relationship1,relationship2)</description>
///   </item>
/// </list>
/// </para>
/// <para>
/// The resulting <see cref="QueryParameters"/> object can be used with <see cref="QueryableExtensions"/>
/// to apply these parameters to Entity Framework queries.
/// </para>
/// </remarks>
public static class JsonApiQueryParser
{
    private const int DEFAULT_PAGE_SIZE = 10;
    private const int MIN_PAGE_SIZE = 1;
    private const int MAX_PAGE_SIZE = 100;

    /// <summary>
    /// Parses all JSON:API query parameters from an HTTP request into a structured QueryParameters object.
    /// </summary>
    /// <param name="request">The HTTP request containing the query parameters</param>
    /// <returns>A QueryParameters object containing all parsed query parameters</returns>
    /// <remarks>
    /// <para>
    /// This method parses:
    /// <list type="number">
    ///   <item>
    ///     <description>Pagination parameters:</description>
    ///     <list type="bullet">
    ///       <item>
    ///         <description>page[number]: The page number (starting from 1)</description>
    ///       </item>
    ///       <item>
    ///         <description>page[size]: The page size (limited to 1-100)</description>
    ///       </item>
    ///     </list>
    ///   </item>
    ///   <item>
    ///     <description>Filter parameters:</description>
    ///     <list type="bullet">
    ///       <item>
    ///         <description>Simple: filter[field]=value</description>
    ///       </item>
    ///       <item>
    ///         <description>Complex: filter[field][operator]=value</description>
    ///       </item>
    ///       <item>
    ///         <description>Logical groups: filter[or][0][field]=value</description>
    ///       </item>
    ///     </list>
    ///   </item>
    ///   <item>
    ///     <description>Sort parameters: sort=field1,-field2 (minus prefix for descending)</description>
    ///   </item>
    ///   <item>
    ///     <description>Include parameters: include=relationship1,relationship2</description>
    ///   </item>
    /// </list>
    /// </para>
    /// <para>
    /// The method applies reasonable defaults and constraints:
    /// <list type="bullet">
    ///   <item>
    ///     <description>Page number defaults to 1 if invalid</description>
    ///   </item>
    ///   <item>
    ///     <description>Page size is clamped between 1-100</description>
    ///   </item>
    ///   <item>
    ///     <description>Field names are properly normalized</description>
    ///   </item>
    /// </list>
    /// </para>
    /// </remarks>
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
                Size = int.TryParse(pageSize, out int size)
                    ? Math.Clamp(size, MIN_PAGE_SIZE, MAX_PAGE_SIZE)
                    : DEFAULT_PAGE_SIZE,
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

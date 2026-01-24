using JsonApiToolkit.Configuration;
using JsonApiToolkit.Models.Errors;
using JsonApiToolkit.Models.Querying;
using JsonApiToolkit.Models.Querying.Filtering;

namespace JsonApiToolkit.Tests.Configuration;

public class QueryComplexityAnalyzerTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // CountFilters tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void CountFilters_WithEmptyGroup_ReturnsZero()
    {
        var group = new FilterGroup();

        int count = QueryComplexityAnalyzer.CountFilters(group);

        Assert.Equal(0, count);
    }

    [Fact]
    public void CountFilters_WithFlatFilters_ReturnsCorrectCount()
    {
        var group = new FilterGroup
        {
            Filters =
            [
                new() { Field = "name", Value = "test" },
                new() { Field = "age", Value = "25" },
                new() { Field = "status", Value = "active" },
            ],
        };

        int count = QueryComplexityAnalyzer.CountFilters(group);

        Assert.Equal(3, count);
    }

    [Fact]
    public void CountFilters_WithNestedGroups_CountsAllFilters()
    {
        var group = new FilterGroup
        {
            Filters = [new() { Field = "status", Value = "active" }],
            Groups =
            [
                new FilterGroup
                {
                    LogicalOperator = LogicalOperator.Or,
                    Filters =
                    [
                        new() { Field = "name", Value = "John" },
                        new() { Field = "name", Value = "Jane" },
                    ],
                },
                new FilterGroup
                {
                    LogicalOperator = LogicalOperator.Not,
                    Filters = [new() { Field = "deleted", Value = "true" }],
                },
            ],
        };

        int count = QueryComplexityAnalyzer.CountFilters(group);

        Assert.Equal(4, count); // 1 + 2 + 1
    }

    [Fact]
    public void CountFilters_WithDeeplyNestedGroups_CountsAllFilters()
    {
        var group = new FilterGroup
        {
            Filters = [new() { Field = "root", Value = "1" }],
            Groups =
            [
                new FilterGroup
                {
                    Filters = [new() { Field = "level1", Value = "2" }],
                    Groups =
                    [
                        new FilterGroup
                        {
                            Filters = [new() { Field = "level2", Value = "3" }],
                            Groups =
                            [
                                new FilterGroup
                                {
                                    Filters = [new() { Field = "level3", Value = "4" }],
                                },
                            ],
                        },
                    ],
                },
            ],
        };

        int count = QueryComplexityAnalyzer.CountFilters(group);

        Assert.Equal(4, count);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CountGroups tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void CountGroups_WithEmptyGroup_ReturnsZero()
    {
        var group = new FilterGroup();

        int count = QueryComplexityAnalyzer.CountGroups(group);

        Assert.Equal(0, count);
    }

    [Fact]
    public void CountGroups_WithNestedGroups_ReturnsCorrectCount()
    {
        var group = new FilterGroup
        {
            Groups =
            [
                new FilterGroup { LogicalOperator = LogicalOperator.Or },
                new FilterGroup
                {
                    LogicalOperator = LogicalOperator.Not,
                    Groups = [new FilterGroup()],
                },
            ],
        };

        int count = QueryComplexityAnalyzer.CountGroups(group);

        Assert.Equal(3, count); // 2 direct + 1 nested
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetMaxDepth tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void GetMaxDepth_WithFlatGroup_ReturnsOne()
    {
        var group = new FilterGroup { Filters = [new() { Field = "name", Value = "test" }] };

        int depth = QueryComplexityAnalyzer.GetMaxDepth(group);

        Assert.Equal(1, depth);
    }

    [Fact]
    public void GetMaxDepth_WithSingleNestedGroup_ReturnsTwo()
    {
        var group = new FilterGroup
        {
            Groups = [new FilterGroup { Filters = [new() { Field = "name", Value = "test" }] }],
        };

        int depth = QueryComplexityAnalyzer.GetMaxDepth(group);

        Assert.Equal(2, depth);
    }

    [Fact]
    public void GetMaxDepth_WithDeeplyNestedGroups_ReturnsCorrectDepth()
    {
        var group = new FilterGroup
        {
            Groups =
            [
                new FilterGroup
                {
                    Groups =
                    [
                        new FilterGroup
                        {
                            Groups =
                            [
                                new FilterGroup
                                {
                                    Filters = [new() { Field = "deep", Value = "1" }],
                                },
                            ],
                        },
                    ],
                },
            ],
        };

        int depth = QueryComplexityAnalyzer.GetMaxDepth(group);

        Assert.Equal(4, depth);
    }

    [Fact]
    public void GetMaxDepth_WithUnevenNesting_ReturnsMaxDepth()
    {
        // One branch is 2 deep, another is 3 deep
        var group = new FilterGroup
        {
            Groups =
            [
                new FilterGroup { Filters = [new() { Field = "shallow", Value = "1" }] },
                new FilterGroup
                {
                    Groups =
                    [
                        new FilterGroup { Filters = [new() { Field = "deeper", Value = "2" }] },
                    ],
                },
            ],
        };

        int depth = QueryComplexityAnalyzer.GetMaxDepth(group);

        Assert.Equal(3, depth);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Validate - MaxFilters tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Validate_WithFiltersBelowLimit_DoesNotThrow()
    {
        var options = new JsonApiOptions { MaxFilters = 10 };
        var parameters = new QueryParameters
        {
            Filter = new FilterGroup
            {
                Filters = [new() { Field = "a", Value = "1" }, new() { Field = "b", Value = "2" }],
            },
        };

        var exception = Record.Exception(() =>
            QueryComplexityAnalyzer.Validate(parameters, options)
        );

        Assert.Null(exception);
    }

    [Fact]
    public void Validate_WithFiltersExceedingLimit_ThrowsBadRequest()
    {
        var options = new JsonApiOptions { MaxFilters = 2 };
        var parameters = new QueryParameters
        {
            Filter = new FilterGroup
            {
                Filters =
                [
                    new() { Field = "a", Value = "1" },
                    new() { Field = "b", Value = "2" },
                    new() { Field = "c", Value = "3" },
                ],
            },
        };

        var exception = Assert.Throws<JsonApiBadRequestException>(() =>
            QueryComplexityAnalyzer.Validate(parameters, options)
        );

        Assert.Contains("3 filters", exception.Message);
        Assert.Contains("maximum allowed is 2", exception.Message);
        Assert.Contains("JsonApiOptions.MaxFilters", exception.Message);
        Assert.Equal(JsonApiErrorCodes.QueryTooComplex, exception.Code);
    }

    [Fact]
    public void Validate_WithFiltersAtExactLimit_DoesNotThrow()
    {
        var options = new JsonApiOptions { MaxFilters = 3 };
        var parameters = new QueryParameters
        {
            Filter = new FilterGroup
            {
                Filters =
                [
                    new() { Field = "a", Value = "1" },
                    new() { Field = "b", Value = "2" },
                    new() { Field = "c", Value = "3" },
                ],
            },
        };

        var exception = Record.Exception(() =>
            QueryComplexityAnalyzer.Validate(parameters, options)
        );

        Assert.Null(exception);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Validate - MaxFilterGroups tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Validate_WithGroupsExceedingLimit_ThrowsBadRequest()
    {
        var options = new JsonApiOptions { MaxFilterGroups = 2 };
        var parameters = new QueryParameters
        {
            Filter = new FilterGroup
            {
                Groups = [new FilterGroup(), new FilterGroup(), new FilterGroup()],
            },
        };

        var exception = Assert.Throws<JsonApiBadRequestException>(() =>
            QueryComplexityAnalyzer.Validate(parameters, options)
        );

        Assert.Contains("3 filter groups", exception.Message);
        Assert.Contains("maximum allowed is 2", exception.Message);
        Assert.Contains("JsonApiOptions.MaxFilterGroups", exception.Message);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Validate - MaxFilterDepth tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Validate_WithDepthExceedingLimit_ThrowsBadRequest()
    {
        var options = new JsonApiOptions { MaxFilterDepth = 2 };
        var parameters = new QueryParameters
        {
            Filter = new FilterGroup
            {
                Groups =
                [
                    new FilterGroup
                    {
                        Groups =
                        [
                            new FilterGroup { Filters = [new() { Field = "deep", Value = "1" }] },
                        ],
                    },
                ],
            },
        };

        var exception = Assert.Throws<JsonApiBadRequestException>(() =>
            QueryComplexityAnalyzer.Validate(parameters, options)
        );

        Assert.Contains("nesting depth", exception.Message);
        Assert.Contains("JsonApiOptions.MaxFilterDepth", exception.Message);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Validate - MaxFilterValueLength tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Validate_WithFilterValueExceedingLength_ThrowsBadRequest()
    {
        var options = new JsonApiOptions { MaxFilterValueLength = 10 };
        var parameters = new QueryParameters
        {
            Filter = new FilterGroup
            {
                Filters = [new() { Field = "search", Value = "this is a very long value" }],
            },
        };

        var exception = Assert.Throws<JsonApiBadRequestException>(() =>
            QueryComplexityAnalyzer.Validate(parameters, options)
        );

        Assert.Contains("search", exception.Message);
        Assert.Contains("maximum allowed is 10", exception.Message);
        Assert.Contains("JsonApiOptions.MaxFilterValueLength", exception.Message);
    }

    [Fact]
    public void Validate_WithFilterValueInNestedGroup_ValidatesCorrectly()
    {
        var options = new JsonApiOptions { MaxFilterValueLength = 5 };
        var parameters = new QueryParameters
        {
            Filter = new FilterGroup
            {
                Groups =
                [
                    new FilterGroup
                    {
                        Filters = [new() { Field = "nested", Value = "too_long_value" }],
                    },
                ],
            },
        };

        var exception = Assert.Throws<JsonApiBadRequestException>(() =>
            QueryComplexityAnalyzer.Validate(parameters, options)
        );

        Assert.Contains("nested", exception.Message);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Validate - MaxIncludeDepth tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Validate_WithIncludesBelowDepthLimit_DoesNotThrow()
    {
        var options = new JsonApiOptions { MaxIncludeDepth = 3 };
        var parameters = new QueryParameters
        {
            Include = ["author", "author.posts", "comments.author"],
        };

        var exception = Record.Exception(() =>
            QueryComplexityAnalyzer.Validate(parameters, options)
        );

        Assert.Null(exception);
    }

    [Fact]
    public void Validate_WithIncludesExceedingDepthLimit_ThrowsBadRequest()
    {
        var options = new JsonApiOptions { MaxIncludeDepth = 2 };
        var parameters = new QueryParameters
        {
            Include = ["author.posts.comments.likes"], // depth 4
        };

        var exception = Assert.Throws<JsonApiBadRequestException>(() =>
            QueryComplexityAnalyzer.Validate(parameters, options)
        );

        Assert.Contains("author.posts.comments.likes", exception.Message);
        Assert.Contains("depth 4", exception.Message);
        Assert.Contains("maximum allowed is 2", exception.Message);
        Assert.Contains("JsonApiOptions.MaxIncludeDepth", exception.Message);
        Assert.Equal(JsonApiErrorCodes.IncludeDepthExceeded, exception.Code);
    }

    [Fact]
    public void Validate_WithIncludeAtExactDepthLimit_DoesNotThrow()
    {
        var options = new JsonApiOptions { MaxIncludeDepth = 3 };
        var parameters = new QueryParameters
        {
            Include = ["author.posts.comments"], // depth 3
        };

        var exception = Record.Exception(() =>
            QueryComplexityAnalyzer.Validate(parameters, options)
        );

        Assert.Null(exception);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Validate - null/empty parameters
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Validate_WithNullFilter_DoesNotThrow()
    {
        var options = new JsonApiOptions();
        var parameters = new QueryParameters { Filter = null };

        var exception = Record.Exception(() =>
            QueryComplexityAnalyzer.Validate(parameters, options)
        );

        Assert.Null(exception);
    }

    [Fact]
    public void Validate_WithNullIncludes_DoesNotThrow()
    {
        var options = new JsonApiOptions();
        var parameters = new QueryParameters { Include = null };

        var exception = Record.Exception(() =>
            QueryComplexityAnalyzer.Validate(parameters, options)
        );

        Assert.Null(exception);
    }

    [Fact]
    public void Validate_WithEmptyIncludes_DoesNotThrow()
    {
        var options = new JsonApiOptions();
        var parameters = new QueryParameters { Include = [] };

        var exception = Record.Exception(() =>
            QueryComplexityAnalyzer.Validate(parameters, options)
        );

        Assert.Null(exception);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Default options tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void JsonApiOptions_HasCorrectDefaults()
    {
        var options = new JsonApiOptions();

        Assert.Equal(50, options.MaxFilters);
        Assert.Equal(10, options.MaxFilterGroups);
        Assert.Equal(3, options.MaxFilterDepth);
        Assert.Equal(1000, options.MaxFilterValueLength);
        Assert.Equal(3, options.MaxIncludeDepth);
        Assert.Equal(100, options.MaxPageSize);
        Assert.Equal(10, options.DefaultPageSize);
    }
}

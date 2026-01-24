using JsonApiToolkit.Models.Querying.Filtering;
using JsonApiToolkit.Validation;

namespace JsonApiToolkit.Tests.Validation;

public class FilterPathValidationTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // Basic filter path extraction
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ValidateFilterPaths_WithNullFilter_ReturnsValid()
    {
        var result = IncludeValidator.ValidateFilterPaths(null, ["author"]);

        Assert.True(result.IsValid);
        Assert.Empty(result.ForbiddenFilterPaths);
    }

    [Fact]
    public void ValidateFilterPaths_WithSimpleFilters_ReturnsValid()
    {
        // Filters without dots should always be valid (they filter the main entity)
        var filter = new FilterGroup
        {
            Filters =
            [
                new FilterParameter { Field = "name", Value = "test" },
                new FilterParameter { Field = "age", Value = "25" },
            ],
        };

        var result = IncludeValidator.ValidateFilterPaths(filter, ["author"]);

        Assert.True(result.IsValid);
        Assert.Empty(result.ForbiddenFilterPaths);
    }

    [Fact]
    public void ValidateFilterPaths_WithAllowedRelationship_ReturnsValid()
    {
        var filter = new FilterGroup
        {
            Filters = [new FilterParameter { Field = "author.name", Value = "John" }],
        };

        var result = IncludeValidator.ValidateFilterPaths(filter, ["author", "posts"]);

        Assert.True(result.IsValid);
        Assert.Empty(result.ForbiddenFilterPaths);
    }

    [Fact]
    public void ValidateFilterPaths_WithForbiddenRelationship_ReturnsInvalid()
    {
        var filter = new FilterGroup
        {
            Filters = [new FilterParameter { Field = "admin.password", Value = "secret" }],
        };

        var result = IncludeValidator.ValidateFilterPaths(filter, ["author", "posts"]);

        Assert.False(result.IsValid);
        Assert.Single(result.ForbiddenFilterPaths);
        Assert.Contains("admin", result.ForbiddenFilterPaths);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Nested filter groups
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ValidateFilterPaths_WithNestedGroups_ChecksAllLevels()
    {
        var filter = new FilterGroup
        {
            Filters = [new FilterParameter { Field = "status", Value = "active" }],
            Groups =
            [
                new FilterGroup
                {
                    LogicalOperator = LogicalOperator.Or,
                    Filters =
                    [
                        new FilterParameter { Field = "admin.email", Value = "test@test.com" },
                        new FilterParameter { Field = "secrets.key", Value = "abc" },
                    ],
                },
            ],
        };

        var result = IncludeValidator.ValidateFilterPaths(filter, ["author"]);

        Assert.False(result.IsValid);
        Assert.Equal(2, result.ForbiddenFilterPaths.Count);
        Assert.Contains("admin", result.ForbiddenFilterPaths);
        Assert.Contains("secrets", result.ForbiddenFilterPaths);
    }

    [Fact]
    public void ValidateFilterPaths_WithDeeplyNestedGroups_ChecksAllLevels()
    {
        var filter = new FilterGroup
        {
            Groups =
            [
                new FilterGroup
                {
                    Groups =
                    [
                        new FilterGroup
                        {
                            Filters = [new FilterParameter { Field = "hidden.data", Value = "x" }],
                        },
                    ],
                },
            ],
        };

        var result = IncludeValidator.ValidateFilterPaths(filter, ["visible"]);

        Assert.False(result.IsValid);
        Assert.Single(result.ForbiddenFilterPaths);
        Assert.Contains("hidden", result.ForbiddenFilterPaths);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Wildcard patterns
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ValidateFilterPaths_WithWildcardPattern_AllowsMatchingPaths()
    {
        var filter = new FilterGroup
        {
            Filters =
            [
                new FilterParameter { Field = "author.posts.title", Value = "test" },
                new FilterParameter { Field = "author.comments.text", Value = "hello" },
            ],
        };

        var result = IncludeValidator.ValidateFilterPaths(filter, ["author.*"]);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateFilterPaths_WithWildcardPattern_BlocksNonMatchingPaths()
    {
        var filter = new FilterGroup
        {
            Filters = [new FilterParameter { Field = "comments.author.name", Value = "John" }],
        };

        var result = IncludeValidator.ValidateFilterPaths(filter, ["author.*"]);

        Assert.False(result.IsValid);
        Assert.Contains("comments.author", result.ForbiddenFilterPaths);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Nested relationship paths
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ValidateFilterPaths_WithNestedPath_ExtractsCorrectRelationship()
    {
        // filter[author.posts.title] should extract "author.posts" as the relationship
        var filter = new FilterGroup
        {
            Filters = [new FilterParameter { Field = "author.posts.title", Value = "test" }],
        };

        var result = IncludeValidator.ValidateFilterPaths(filter, ["author.posts"]);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateFilterPaths_WithDeepNestedPath_BlocksIfNotAllowed()
    {
        // filter[author.posts.comments.text] has relationship "author.posts.comments"
        var filter = new FilterGroup
        {
            Filters =
            [
                new FilterParameter { Field = "author.posts.comments.text", Value = "test" },
            ],
        };

        var result = IncludeValidator.ValidateFilterPaths(filter, ["author.posts"]);

        Assert.False(result.IsValid);
        Assert.Contains("author.posts.comments", result.ForbiddenFilterPaths);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Duplicate handling
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ValidateFilterPaths_WithDuplicateForbiddenPaths_ReturnsUnique()
    {
        var filter = new FilterGroup
        {
            Filters =
            [
                new FilterParameter { Field = "admin.email", Value = "a@b.com" },
                new FilterParameter { Field = "admin.password", Value = "secret" },
                new FilterParameter { Field = "admin.role", Value = "superuser" },
            ],
        };

        var result = IncludeValidator.ValidateFilterPaths(filter, ["author"]);

        Assert.False(result.IsValid);
        Assert.Single(result.ForbiddenFilterPaths); // Should only have "admin" once
        Assert.Equal("admin", result.ForbiddenFilterPaths[0]);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Case sensitivity
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ValidateFilterPaths_CaseInsensitive_AllowsMatchingPaths()
    {
        var filter = new FilterGroup
        {
            Filters = [new FilterParameter { Field = "Author.Name", Value = "John" }],
        };

        var result = IncludeValidator.ValidateFilterPaths(filter, ["author"]);

        Assert.True(result.IsValid);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Empty allowed patterns
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ValidateFilterPaths_WithEmptyAllowedPatterns_BlocksAllRelationshipFilters()
    {
        var filter = new FilterGroup
        {
            Filters = [new FilterParameter { Field = "author.name", Value = "John" }],
        };

        var result = IncludeValidator.ValidateFilterPaths(filter, []);

        Assert.False(result.IsValid);
        Assert.Single(result.ForbiddenFilterPaths);
    }

    [Fact]
    public void ValidateFilterPaths_WithEmptyAllowedPatterns_AllowsSimpleFilters()
    {
        var filter = new FilterGroup
        {
            Filters = [new FilterParameter { Field = "name", Value = "John" }],
        };

        var result = IncludeValidator.ValidateFilterPaths(filter, []);

        Assert.True(result.IsValid);
    }
}

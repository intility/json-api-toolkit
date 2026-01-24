using JsonApiToolkit.Models.Errors;

namespace JsonApiToolkit.Tests.Models.Errors;

public class JsonApiErrorsTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // NotFound
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void NotFound_CreatesCorrectException()
    {
        var ex = JsonApiErrors.NotFound("books", 123);

        Assert.IsType<JsonApiNotFoundException>(ex);
        Assert.Equal(404, ex.StatusCode);
        Assert.Equal(JsonApiErrorCodes.ResourceNotFound, ex.Code);
        Assert.Equal("Resource 'books' with id '123' not found.", ex.Message);
        Assert.NotNull(ex.Meta);
        Assert.Equal("books", ex.Meta["resourceType"]);
        Assert.Equal(123, ex.Meta["id"]);
    }

    [Fact]
    public void RelatedNotFound_CreatesCorrectException()
    {
        var ex = JsonApiErrors.RelatedNotFound("books", 1, "author", 99);

        Assert.IsType<JsonApiNotFoundException>(ex);
        Assert.Equal(404, ex.StatusCode);
        Assert.Equal(JsonApiErrorCodes.ResourceNotFound, ex.Code);
        Assert.Contains("author", ex.Message);
        Assert.Contains("99", ex.Message);
        Assert.NotNull(ex.Meta);
        Assert.Equal("author", ex.Meta["relationship"]);
        Assert.Equal(99, ex.Meta["relatedId"]);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // InvalidFilterValue
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void InvalidFilterValue_IncludesSourceParameter()
    {
        var ex = JsonApiErrors.InvalidFilterValue("age", "abc", typeof(int));

        Assert.IsType<JsonApiBadRequestException>(ex);
        Assert.Equal(400, ex.StatusCode);
        Assert.Equal(JsonApiErrorCodes.InvalidFilterValue, ex.Code);
        Assert.NotNull(ex.ErrorSource);
        Assert.Equal("filter[age]", ex.ErrorSource.Parameter);
        Assert.NotNull(ex.Meta);
        Assert.Equal("age", ex.Meta["field"]);
        Assert.Equal("Int32", ex.Meta["expectedType"]);
        Assert.Equal("abc", ex.Meta["actualValue"]);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // InvalidFilterField
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void InvalidFilterField_WithAvailableFields_IncludesThemInMeta()
    {
        var availableFields = new[] { "name", "email", "age" };

        var ex = JsonApiErrors.InvalidFilterField("foo", typeof(TestClass), availableFields);

        Assert.IsType<JsonApiBadRequestException>(ex);
        Assert.Equal(JsonApiErrorCodes.InvalidFilterField, ex.Code);
        Assert.NotNull(ex.ErrorSource);
        Assert.Equal("filter[foo]", ex.ErrorSource.Parameter);
        Assert.NotNull(ex.Meta);
        Assert.Equal("TestClass", ex.Meta["entityType"]);
        Assert.Equal(availableFields.ToList(), ex.Meta["availableFields"]);
    }

    [Fact]
    public void InvalidFilterField_WithoutAvailableFields_OmitsFromMeta()
    {
        var ex = JsonApiErrors.InvalidFilterField("foo", typeof(TestClass));

        Assert.NotNull(ex.Meta);
        Assert.False(ex.Meta.ContainsKey("availableFields"));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // IncludeNotAllowed
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void IncludeNotAllowed_WithAllowedIncludes_IncludesThemInMeta()
    {
        var allowed = new[] { "author", "comments" };

        var ex = JsonApiErrors.IncludeNotAllowed("secretData", allowed);

        Assert.IsType<JsonApiForbiddenException>(ex);
        Assert.Equal(403, ex.StatusCode);
        Assert.Equal(JsonApiErrorCodes.IncludeNotAllowed, ex.Code);
        Assert.NotNull(ex.ErrorSource);
        Assert.Equal("include", ex.ErrorSource.Parameter);
        Assert.NotNull(ex.Meta);
        Assert.Equal("secretData", ex.Meta["requestedInclude"]);
        Assert.Equal(allowed.ToList(), ex.Meta["allowedIncludes"]);
    }

    [Fact]
    public void IncludeNotAllowed_WithoutAllowedIncludes_OmitsFromMeta()
    {
        var ex = JsonApiErrors.IncludeNotAllowed("secretData");

        Assert.NotNull(ex.Meta);
        Assert.False(ex.Meta.ContainsKey("allowedIncludes"));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // AlreadyExists
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AlreadyExists_IncludesSourcePointer()
    {
        var ex = JsonApiErrors.AlreadyExists("users", "email", "test@example.com");

        Assert.IsType<JsonApiConflictException>(ex);
        Assert.Equal(409, ex.StatusCode);
        Assert.Equal(JsonApiErrorCodes.ResourceAlreadyExists, ex.Code);
        Assert.NotNull(ex.ErrorSource);
        Assert.Equal("/data/attributes/email", ex.ErrorSource.Pointer);
        Assert.NotNull(ex.Meta);
        Assert.Equal("users", ex.Meta["resourceType"]);
        Assert.Equal("email", ex.Meta["field"]);
        Assert.Equal("test@example.com", ex.Meta["value"]);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // QueryTooComplex
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void QueryTooComplex_IncludesLimitDetails()
    {
        var ex = JsonApiErrors.QueryTooComplex("filters", 50, 75, "JsonApiOptions.MaxFilters");

        Assert.IsType<JsonApiBadRequestException>(ex);
        Assert.Equal(JsonApiErrorCodes.QueryTooComplex, ex.Code);
        Assert.Contains("75", ex.Message);
        Assert.Contains("50", ex.Message);
        Assert.Contains("JsonApiOptions.MaxFilters", ex.Message);
        Assert.NotNull(ex.Meta);
        Assert.Equal(50, ex.Meta["limit"]);
        Assert.Equal(75, ex.Meta["actual"]);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Validation helpers
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ValidationFailed_CreatesCorrectException()
    {
        var ex = JsonApiErrors.ValidationFailed("email", "Invalid email format");

        Assert.IsType<JsonApiBadRequestException>(ex);
        Assert.Equal(JsonApiErrorCodes.ValidationFailed, ex.Code);
        Assert.Equal("Invalid email format", ex.Message);
        Assert.NotNull(ex.ErrorSource);
        Assert.Equal("/data/attributes/email", ex.ErrorSource.Pointer);
    }

    [Fact]
    public void RequiredFieldMissing_CreatesCorrectException()
    {
        var ex = JsonApiErrors.RequiredFieldMissing("title");

        Assert.IsType<JsonApiBadRequestException>(ex);
        Assert.Equal(JsonApiErrorCodes.RequiredFieldMissing, ex.Code);
        Assert.Contains("title", ex.Message);
        Assert.NotNull(ex.ErrorSource);
        Assert.Equal("/data/attributes/title", ex.ErrorSource.Pointer);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // All factories produce valid exceptions
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AllFactories_ProduceJsonApiExceptionSubclasses()
    {
        var exceptions = new JsonApiException[]
        {
            JsonApiErrors.NotFound("books", 1),
            JsonApiErrors.RelatedNotFound("books", 1, "author", 2),
            JsonApiErrors.InvalidFilterValue("age", "abc", typeof(int)),
            JsonApiErrors.InvalidFilterField("foo", typeof(TestClass)),
            JsonApiErrors.InvalidFilterOperator("bad"),
            JsonApiErrors.InvalidSortField("foo", typeof(TestClass)),
            JsonApiErrors.QueryTooComplex("filters", 50, 75, "config"),
            JsonApiErrors.IncludeNotAllowed("secret"),
            JsonApiErrors.FilterNotAllowed("secret.field"),
            JsonApiErrors.AlreadyExists("users", "email", "test@test.com"),
            JsonApiErrors.ValidationFailed("email", "Invalid"),
            JsonApiErrors.RequiredFieldMissing("title"),
        };

        foreach (var ex in exceptions)
        {
            Assert.NotNull(ex.Code);
            Assert.NotNull(ex.Message);
            Assert.True(ex.StatusCode >= 400 && ex.StatusCode < 600);
        }
    }

    private class TestClass
    {
        public string? Name { get; set; }
    }
}

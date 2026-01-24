namespace JsonApiToolkit.Models.Errors;

/// <summary>
/// Standard error codes for JSON:API responses.
/// Use these with JsonApiErrors factory methods for consistent error handling.
/// </summary>
#pragma warning disable CS1591 // Constants are self-documenting
public static class JsonApiErrorCodes
{
    // Resource errors
    public const string ResourceNotFound = "RESOURCE_NOT_FOUND";
    public const string ResourceAlreadyExists = "RESOURCE_ALREADY_EXISTS";

    // Filter errors
    public const string InvalidFilterField = "INVALID_FILTER_FIELD";
    public const string InvalidFilterValue = "INVALID_FILTER_VALUE";
    public const string InvalidFilterOperator = "INVALID_FILTER_OPERATOR";
    public const string FilterNotAllowed = "FILTER_NOT_ALLOWED";

    // Include errors
    public const string IncludeNotAllowed = "INCLUDE_NOT_ALLOWED";
    public const string IncludeDepthExceeded = "INCLUDE_DEPTH_EXCEEDED";

    // Pagination errors
    public const string InvalidPageNumber = "INVALID_PAGE_NUMBER";
    public const string InvalidPageSize = "INVALID_PAGE_SIZE";
    public const string PageSizeExceeded = "PAGE_SIZE_EXCEEDED";

    // Sort errors
    public const string InvalidSortField = "INVALID_SORT_FIELD";

    // Query complexity
    public const string QueryTooComplex = "QUERY_TOO_COMPLEX";
    public const string TooManyFilters = "TOO_MANY_FILTERS";

    // Validation
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string RequiredFieldMissing = "REQUIRED_FIELD_MISSING";

    // Auth
    public const string AuthenticationRequired = "AUTHENTICATION_REQUIRED";
    public const string InsufficientPermissions = "INSUFFICIENT_PERMISSIONS";
}
#pragma warning restore CS1591

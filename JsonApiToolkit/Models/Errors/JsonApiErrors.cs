namespace JsonApiToolkit.Models.Errors;

/// <summary>
/// Factory methods for creating consistent, well-structured JSON:API errors.
/// </summary>
public static class JsonApiErrors
{
    // ─────────────────────────────────────────────────────────────────────────
    // 404 - Not Found
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Creates a 404 error for a missing resource.</summary>
    public static JsonApiNotFoundException NotFound(string resourceType, object id) =>
        new(
            $"Resource '{resourceType}' with id '{id}' not found.",
            JsonApiErrorCodes.ResourceNotFound,
            meta: new Dictionary<string, object> { ["resourceType"] = resourceType, ["id"] = id }
        );

    /// <summary>Creates a 404 error for a missing related resource.</summary>
    public static JsonApiNotFoundException RelatedNotFound(
        string resourceType,
        object id,
        string relationship,
        object relatedId
    ) =>
        new(
            $"Related resource '{relationship}' with id '{relatedId}' not found on '{resourceType}/{id}'.",
            JsonApiErrorCodes.ResourceNotFound,
            meta: new Dictionary<string, object>
            {
                ["resourceType"] = resourceType,
                ["id"] = id,
                ["relationship"] = relationship,
                ["relatedId"] = relatedId,
            }
        );

    // ─────────────────────────────────────────────────────────────────────────
    // 400 - Bad Request (filters)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Creates a 400 error for an invalid filter value type.</summary>
    public static JsonApiBadRequestException InvalidFilterValue(
        string field,
        string actualValue,
        Type expectedType
    ) =>
        new(
            $"Cannot convert '{actualValue}' to type {expectedType.Name} for field '{field}'.",
            JsonApiErrorCodes.InvalidFilterValue,
            new ErrorSource { Parameter = $"filter[{field}]" },
            new Dictionary<string, object>
            {
                ["field"] = field,
                ["expectedType"] = expectedType.Name,
                ["actualValue"] = actualValue,
            }
        );

    /// <summary>Creates a 400 error for a non-existent filter field.</summary>
    public static JsonApiBadRequestException InvalidFilterField(
        string field,
        Type entityType,
        IEnumerable<string>? availableFields = null
    )
    {
        var meta = new Dictionary<string, object>
        {
            ["field"] = field,
            ["entityType"] = entityType.Name,
        };

        if (availableFields != null)
            meta["availableFields"] = availableFields.ToList();

        return new JsonApiBadRequestException(
            $"Property '{field}' does not exist on type '{entityType.Name}'.",
            JsonApiErrorCodes.InvalidFilterField,
            new ErrorSource { Parameter = $"filter[{field}]" },
            meta
        );
    }

    /// <summary>Creates a 400 error for an invalid filter operator.</summary>
    public static JsonApiBadRequestException InvalidFilterOperator(
        string op,
        IEnumerable<string>? validOperators = null
    )
    {
        var meta = new Dictionary<string, object> { ["operator"] = op };

        if (validOperators != null)
            meta["validOperators"] = validOperators.ToList();

        return new JsonApiBadRequestException(
            $"Unknown filter operator '{op}'.",
            JsonApiErrorCodes.InvalidFilterOperator,
            new ErrorSource { Parameter = "filter" },
            meta
        );
    }

    /// <summary>Creates a 400 error for an invalid sort field.</summary>
    public static JsonApiBadRequestException InvalidSortField(
        string field,
        Type entityType,
        IEnumerable<string>? availableFields = null
    )
    {
        var meta = new Dictionary<string, object>
        {
            ["field"] = field,
            ["entityType"] = entityType.Name,
        };

        if (availableFields != null)
            meta["availableFields"] = availableFields.ToList();

        return new JsonApiBadRequestException(
            $"Cannot sort by '{field}'. Property does not exist on type '{entityType.Name}'.",
            JsonApiErrorCodes.InvalidSortField,
            new ErrorSource { Parameter = "sort" },
            meta
        );
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 400 - Bad Request (query complexity) - for Phase 2
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Creates a 400 error when query exceeds complexity limits.</summary>
    public static JsonApiBadRequestException QueryTooComplex(
        string limitName,
        int limit,
        int actual,
        string configKey
    ) =>
        new(
            $"Query contains {actual} {limitName}, but maximum allowed is {limit}. "
                + $"Reduce count or configure a higher limit via {configKey}.",
            JsonApiErrorCodes.QueryTooComplex,
            new ErrorSource { Parameter = "filter" },
            new Dictionary<string, object>
            {
                ["limitName"] = limitName,
                ["limit"] = limit,
                ["actual"] = actual,
                ["configKey"] = configKey,
            }
        );

    // ─────────────────────────────────────────────────────────────────────────
    // 403 - Forbidden
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Creates a 403 error for a disallowed include path.</summary>
    public static JsonApiForbiddenException IncludeNotAllowed(
        string include,
        IEnumerable<string>? allowedIncludes = null
    )
    {
        var meta = new Dictionary<string, object> { ["requestedInclude"] = include };

        if (allowedIncludes != null)
            meta["allowedIncludes"] = allowedIncludes.ToList();

        return new JsonApiForbiddenException(
            $"Include path '{include}' is not allowed.",
            JsonApiErrorCodes.IncludeNotAllowed,
            new ErrorSource { Parameter = "include" },
            meta
        );
    }

    /// <summary>Creates a 403 error for filtering on a disallowed relationship.</summary>
    public static JsonApiForbiddenException FilterNotAllowed(string relationshipPath) =>
        new(
            $"Filtering on relationship '{relationshipPath}' is not allowed.",
            JsonApiErrorCodes.FilterNotAllowed,
            new ErrorSource { Parameter = $"filter[{relationshipPath}]" },
            new Dictionary<string, object> { ["relationshipPath"] = relationshipPath }
        );

    // ─────────────────────────────────────────────────────────────────────────
    // 409 - Conflict
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Creates a 409 error for duplicate resource.</summary>
    public static JsonApiConflictException AlreadyExists(
        string resourceType,
        string field,
        object value
    ) =>
        new(
            $"A '{resourceType}' with {field} '{value}' already exists.",
            JsonApiErrorCodes.ResourceAlreadyExists,
            new ErrorSource { Pointer = $"/data/attributes/{field}" },
            new Dictionary<string, object>
            {
                ["resourceType"] = resourceType,
                ["field"] = field,
                ["value"] = value,
            }
        );

    // ─────────────────────────────────────────────────────────────────────────
    // Validation helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Creates a 400 error for a validation failure.</summary>
    public static JsonApiBadRequestException ValidationFailed(string field, string message) =>
        new(
            message,
            JsonApiErrorCodes.ValidationFailed,
            new ErrorSource { Pointer = $"/data/attributes/{field}" },
            new Dictionary<string, object> { ["field"] = field }
        );

    /// <summary>Creates a 400 error for a missing required field.</summary>
    public static JsonApiBadRequestException RequiredFieldMissing(string field) =>
        new(
            $"Required field '{field}' is missing.",
            JsonApiErrorCodes.RequiredFieldMissing,
            new ErrorSource { Pointer = $"/data/attributes/{field}" },
            new Dictionary<string, object> { ["field"] = field }
        );
}

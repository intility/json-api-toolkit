namespace JsonApiToolkit.Models.Errors;

/// <summary>
/// Base class for JSON:API exceptions with status code, code, source, and meta.
/// </summary>
public abstract class JsonApiException : Exception
{
    /// <summary>
    /// HTTP status code for the error.
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// Application-specific error code.
    /// </summary>
    public string? Code { get; }

    /// <summary>
    /// Location in the request where the error occurred.
    /// </summary>
    public ErrorSource? ErrorSource { get; }

    /// <summary>
    /// Additional metadata about the error.
    /// </summary>
    public Dictionary<string, object>? Meta { get; }

    /// <summary>
    /// Initializes a new JSON:API exception.
    /// </summary>
    protected JsonApiException(
        int statusCode,
        string message,
        string? code = null,
        ErrorSource? errorSource = null,
        Dictionary<string, object>? meta = null,
        Exception? innerException = null
    )
        : base(message, innerException)
    {
        StatusCode = statusCode;
        Code = code;
        ErrorSource = errorSource;
        Meta = meta;
    }
}

/// <summary>
/// Exception for bad request errors (400).
/// </summary>
public class JsonApiBadRequestException : JsonApiException
{
    /// <summary>
    /// Initializes a new bad request exception.
    /// </summary>
    public JsonApiBadRequestException(string message)
        : base(400, message) { }

    /// <summary>
    /// Initializes a new bad request exception with additional details.
    /// </summary>
    public JsonApiBadRequestException(
        string message,
        string? code = null,
        ErrorSource? errorSource = null,
        Dictionary<string, object>? meta = null,
        Exception? innerException = null
    )
        : base(400, message, code, errorSource, meta, innerException) { }
}

/// <summary>
/// Exception for not found errors (404).
/// </summary>
public class JsonApiNotFoundException : JsonApiException
{
    /// <summary>
    /// Initializes a new not found exception.
    /// </summary>
    public JsonApiNotFoundException(string message)
        : base(404, message) { }

    /// <summary>
    /// Initializes a new not found exception with additional details.
    /// </summary>
    public JsonApiNotFoundException(
        string message,
        string? code = null,
        ErrorSource? errorSource = null,
        Dictionary<string, object>? meta = null,
        Exception? innerException = null
    )
        : base(404, message, code, errorSource, meta, innerException) { }
}

/// <summary>
/// Exception for conflict errors (409).
/// </summary>
public class JsonApiConflictException : JsonApiException
{
    /// <summary>
    /// Initializes a new conflict exception.
    /// </summary>
    public JsonApiConflictException(string message)
        : base(409, message) { }

    /// <summary>
    /// Initializes a new conflict exception with additional details.
    /// </summary>
    public JsonApiConflictException(
        string message,
        string? code = null,
        ErrorSource? errorSource = null,
        Dictionary<string, object>? meta = null,
        Exception? innerException = null
    )
        : base(409, message, code, errorSource, meta, innerException) { }
}

/// <summary>
/// Exception for unauthorized errors (401).
/// </summary>
public class JsonApiUnauthorizedException : JsonApiException
{
    /// <summary>
    /// Initializes a new unauthorized exception.
    /// </summary>
    public JsonApiUnauthorizedException(string message)
        : base(401, message) { }

    /// <summary>
    /// Initializes a new unauthorized exception with additional details.
    /// </summary>
    public JsonApiUnauthorizedException(
        string message,
        string? code = null,
        ErrorSource? errorSource = null,
        Dictionary<string, object>? meta = null,
        Exception? innerException = null
    )
        : base(401, message, code, errorSource, meta, innerException) { }
}

/// <summary>
/// Exception for forbidden errors (403).
/// </summary>
public class JsonApiForbiddenException : JsonApiException
{
    /// <summary>
    /// Initializes a new forbidden exception.
    /// </summary>
    public JsonApiForbiddenException(string message)
        : base(403, message) { }

    /// <summary>
    /// Initializes a new forbidden exception with additional details.
    /// </summary>
    public JsonApiForbiddenException(
        string message,
        string? code = null,
        ErrorSource? errorSource = null,
        Dictionary<string, object>? meta = null,
        Exception? innerException = null
    )
        : base(403, message, code, errorSource, meta, innerException) { }
}

/// <summary>
/// Exception for rate limit errors (429).
/// </summary>
public class JsonApiTooManyRequestsException : JsonApiException
{
    /// <summary>
    /// Initializes a new rate limit exception.
    /// </summary>
    public JsonApiTooManyRequestsException(string message)
        : base(429, message) { }

    /// <summary>
    /// Initializes a new rate limit exception with additional details.
    /// </summary>
    public JsonApiTooManyRequestsException(
        string message,
        string? code = null,
        ErrorSource? errorSource = null,
        Dictionary<string, object>? meta = null,
        Exception? innerException = null
    )
        : base(429, message, code, errorSource, meta, innerException) { }
}

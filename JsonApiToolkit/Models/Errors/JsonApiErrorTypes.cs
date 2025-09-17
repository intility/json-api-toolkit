namespace JsonApiToolkit.Models.Errors;

/// <summary>
/// Base class for all JSON:API exceptions that provides comprehensive error information.
/// </summary>
/// <remarks>
/// This base class supports the full JSON:API error object specification, allowing
/// for detailed error reporting with structured metadata, source information, and error codes.
/// </remarks>
public abstract class JsonApiException : Exception
{
    /// <summary>
    /// HTTP status code for this error.
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// Application-specific error code for categorizing the error.
    /// </summary>
    public string? Code { get; }

    /// <summary>
    /// Source information indicating where the error occurred.
    /// </summary>
    public ErrorSource? ErrorSource { get; }

    /// <summary>
    /// Additional metadata about the error.
    /// </summary>
    public Dictionary<string, object>? Meta { get; }

    /// <summary>
    /// Initializes a new instance of the JsonApiException class.
    /// </summary>
    /// <param name="statusCode">HTTP status code</param>
    /// <param name="message">Error message</param>
    /// <param name="code">Application-specific error code</param>
    /// <param name="errorSource">Source information</param>
    /// <param name="meta">Additional metadata</param>
    /// <param name="innerException">Inner exception</param>
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
/// Exception representing a 400 Bad Request error.
/// </summary>
public class JsonApiBadRequestException : JsonApiException
{
    /// <summary>
    /// Initializes a new instance of the JsonApiBadRequestException class.
    /// </summary>
    /// <param name="message">Error message</param>
    public JsonApiBadRequestException(string message)
        : base(400, message) { }

    /// <summary>
    /// Initializes a new instance of the JsonApiBadRequestException class with detailed error information.
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="code">Application-specific error code</param>
    /// <param name="errorSource">Source information</param>
    /// <param name="meta">Additional metadata</param>
    /// <param name="innerException">Inner exception</param>
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
/// Exception representing a 404 Not Found error.
/// </summary>
public class JsonApiNotFoundException : JsonApiException
{
    /// <summary>
    /// Initializes a new instance of the JsonApiNotFoundException class.
    /// </summary>
    /// <param name="message">Error message</param>
    public JsonApiNotFoundException(string message)
        : base(404, message) { }

    /// <summary>
    /// Initializes a new instance of the JsonApiNotFoundException class with detailed error information.
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="code">Application-specific error code</param>
    /// <param name="errorSource">Source information</param>
    /// <param name="meta">Additional metadata</param>
    /// <param name="innerException">Inner exception</param>
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
/// Exception representing a 409 Conflict error.
/// </summary>
public class JsonApiConflictException : JsonApiException
{
    /// <summary>
    /// Initializes a new instance of the JsonApiConflictException class.
    /// </summary>
    /// <param name="message">Error message</param>
    public JsonApiConflictException(string message)
        : base(409, message) { }

    /// <summary>
    /// Initializes a new instance of the JsonApiConflictException class with detailed error information.
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="code">Application-specific error code</param>
    /// <param name="errorSource">Source information</param>
    /// <param name="meta">Additional metadata</param>
    /// <param name="innerException">Inner exception</param>
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
/// Exception representing a 401 Unauthorized error.
/// </summary>
public class JsonApiUnauthorizedException : JsonApiException
{
    /// <summary>
    /// Initializes a new instance of the JsonApiUnauthorizedException class.
    /// </summary>
    /// <param name="message">Error message</param>
    public JsonApiUnauthorizedException(string message)
        : base(401, message) { }

    /// <summary>
    /// Initializes a new instance of the JsonApiUnauthorizedException class with detailed error information.
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="code">Application-specific error code</param>
    /// <param name="errorSource">Source information</param>
    /// <param name="meta">Additional metadata</param>
    /// <param name="innerException">Inner exception</param>
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
/// Exception representing a 403 Forbidden error.
/// </summary>
public class JsonApiForbiddenException : JsonApiException
{
    /// <summary>
    /// Initializes a new instance of the JsonApiForbiddenException class.
    /// </summary>
    /// <param name="message">Error message</param>
    public JsonApiForbiddenException(string message)
        : base(403, message) { }

    /// <summary>
    /// Initializes a new instance of the JsonApiForbiddenException class with detailed error information.
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="code">Application-specific error code</param>
    /// <param name="errorSource">Source information</param>
    /// <param name="meta">Additional metadata</param>
    /// <param name="innerException">Inner exception</param>
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
/// Exception representing a 429 Too Many Requests error.
/// </summary>
public class JsonApiTooManyRequestsException : JsonApiException
{
    /// <summary>
    /// Initializes a new instance of the JsonApiTooManyRequestsException class.
    /// </summary>
    /// <param name="message">Error message</param>
    public JsonApiTooManyRequestsException(string message)
        : base(429, message) { }

    /// <summary>
    /// Initializes a new instance of the JsonApiTooManyRequestsException class with detailed error information.
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="code">Application-specific error code</param>
    /// <param name="errorSource">Source information</param>
    /// <param name="meta">Additional metadata</param>
    /// <param name="innerException">Inner exception</param>
    public JsonApiTooManyRequestsException(
        string message,
        string? code = null,
        ErrorSource? errorSource = null,
        Dictionary<string, object>? meta = null,
        Exception? innerException = null
    )
        : base(429, message, code, errorSource, meta, innerException) { }
}

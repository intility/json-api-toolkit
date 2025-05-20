// Supress warnings for this file
#pragma warning disable RCS1194

namespace JsonApiToolkit.Models.Errors;

/// <summary>
/// Exception representing a 400 Bad Request error.
/// </summary>
public class JsonApiBadRequestException(string message) : Exception(message) { }

/// <summary>
/// Exception representing a 404 Not Found error.
/// </summary>
public class JsonApiNotFoundException(string message) : Exception(message) { }

/// <summary>
/// Exception representing a 409 Conflict error.
/// </summary>
public class JsonApiConflictException(string message) : Exception(message) { }

/// <summary>
/// Exception representing a 401 Unauthorized error.
/// </summary>
public class JsonApiUnauthorizedException(string message) : Exception(message) { }

/// <summary>
/// Exception representing a 403 Forbidden error.
/// </summary>
public class JsonApiForbiddenException(string message) : Exception(message) { }

/**
 * Location where the error occurred in the request.
 */
export interface JsonApiErrorSource {
  /** JSON Pointer to the value in the request body (e.g., "/data/attributes/email"). */
  pointer?: string;
  /** Query parameter that caused the error (e.g., "filter[age]"). */
  parameter?: string;
}

/**
 * A single JSON:API error object.
 * See https://jsonapi.org/format/#error-objects
 */
export interface JsonApiError {
  /** Unique identifier for this error occurrence. */
  id?: string;
  /** HTTP status code as a string (e.g., "404"). */
  status?: string;
  /** Application-specific error code (e.g., "RESOURCE_NOT_FOUND"). */
  code?: string;
  /** Short human-readable summary. */
  title?: string;
  /** Detailed explanation of the error. */
  detail?: string;
  /** Location of the error in the request. */
  source?: JsonApiErrorSource;
  /** Additional metadata about the error. */
  meta?: Record<string, unknown>;
}

/**
 * A JSON:API error response document containing one or more errors.
 */
export interface JsonApiErrorResponse {
  errors: JsonApiError[];
}

/**
 * Standard error codes produced by JsonApiToolkit (v1.3.0+).
 * Use these to match against `JsonApiError.code` for programmatic error handling.
 */
export const JsonApiErrorCodes = {
  // Resource errors
  RESOURCE_NOT_FOUND: 'RESOURCE_NOT_FOUND',
  RESOURCE_ALREADY_EXISTS: 'RESOURCE_ALREADY_EXISTS',

  // Filter errors
  INVALID_FILTER_FIELD: 'INVALID_FILTER_FIELD',
  INVALID_FILTER_VALUE: 'INVALID_FILTER_VALUE',
  INVALID_FILTER_OPERATOR: 'INVALID_FILTER_OPERATOR',
  FILTER_NOT_ALLOWED: 'FILTER_NOT_ALLOWED',
  UNSUPPORTED_FILTER_GROUP: 'UNSUPPORTED_FILTER_GROUP',

  // Include errors
  INCLUDE_NOT_ALLOWED: 'INCLUDE_NOT_ALLOWED',
  INCLUDE_DEPTH_EXCEEDED: 'INCLUDE_DEPTH_EXCEEDED',

  // Pagination errors
  INVALID_PAGE_NUMBER: 'INVALID_PAGE_NUMBER',
  INVALID_PAGE_SIZE: 'INVALID_PAGE_SIZE',
  PAGE_SIZE_EXCEEDED: 'PAGE_SIZE_EXCEEDED',

  // Sort errors
  INVALID_SORT_FIELD: 'INVALID_SORT_FIELD',

  // Query complexity
  QUERY_TOO_COMPLEX: 'QUERY_TOO_COMPLEX',
  TOO_MANY_FILTERS: 'TOO_MANY_FILTERS',

  // Validation
  VALIDATION_FAILED: 'VALIDATION_FAILED',
  REQUIRED_FIELD_MISSING: 'REQUIRED_FIELD_MISSING',

  // Auth
  AUTHENTICATION_REQUIRED: 'AUTHENTICATION_REQUIRED',
  INSUFFICIENT_PERMISSIONS: 'INSUFFICIENT_PERMISSIONS',
} as const;

/**
 * Union type of all standard error code strings.
 */
export type JsonApiErrorCode =
  (typeof JsonApiErrorCodes)[keyof typeof JsonApiErrorCodes];

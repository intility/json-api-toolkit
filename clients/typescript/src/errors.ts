import type {
  JsonApiError,
  JsonApiErrorCode,
  JsonApiErrorResponse,
} from './types/errors.ts';

/**
 * Type guard that checks if a value is a JSON:API error response.
 * Useful for distinguishing error responses from successful data responses.
 *
 * @example
 * ```ts
 * const body = await response.json();
 * if (isJsonApiErrorResponse(body)) {
 *   for (const error of body.errors) {
 *     console.error(`${error.title}: ${error.detail}`);
 *   }
 * }
 * ```
 */
export function isJsonApiErrorResponse(
  value: unknown,
): value is JsonApiErrorResponse {
  return (
    typeof value === 'object' &&
    value !== null &&
    'errors' in value &&
    Array.isArray((value as JsonApiErrorResponse).errors)
  );
}

/**
 * Thrown by {@link createJsonApiClient} for any non-2xx response.
 * `errors` is empty when the body was not a JSON:API error document
 * (e.g. a 415 with no body).
 */
export class JsonApiRequestError extends Error {
  readonly status: number;
  readonly errors: JsonApiError[];

  constructor(status: number, errors: JsonApiError[]) {
    super(errors[0]?.title ?? `Request failed with status ${status}`);
    this.name = 'JsonApiRequestError';
    this.status = status;
    this.errors = errors;
  }

  /** Whether any error in the response carries the given code. */
  hasCode(code: JsonApiErrorCode): boolean {
    return this.errors.some((error) => error.code === code);
  }

  /**
   * Groups errors by the field named in `source.pointer`
   * (e.g. "/data/attributes/email" -> "email"). Errors without a
   * pointer are omitted.
   */
  fieldErrors(): Record<string, JsonApiError[]> {
    const out: Record<string, JsonApiError[]> = {};
    for (const error of this.errors) {
      const field = error.source?.pointer?.split('/').pop();
      if (!field) continue;
      (out[field] ??= []).push(error);
    }
    return out;
  }
}

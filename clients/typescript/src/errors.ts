import type { JsonApiErrorResponse } from './types/errors.ts';

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

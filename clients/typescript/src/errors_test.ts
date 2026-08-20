import { assertEquals } from '@std/assert';
import { isJsonApiErrorResponse } from './errors.ts';
import { JsonApiErrorCodes } from './types/errors.ts';

Deno.test('isJsonApiErrorResponse', async (t) => {
  await t.step('returns true for single error', () => {
    assertEquals(
      isJsonApiErrorResponse({
        errors: [{ status: '404', title: 'Not Found' }],
      }),
      true,
    );
  });

  await t.step('returns true for multiple errors', () => {
    assertEquals(
      isJsonApiErrorResponse({
        errors: [
          { status: '400', detail: 'Invalid field' },
          { status: '400', detail: 'Missing value' },
        ],
      }),
      true,
    );
  });

  await t.step('returns true for error with all fields', () => {
    assertEquals(
      isJsonApiErrorResponse({
        errors: [
          {
            id: 'err-1',
            status: '400',
            code: JsonApiErrorCodes.INVALID_FILTER_VALUE,
            title: 'Bad Request',
            detail: "Cannot convert 'abc' to Int32",
            source: { parameter: 'filter[age]' },
            meta: { field: 'age', expectedType: 'Int32' },
          },
        ],
      }),
      true,
    );
  });

  await t.step('returns true for empty errors array', () => {
    assertEquals(isJsonApiErrorResponse({ errors: [] }), true);
  });

  await t.step('returns false for null', () => {
    assertEquals(isJsonApiErrorResponse(null), false);
  });

  await t.step('returns false for undefined', () => {
    assertEquals(isJsonApiErrorResponse(undefined), false);
  });

  await t.step('returns false for a string', () => {
    assertEquals(isJsonApiErrorResponse('error'), false);
  });

  await t.step('returns false for a number', () => {
    assertEquals(isJsonApiErrorResponse(0), false);
  });

  await t.step('returns false for a normal JSON:API data response', () => {
    assertEquals(
      isJsonApiErrorResponse({
        data: [{ id: '1', type: 'todos', attributes: { title: 'test' } }],
      }),
      false,
    );
  });

  await t.step('returns false when errors is not an array', () => {
    assertEquals(isJsonApiErrorResponse({ errors: 'not-an-array' }), false);
  });

  await t.step('returns false for empty object', () => {
    assertEquals(isJsonApiErrorResponse({}), false);
  });
});

Deno.test('JsonApiErrorCodes', async (t) => {
  await t.step('contains all expected error codes', () => {
    assertEquals(JsonApiErrorCodes.RESOURCE_NOT_FOUND, 'RESOURCE_NOT_FOUND');
    assertEquals(
      JsonApiErrorCodes.RESOURCE_ALREADY_EXISTS,
      'RESOURCE_ALREADY_EXISTS',
    );
    assertEquals(
      JsonApiErrorCodes.INVALID_FILTER_FIELD,
      'INVALID_FILTER_FIELD',
    );
    assertEquals(
      JsonApiErrorCodes.INVALID_FILTER_VALUE,
      'INVALID_FILTER_VALUE',
    );
    assertEquals(JsonApiErrorCodes.INCLUDE_NOT_ALLOWED, 'INCLUDE_NOT_ALLOWED');
    assertEquals(JsonApiErrorCodes.QUERY_TOO_COMPLEX, 'QUERY_TOO_COMPLEX');
    assertEquals(JsonApiErrorCodes.VALIDATION_FAILED, 'VALIDATION_FAILED');
    assertEquals(
      JsonApiErrorCodes.AUTHENTICATION_REQUIRED,
      'AUTHENTICATION_REQUIRED',
    );
  });

  await t.step('has 18 error codes', () => {
    assertEquals(Object.keys(JsonApiErrorCodes).length, 18);
  });
});

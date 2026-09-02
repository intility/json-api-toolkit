import { assertEquals } from '@std/assert';
import { isJsonApiErrorResponse, JsonApiRequestError } from './errors.ts';
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

  await t.step('has 19 error codes', () => {
    assertEquals(Object.keys(JsonApiErrorCodes).length, 19);
  });
});

Deno.test('JsonApiRequestError', async (t) => {
  await t.step('message falls back to the status when no errors', () => {
    const error = new JsonApiRequestError(500, []);
    assertEquals(error.message, 'Request failed with status 500');
    assertEquals(error.status, 500);
    assertEquals(error.errors, []);
  });

  await t.step('message uses the first error title', () => {
    const error = new JsonApiRequestError(404, [
      { status: '404', title: 'Not Found' },
    ]);
    assertEquals(error.message, 'Not Found');
  });

  await t.step('hasCode matches any error with that code', () => {
    const error = new JsonApiRequestError(400, [
      { code: JsonApiErrorCodes.VALIDATION_FAILED },
      { code: JsonApiErrorCodes.REQUIRED_FIELD_MISSING },
    ]);
    assertEquals(error.hasCode(JsonApiErrorCodes.VALIDATION_FAILED), true);
    assertEquals(error.hasCode(JsonApiErrorCodes.RESOURCE_NOT_FOUND), false);
  });

  await t.step("fieldErrors groups by the pointer's last segment", () => {
    const error = new JsonApiRequestError(400, [
      {
        code: JsonApiErrorCodes.VALIDATION_FAILED,
        source: { pointer: '/data/attributes/email' },
      },
      {
        code: JsonApiErrorCodes.REQUIRED_FIELD_MISSING,
        source: { pointer: '/data/attributes/email' },
      },
      {
        code: JsonApiErrorCodes.REQUIRED_FIELD_MISSING,
        source: { pointer: '/data/attributes/title' },
      },
      { code: JsonApiErrorCodes.AUTHENTICATION_REQUIRED }, // no pointer
    ]);
    const fields = error.fieldErrors();
    assertEquals(Object.keys(fields).sort(), ['email', 'title']);
    assertEquals(fields.email.length, 2);
    assertEquals(fields.title.length, 1);
  });
});

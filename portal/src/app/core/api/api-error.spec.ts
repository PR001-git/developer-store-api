import { HttpErrorResponse } from '@angular/common/http';
import { describe, expect, it } from 'vitest';
import { ApiError, parseValidationDetail, toApiError, toFieldPath } from './api-error';

describe('toApiError', () => {
  it('reads the {type, error, detail} body of an API error', () => {
    const error = toApiError(new HttpErrorResponse({
      status: 404,
      error: { type: 'ResourceNotFound', error: 'Resource not found', detail: 'Sale with ID 42 not found' },
    }));

    expect(error).toBeInstanceOf(ApiError);
    expect(error.status).toBe(404);
    expect(error.type).toBe('ResourceNotFound');
    expect(error.title).toBe('Resource not found');
    expect(error.detail).toBe('Sale with ID 42 not found');
    expect(error.fieldErrors).toEqual({});
  });

  it('splits a ValidationError detail into field paths', () => {
    const error = toApiError(new HttpErrorResponse({
      status: 400,
      error: {
        type: 'ValidationError',
        error: 'Invalid input data',
        detail: "SaleDate: 'Sale Date' must not be empty.; Items[0].Quantity: It's not possible to sell above 20 identical items",
      },
    }));

    expect(error.fieldErrors).toEqual({
      saleDate: ["'Sale Date' must not be empty."],
      'items[0].quantity': ["It's not possible to sell above 20 identical items"],
    });
  });

  it('answers NetworkError when the API cannot be reached', () => {
    const error = toApiError(new HttpErrorResponse({ status: 0 }));

    expect(error.type).toBe('NetworkError');
    expect(error.title).toBe("Can't reach the API");
  });

  it('answers UnexpectedResponse for a body that is not the API error format', () => {
    const error = toApiError(new HttpErrorResponse({
      status: 415,
      error: { title: 'Unsupported Media Type', status: 415 },
    }));

    expect(error.type).toBe('UnexpectedResponse');
    expect(error.detail).toBe('The API answered 415.');
  });
});

describe('parseValidationDetail', () => {
  it('keeps a failure without a property as a form-level message', () => {
    expect(parseValidationDetail('A sale must have at least one item')).toEqual({
      '': ['A sale must have at least one item'],
    });
  });

  it('collects several messages for one property', () => {
    expect(parseValidationDetail('Password: Too short.; Password: Needs a number.')).toEqual({
      password: ['Too short.', 'Needs a number.'],
    });
  });

  it('returns no errors for an empty detail', () => {
    expect(parseValidationDetail('')).toEqual({});
  });
});

describe('toFieldPath', () => {
  it('camel-cases each segment of a FluentValidation property', () => {
    expect(toFieldPath('Items[0].UnitPrice')).toBe('items[0].unitPrice');
  });

  it('drops the $. prefix of a model-binding path', () => {
    expect(toFieldPath('$.items[1].quantity')).toBe('items[1].quantity');
  });
});

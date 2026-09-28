import { HttpErrorResponse } from '@angular/common/http';
import { ApiErrorBody } from './api-models';

/** Messages by field path (`items[0].quantity`); the empty key holds messages that name no field. */
export type FieldErrors = Readonly<Record<string, readonly string[]>>;

/** Every failed API call reaches components as an ApiError, whatever went wrong. */
export class ApiError extends Error {
  constructor(
    readonly status: number,
    readonly type: string,
    readonly title: string,
    readonly detail: string,
    readonly fieldErrors: FieldErrors = {},
  ) {
    super(detail || title);
    this.name = 'ApiError';
  }
}

export function toApiError(response: HttpErrorResponse): ApiError {
  if (response.status === 0) {
    return new ApiError(0, 'NetworkError', "Can't reach the API",
      'Check that the API is running (docker compose up) and try again.');
  }

  const body: unknown = response.error;
  if (isApiErrorBody(body)) {
    const fieldErrors = body.type === 'ValidationError' ? parseValidationDetail(body.detail) : {};
    return new ApiError(response.status, body.type, body.error, body.detail, fieldErrors);
  }

  // The framework's own 404, 405 and 415 answers keep ASP.NET Core's default body.
  return new ApiError(response.status, 'UnexpectedResponse', 'Unexpected response',
    `The API answered ${response.status}.`);
}

/** Splits the API's `Property: message; Property: message` detail. */
export function parseValidationDetail(detail: string): FieldErrors {
  const errors: Record<string, string[]> = {};
  for (const part of detail.split('; ').map(text => text.trim()).filter(text => text !== '')) {
    const separator = part.indexOf(': ');
    const property = separator > 0 ? part.slice(0, separator) : '';
    const namesField = property !== '' && !/\s/.test(property);
    const key = namesField ? toFieldPath(property) : '';
    (errors[key] ??= []).push(namesField ? part.slice(separator + 2) : part);
  }
  return errors;
}

export function toFieldPath(property: string): string {
  return property
    .replace(/^\$\./, '')
    .split('.')
    .map(segment => segment.charAt(0).toLowerCase() + segment.slice(1))
    .join('.');
}

function isApiErrorBody(value: unknown): value is ApiErrorBody {
  if (typeof value !== 'object' || value === null) {
    return false;
  }
  const candidate = value as Record<string, unknown>;
  return typeof candidate['type'] === 'string'
    && typeof candidate['error'] === 'string'
    && typeof candidate['detail'] === 'string';
}

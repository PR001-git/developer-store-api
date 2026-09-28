import { describe, expect, it } from 'vitest';
import { describeFieldError } from './field-errors';

describe('describeFieldError', () => {
  it('prefers the server message', () => {
    expect(describeFieldError({ server: 'Sale number S-1 already exists', required: true }, 'Sale number')).toBe('Sale number S-1 already exists');
  });

  it.each([
    [{ required: true }, 'Quantity is required.'],
    [{ quantityMax: true }, "It's not possible to sell above 20 identical items."],
    [{ quantityMin: true }, 'Quantity must be a whole number of at least 1.'],
    [{ maxlength: { requiredLength: 50 } }, 'Quantity must have at most 50 characters.'],
  ])('describes %j', (errors, expected) => {
    expect(describeFieldError(errors, 'Quantity')).toBe(expected);
  });

  it('describes nothing for a valid control', () => {
    expect(describeFieldError(null, 'Quantity')).toBeNull();
  });
});

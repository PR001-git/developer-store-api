import { FormControl } from '@angular/forms';
import { describe, expect, it } from 'vitest';
import { passwordValidator, PHONE_PATTERN, unmetPasswordRules } from './credentials';

describe('credentials', () => {
  it('lists the password rules a value misses', () => {
    expect(unmetPasswordRules('weak')).toEqual(['length', 'uppercase', 'number', 'symbol']);
    expect(unmetPasswordRules('Portal@2026')).toEqual([]);
  });

  it('fails a control whose password misses a rule', () => {
    expect(passwordValidator(new FormControl('Portal2026'))).toEqual({ password: ['symbol'] });
    expect(passwordValidator(new FormControl('Portal@2026'))).toBeNull();
  });

  it('accepts only phones both API validators accept: + and 11 to 15 digits', () => {
    expect(PHONE_PATTERN.test('+5511987654321')).toBe(true);
    expect(PHONE_PATTERN.test('5511987654321')).toBe(false);
    expect(PHONE_PATTERN.test('+551198')).toBe(false);
  });
});

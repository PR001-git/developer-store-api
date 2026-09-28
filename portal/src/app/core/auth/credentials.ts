import { ValidatorFn } from '@angular/forms';

/** Both API validators must pass: UserValidator wants `+` and 11–15 digits; PhoneValidator is looser. */
export const PHONE_PATTERN = /^\+[1-9]\d{10,14}$/;

export const PASSWORD_RULES = [
  { key: 'length', label: 'At least 8 characters', test: (value: string) => value.length >= 8 },
  { key: 'uppercase', label: 'An uppercase letter', test: (value: string) => /[A-Z]/.test(value) },
  { key: 'lowercase', label: 'A lowercase letter', test: (value: string) => /[a-z]/.test(value) },
  { key: 'number', label: 'A number', test: (value: string) => /[0-9]/.test(value) },
  { key: 'symbol', label: 'A symbol: ! ? * . @ # $ % ^ & + =', test: (value: string) => /[!?*.@#$%^&+=]/.test(value) },
] as const;

export function unmetPasswordRules(value: string): string[] {
  return PASSWORD_RULES.filter(rule => !rule.test(value)).map(rule => rule.key);
}

export const passwordValidator: ValidatorFn = control => {
  const unmet = unmetPasswordRules(typeof control.value === 'string' ? control.value : '');
  return unmet.length > 0 ? { password: unmet } : null;
};

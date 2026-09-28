import { ValidationErrors } from '@angular/forms';

/** The one place form error copy lives, so every screen words errors the same way. */
export function describeFieldError(errors: ValidationErrors | null, label: string): string | null {
  if (!errors) return null;
  if (typeof errors['server'] === 'string') return errors['server'];
  if (errors['required']) return `${label} is required.`;
  if (errors['pattern']) return `${label} must be a valid ID (a GUID).`;
  if (errors['email']) return 'Enter an email address like name@example.com.';
  if (errors['maxlength']) return `${label} must have at most ${errors['maxlength'].requiredLength} characters.`;
  if (errors['minlength']) return `${label} must have at least ${errors['minlength'].requiredLength} characters.`;
  if (errors['quantityMin']) return 'Quantity must be a whole number of at least 1.';
  if (errors['quantityMax']) return "It's not possible to sell above 20 identical items.";
  if (errors['pricePositive']) return 'Unit price must be above 0.';
  if (errors['priceDecimals']) return 'Unit price can have at most 2 decimal places.';
  if (errors['phone']) return 'Use the international format, e.g. +5511987654321.';
  if (errors['password']) return 'The password must meet every rule below.';
  return `${label} is invalid.`;
}

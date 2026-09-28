/** The portal's one place for locale and currency (Decision 12). */
export const LOCALE = 'en-US';
export const CURRENCY = 'BRL';

const money = new Intl.NumberFormat(LOCALE, { style: 'currency', currency: CURRENCY });
const dateTime = new Intl.DateTimeFormat(LOCALE, { dateStyle: 'medium', timeStyle: 'short' });

export function formatMoney(value: number): string {
  return money.format(value);
}

export function formatDateTime(iso: string): string {
  return dateTime.format(new Date(iso));
}

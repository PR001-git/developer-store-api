import { describe, expect, it } from 'vitest';
import { formatDateTime, formatMoney } from './format';

describe('format', () => {
  it('formats money in BRL', () => {
    expect(formatMoney(20.25)).toBe('R$20.25');
    expect(formatMoney(0)).toBe('R$0.00');
  });

  it('formats an instant in local time', () => {
    // Intl may put a narrow no-break space before AM/PM.
    expect(formatDateTime('2026-09-24T14:30:00Z').replace(/ /g, ' ')).toBe('Sep 24, 2026, 11:30 AM');
  });
});

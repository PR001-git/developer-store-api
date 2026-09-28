import { describe, expect, it } from 'vitest';
import { endOfDayIso, fromDateTimeLocal, parseDay, startOfDayIso, toDateTimeLocal, toOffsetIso } from './local-time';

describe('local time (tests run in America/Sao_Paulo, UTC-03:00)', () => {
  it('runs in the pinned zone', () => {
    expect(new Date(2026, 8, 24).getTimezoneOffset()).toBe(180);
  });

  it('writes a local date with its offset', () => {
    expect(toOffsetIso(new Date(2026, 8, 24, 11, 30))).toBe('2026-09-24T11:30:00.000-03:00');
  });

  it('turns a day into its first and last local instant', () => {
    expect(startOfDayIso('2026-09-24')).toBe('2026-09-24T00:00:00.000-03:00');
    expect(endOfDayIso('2026-09-24')).toBe('2026-09-24T23:59:59.999-03:00');
  });

  it('rejects a day that does not exist', () => {
    expect(parseDay('2026-02-30')).toBeNull();
    expect(startOfDayIso('')).toBeNull();
    expect(endOfDayIso('24/09/2026')).toBeNull();
  });

  it('shows a UTC instant as local datetime-local text', () => {
    expect(toDateTimeLocal('2026-09-24T14:30:00Z')).toBe('2026-09-24T11:30');
  });

  it('reads datetime-local text as local time', () => {
    expect(fromDateTimeLocal('2026-09-24T11:30')?.toISOString()).toBe('2026-09-24T14:30:00.000Z');
    expect(fromDateTimeLocal('not a date')).toBeNull();
  });
});

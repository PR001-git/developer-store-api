const pad = (value: number, length = 2): string => String(value).padStart(length, '0');

/** `2026-09-24T11:30:00.000-03:00`: the local wall-clock time with its offset, so the API converts it to UTC (R13). */
export function toOffsetIso(date: Date): string {
  const offset = -date.getTimezoneOffset();
  const sign = offset >= 0 ? '+' : '-';
  const absolute = Math.abs(offset);
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`
    + `T${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}.${pad(date.getMilliseconds(), 3)}`
    + `${sign}${pad(Math.floor(absolute / 60))}:${pad(absolute % 60)}`;
}

/** Reads `YYYY-MM-DD` as local midnight, or null when the text isn't a real day. */
export function parseDay(day: string): Date | null {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(day);
  if (!match) {
    return null;
  }
  const [year, month, date] = [Number(match[1]), Number(match[2]), Number(match[3])];
  const parsed = new Date(year, month - 1, date);
  return parsed.getMonth() === month - 1 && parsed.getDate() === date ? parsed : null;
}

export function startOfDayIso(day: string): string | null {
  const parsed = parseDay(day);
  return parsed ? toOffsetIso(parsed) : null;
}

export function endOfDayIso(day: string): string | null {
  const parsed = parseDay(day);
  if (!parsed) {
    return null;
  }
  parsed.setHours(23, 59, 59, 999);
  return toOffsetIso(parsed);
}

/** Reads the value of an `<input type="datetime-local">` as local time. */
export function fromDateTimeLocal(value: string): Date | null {
  const match = /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})$/.exec(value);
  if (!match) {
    return null;
  }
  const [year, month, date, hours, minutes] = match.slice(1).map(Number);
  return new Date(year, month - 1, date, hours, minutes);
}

/** Formats an instant for an `<input type="datetime-local">`, in local time. */
export function toDateTimeLocal(iso: string): string {
  const date = new Date(iso);
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

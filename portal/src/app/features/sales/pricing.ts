/** A preview of the API's R1 and R3 rules while a line is edited. After a save, the server's numbers are shown. */

export const MAX_QUANTITY = 20;
export type DiscountPercentage = 0 | 10 | 20;

export interface LinePreview {
  readonly gross: number;
  readonly discountPercentage: DiscountPercentage;
  readonly discountAmount: number;
  readonly total: number;
}

export interface SalePreview {
  readonly gross: number;
  readonly discount: number;
  readonly total: number;
}

export interface TierHint {
  readonly itemsToNext: number;
  readonly nextPercentage: 10 | 20;
}

const isValidQuantity = (quantity: number): boolean =>
  Number.isInteger(quantity) && quantity >= 1 && quantity <= MAX_QUANTITY;

export function discountPercentage(quantity: number): DiscountPercentage {
  if (!isValidQuantity(quantity)) {
    throw new RangeError(`Quantity must be a whole number from 1 to ${MAX_QUANTITY}.`);
  }
  if (quantity >= 10) return 20;
  if (quantity >= 4) return 10;
  return 0;
}

export function previewLine(quantity: number | null, unitPrice: number | null): LinePreview | null {
  if (quantity === null || unitPrice === null || !isValidQuantity(quantity)) {
    return null;
  }
  const priceCents = Math.round(unitPrice * 100);
  if (priceCents <= 0 || Math.abs(unitPrice * 100 - priceCents) > 1e-6) {
    return null;
  }

  const percentage = discountPercentage(quantity);
  const grossCents = quantity * priceCents;
  // Amounts are positive, so Math.round's "half up" is the API's MidpointRounding.AwayFromZero.
  const discountCents = Math.round((grossCents * percentage) / 100);
  return {
    gross: grossCents / 100,
    discountPercentage: percentage,
    discountAmount: discountCents / 100,
    total: (grossCents - discountCents) / 100,
  };
}

export function previewSale(lines: readonly (LinePreview | null)[]): SalePreview {
  const cents = (value: number): number => Math.round(value * 100);
  const complete = lines.filter((line): line is LinePreview => line !== null);
  const gross = complete.reduce((sum, line) => sum + cents(line.gross), 0);
  const discount = complete.reduce((sum, line) => sum + cents(line.discountAmount), 0);
  return { gross: gross / 100, discount: discount / 100, total: (gross - discount) / 100 };
}

export function nextTier(quantity: number | null): TierHint | null {
  if (quantity === null || !isValidQuantity(quantity) || quantity >= 10) {
    return null;
  }
  return quantity < 4
    ? { itemsToNext: 4 - quantity, nextPercentage: 10 }
    : { itemsToNext: 10 - quantity, nextPercentage: 20 };
}

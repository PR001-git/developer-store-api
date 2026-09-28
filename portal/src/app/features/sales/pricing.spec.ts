import { describe, expect, it } from 'vitest';
import { discountPercentage, nextTier, previewLine, previewSale } from './pricing';

describe('discountPercentage (R1, with quantity 4 at 10% per D3)', () => {
  it.each([
    [1, 0], [3, 0], [4, 10], [9, 10], [10, 20], [20, 20],
  ])('gives %i items %i%%', (quantity, expected) => {
    expect(discountPercentage(quantity)).toBe(expected);
  });

  it.each([0, 21, 2.5])('rejects %s items', quantity => {
    expect(() => discountPercentage(quantity)).toThrow(RangeError);
  });
});

describe('previewLine (R3)', () => {
  it('prices the spec example: 5 items at 4.50 get 10% off', () => {
    expect(previewLine(5, 4.5)).toEqual({ gross: 22.5, discountPercentage: 10, discountAmount: 2.25, total: 20.25 });
  });

  it('rounds a half cent away from zero, as the API does', () => {
    // 5 × 0.05 = 0.25; 10% is 0.025, which is 0.03 away from zero (banker's rounding would give 0.02).
    expect(previewLine(5, 0.05)?.discountAmount).toBe(0.03);
  });

  it('has no preview while the line is incomplete or invalid', () => {
    expect(previewLine(null, 4.5)).toBeNull();
    expect(previewLine(5, null)).toBeNull();
    expect(previewLine(21, 4.5)).toBeNull();
    expect(previewLine(5, 0)).toBeNull();
    expect(previewLine(5, 1.234)).toBeNull();
  });
});

describe('previewSale', () => {
  it('adds up the complete lines and skips the rest', () => {
    expect(previewSale([previewLine(5, 4.5), previewLine(12, 7.9), null])).toEqual({
      gross: 117.3, discount: 21.21, total: 96.09,
    });
  });
});

describe('nextTier', () => {
  it('says how many more items reach the next tier', () => {
    expect(nextTier(3)).toEqual({ itemsToNext: 1, nextPercentage: 10 });
    expect(nextTier(4)).toEqual({ itemsToNext: 6, nextPercentage: 20 });
    expect(nextTier(9)).toEqual({ itemsToNext: 1, nextPercentage: 20 });
  });

  it('has no hint at the top tier or for an invalid quantity', () => {
    expect(nextTier(10)).toBeNull();
    expect(nextTier(null)).toBeNull();
    expect(nextTier(0)).toBeNull();
  });
});

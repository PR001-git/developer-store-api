import { describe, expect, it } from 'vitest';
import {
  DEFAULT_QUERY, formatOrder, parseOrder, queryFromParams, queryToHttpParams, queryToParams, toMatchPattern, toggleSort,
} from './sales-query';

describe('parseOrder', () => {
  it('reads the fields and directions of _order', () => {
    expect(parseOrder('totalAmount desc, customerName')).toEqual([
      { field: 'totalAmount', direction: 'desc' },
      { field: 'customerName', direction: 'asc' },
    ]);
  });

  it('matches field names in any case, as the API does', () => {
    expect(parseOrder('SALEDATE ASC')).toEqual([{ field: 'saleDate', direction: 'asc' }]);
  });

  it('falls back to newest first for a missing or unusable order', () => {
    expect(parseOrder(null)).toEqual([{ field: 'saleDate', direction: 'desc' }]);
    expect(parseOrder('items desc, , id')).toEqual([{ field: 'saleDate', direction: 'desc' }]);
  });

  it('drops a repeated field', () => {
    expect(parseOrder('branchName, branchName desc')).toEqual([{ field: 'branchName', direction: 'asc' }]);
  });
});

describe('toggleSort', () => {
  const byDate = [{ field: 'saleDate', direction: 'desc' }] as const;

  it('sorts by a new column, money and dates highest first', () => {
    expect(toggleSort(byDate, 'totalAmount', false)).toEqual([{ field: 'totalAmount', direction: 'desc' }]);
    expect(toggleSort(byDate, 'customerName', false)).toEqual([{ field: 'customerName', direction: 'asc' }]);
  });

  it('flips the primary column', () => {
    expect(toggleSort(byDate, 'saleDate', false)).toEqual([{ field: 'saleDate', direction: 'asc' }]);
  });

  it('adds a secondary sort, then flips it in place', () => {
    const two = toggleSort(byDate, 'branchName', true);
    expect(two).toEqual([{ field: 'saleDate', direction: 'desc' }, { field: 'branchName', direction: 'asc' }]);
    expect(toggleSort(two, 'branchName', true)).toEqual([
      { field: 'saleDate', direction: 'desc' },
      { field: 'branchName', direction: 'desc' },
    ]);
  });
});

describe('the URL form of the query', () => {
  it('omits every default, so /sales stays clean', () => {
    expect(queryToParams(DEFAULT_QUERY)).toEqual({});
  });

  it('round-trips a filtered, sorted page', () => {
    const query = {
      ...DEFAULT_QUERY,
      page: 2,
      size: 20,
      order: parseOrder('totalAmount desc'),
      customerName: 'silva',
      status: 'open' as const,
      soldFrom: '2026-09-01',
      maxTotal: '100',
    };

    const params = queryToParams(query);
    expect(params).toEqual({
      _page: '2', _size: '20', _order: 'totalAmount desc', customerName: 'silva',
      isCancelled: 'false', _minSaleDate: '2026-09-01', _maxTotalAmount: '100',
    });
    expect(queryFromParams(params)).toEqual(query);
  });

  it('repairs a page or size the portal would not offer', () => {
    const query = queryFromParams({ _page: '0', _size: '7' });
    expect(query.page).toBe(1);
    expect(query.size).toBe(10);
  });
});

describe('the API form of the query', () => {
  it('always sends paging and ordering', () => {
    expect(queryToHttpParams(DEFAULT_QUERY).toString()).toBe('_page=1&_size=10&_order=saleDate%20desc');
  });

  it('sends text filters as contains unless the user typed a wildcard', () => {
    expect(toMatchPattern(' silva ')).toBe('*silva*');
    expect(toMatchPattern('S-0001*')).toBe('S-0001*');
    expect(toMatchPattern('   ')).toBe('');
  });

  it('turns local days into offset instants and status into isCancelled', () => {
    const params = queryToHttpParams({
      ...DEFAULT_QUERY, soldFrom: '2026-09-24', soldTo: '2026-09-24', status: 'cancelled', minTotal: '20.00',
    });

    expect(params.get('_minSaleDate')).toBe('2026-09-24T00:00:00.000-03:00');
    expect(params.get('_maxSaleDate')).toBe('2026-09-24T23:59:59.999-03:00');
    expect(params.get('isCancelled')).toBe('true');
    expect(params.get('_minTotalAmount')).toBe('20.00');
    expect(params.has('customerName')).toBe(false);
  });
});

describe('formatOrder', () => {
  it('writes every direction explicitly', () => {
    expect(formatOrder(parseOrder('customerName, totalAmount desc'))).toBe('customerName asc, totalAmount desc');
  });
});

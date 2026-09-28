import { HttpParams } from '@angular/common/http';
import { Params } from '@angular/router';
import { StrictParameterCodec } from '../../core/api/strict-parameter-codec';
import { endOfDayIso, startOfDayIso } from '../../core/time/local-time';

/** The fields `_order` accepts (the API's SaleOrderParser). Item count isn't one of them. */
export const SORT_FIELDS = ['saleNumber', 'saleDate', 'customerName', 'branchName', 'totalAmount', 'isCancelled'] as const;
export type SortField = (typeof SORT_FIELDS)[number];
export type SortDirection = 'asc' | 'desc';
export interface SortClause {
  readonly field: SortField;
  readonly direction: SortDirection;
}
export type StatusFilter = 'all' | 'open' | 'cancelled';
export const PAGE_SIZES = [10, 20, 50, 100] as const;

/** What the list shows. Text, ids and totals are kept as typed; days are `YYYY-MM-DD` in local time. */
export interface SalesQuery {
  readonly page: number;
  readonly size: number;
  readonly order: readonly SortClause[];
  readonly saleNumber: string;
  readonly customerName: string;
  readonly branchName: string;
  readonly customerId: string;
  readonly branchId: string;
  readonly status: StatusFilter;
  readonly soldFrom: string;
  readonly soldTo: string;
  readonly minTotal: string;
  readonly maxTotal: string;
}

export const DEFAULT_ORDER: readonly SortClause[] = [{ field: 'saleDate', direction: 'desc' }];

export const DEFAULT_QUERY: SalesQuery = {
  page: 1, size: 10, order: DEFAULT_ORDER,
  saleNumber: '', customerName: '', branchName: '', customerId: '', branchId: '',
  status: 'all', soldFrom: '', soldTo: '', minTotal: '', maxTotal: '',
};

const HIGHEST_FIRST: ReadonlySet<SortField> = new Set<SortField>(['saleDate', 'totalAmount']);

export function parseOrder(text: string | null | undefined): readonly SortClause[] {
  const clauses: SortClause[] = [];
  for (const raw of (text ?? '').split(',')) {
    const [name = '', direction = 'asc', ...extra] = raw.trim().split(/\s+/);
    const field = SORT_FIELDS.find(candidate => candidate.toLowerCase() === name.toLowerCase());
    const normalized = direction.toLowerCase();
    if (!field || extra.length > 0 || (normalized !== 'asc' && normalized !== 'desc')
      || clauses.some(clause => clause.field === field)) {
      continue;
    }
    clauses.push({ field, direction: normalized });
  }
  return clauses.length > 0 ? clauses : DEFAULT_ORDER;
}

export function formatOrder(order: readonly SortClause[]): string {
  return order.map(clause => `${clause.field} ${clause.direction}`).join(', ');
}

/** A header click sorts by that column alone; Shift+click adds it as the next sort, or flips it. */
export function toggleSort(order: readonly SortClause[], field: SortField, additive: boolean): readonly SortClause[] {
  const index = order.findIndex(clause => clause.field === field);
  const flip = (clause: SortClause): SortClause =>
    ({ field: clause.field, direction: clause.direction === 'asc' ? 'desc' : 'asc' });
  const fresh: SortClause = { field, direction: HIGHEST_FIRST.has(field) ? 'desc' : 'asc' };

  if (!additive) {
    return index === 0 ? [flip(order[0])] : [fresh];
  }
  return index === -1 ? [...order, fresh] : order.map((clause, i) => (i === index ? flip(clause) : clause));
}

export function queryFromParams(params: Params): SalesQuery {
  const text = (key: string): string => (typeof params[key] === 'string' ? params[key] : '');
  const page = Number.parseInt(text('_page'), 10);
  const size = Number.parseInt(text('_size'), 10);
  const isCancelled = text('isCancelled');

  return {
    page: Number.isInteger(page) && page >= 1 ? page : 1,
    size: (PAGE_SIZES as readonly number[]).includes(size) ? size : 10,
    order: parseOrder(text('_order')),
    saleNumber: text('saleNumber'),
    customerName: text('customerName'),
    branchName: text('branchName'),
    customerId: text('customerId'),
    branchId: text('branchId'),
    status: isCancelled === 'true' ? 'cancelled' : isCancelled === 'false' ? 'open' : 'all',
    soldFrom: text('_minSaleDate'),
    soldTo: text('_maxSaleDate'),
    minTotal: text('_minTotalAmount'),
    maxTotal: text('_maxTotalAmount'),
  };
}

/** The router query parameters for a query, without its defaults. */
export function queryToParams(query: SalesQuery): Params {
  const params: Params = {};
  const put = (key: string, value: string): void => {
    if (value.trim() !== '') {
      params[key] = value.trim();
    }
  };

  if (query.page !== 1) params['_page'] = String(query.page);
  if (query.size !== 10) params['_size'] = String(query.size);
  const order = formatOrder(query.order);
  if (order !== formatOrder(DEFAULT_ORDER)) params['_order'] = order;
  put('saleNumber', query.saleNumber);
  put('customerName', query.customerName);
  put('branchName', query.branchName);
  put('customerId', query.customerId);
  put('branchId', query.branchId);
  if (query.status !== 'all') params['isCancelled'] = String(query.status === 'cancelled');
  put('_minSaleDate', query.soldFrom);
  put('_maxSaleDate', query.soldTo);
  put('_minTotalAmount', query.minTotal);
  put('_maxTotalAmount', query.maxTotal);
  return params;
}

/** Plain text means "contains"; text with a `*` is sent as typed (`value*`, `*value`, `*value*`). */
export function toMatchPattern(text: string): string {
  const trimmed = text.trim();
  return trimmed === '' || trimmed.includes('*') ? trimmed : `*${trimmed}*`;
}

export function queryToHttpParams(query: SalesQuery): HttpParams {
  let params = new HttpParams({ encoder: new StrictParameterCodec() })
    .set('_page', query.page)
    .set('_size', query.size)
    .set('_order', formatOrder(query.order));
  const set = (key: string, value: string | null): void => {
    if (value) {
      params = params.set(key, value);
    }
  };

  set('saleNumber', toMatchPattern(query.saleNumber));
  set('customerName', toMatchPattern(query.customerName));
  set('branchName', toMatchPattern(query.branchName));
  set('customerId', query.customerId.trim());
  set('branchId', query.branchId.trim());
  if (query.status !== 'all') {
    params = params.set('isCancelled', query.status === 'cancelled');
  }
  set('_minSaleDate', startOfDayIso(query.soldFrom));
  set('_maxSaleDate', endOfDayIso(query.soldTo));
  set('_minTotalAmount', query.minTotal.trim());
  set('_maxTotalAmount', query.maxTotal.trim());
  return params;
}

const FILTER_KEYS = [
  'saleNumber', 'customerName', 'branchName', 'customerId', 'branchId', 'status', 'soldFrom', 'soldTo', 'minTotal', 'maxTotal',
] as const;

/** Picks the empty state: "No sales yet." or "No sales match these filters." */
export function hasFilters(query: SalesQuery): boolean {
  return FILTER_KEYS.some(key => query[key] !== DEFAULT_QUERY[key]);
}

import { HttpParams, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { aSale } from '../../testing/sale-fixture';
import { SalesApi } from './sales-api';

describe('SalesApi', () => {
  let api: SalesApi;
  let backend: HttpTestingController;
  const SALE = aSale();

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    api = TestBed.inject(SalesApi);
    backend = TestBed.inject(HttpTestingController);
  });

  it('lists sales with the given parameters and unwraps the page', () => {
    let page: unknown;
    api.list(new HttpParams().set('_page', 2).set('customerName', '*silva*')).subscribe(result => (page = result));

    const request = backend.expectOne(candidate => candidate.url === '/api/sales');
    expect(request.request.params.get('_page')).toBe('2');
    expect(request.request.params.get('customerName')).toBe('*silva*');
    request.flush({ success: true, message: 'Sales retrieved successfully', data: [SALE], currentPage: 2, totalPages: 3, totalItems: 21 });

    expect(page).toEqual({ sales: [SALE], currentPage: 2, totalPages: 3, totalItems: 21 });
  });

  it('unwraps a single sale', () => {
    let sale: unknown;
    api.get(SALE.id).subscribe(result => (sale = result));
    backend.expectOne(`/api/sales/${SALE.id}`).flush({ success: true, message: 'Sale retrieved successfully', data: SALE });

    expect(sale).toEqual(SALE);
  });

  it.each([
    ['cancel', 'PATCH', `/api/sales/${SALE.id}/cancel`, () => TestBed.inject(SalesApi).cancel(SALE.id)],
    ['cancelItem', 'PATCH', `/api/sales/${SALE.id}/items/i1/cancel`, () => TestBed.inject(SalesApi).cancelItem(SALE.id, 'i1')],
    ['update', 'PUT', `/api/sales/${SALE.id}`, () => TestBed.inject(SalesApi).update(SALE.id, {} as never)],
    ['create', 'POST', '/api/sales', () => TestBed.inject(SalesApi).create({} as never)],
  ])('%s calls %s %s and returns the sale', (_name, method, url, call) => {
    let sale: unknown;
    call().subscribe(result => (sale = result));
    const request = backend.expectOne(url);
    expect(request.request.method).toBe(method);
    request.flush({ success: true, message: 'ok', data: SALE });

    expect(sale).toEqual(SALE);
  });

  it('deletes a sale', () => {
    let done = false;
    api.delete(SALE.id).subscribe(() => (done = true));
    const request = backend.expectOne(`/api/sales/${SALE.id}`);
    expect(request.request.method).toBe('DELETE');
    request.flush({ success: true, message: 'Sale deleted successfully' });

    expect(done).toBe(true);
  });
});

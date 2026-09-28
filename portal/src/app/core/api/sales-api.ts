import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { HttpParams } from '@angular/common/http';
import { map, Observable } from 'rxjs';
import {
  ApiResponse, ApiResponseWithData, CreateSaleRequest, PaginatedResponse, Sale, SalesPage, UpdateSaleRequest,
} from './api-models';

@Injectable({ providedIn: 'root' })
export class SalesApi {
  private readonly http = inject(HttpClient);

  /** The list screen builds the parameters with queryToHttpParams, so core never depends on a feature. */
  list(params: HttpParams): Observable<SalesPage> {
    return this.http.get<PaginatedResponse<Sale>>('/api/sales', { params }).pipe(
      map(response => ({
        sales: response.data,
        currentPage: response.currentPage,
        totalPages: response.totalPages,
        totalItems: response.totalItems,
      })),
    );
  }

  get(id: string): Observable<Sale> {
    return this.unwrap(this.http.get<ApiResponseWithData<Sale>>(`/api/sales/${id}`));
  }

  create(body: CreateSaleRequest): Observable<Sale> {
    return this.unwrap(this.http.post<ApiResponseWithData<Sale>>('/api/sales', body));
  }

  update(id: string, body: UpdateSaleRequest): Observable<Sale> {
    return this.unwrap(this.http.put<ApiResponseWithData<Sale>>(`/api/sales/${id}`, body));
  }

  cancel(id: string): Observable<Sale> {
    return this.unwrap(this.http.patch<ApiResponseWithData<Sale>>(`/api/sales/${id}/cancel`, null));
  }

  cancelItem(id: string, itemId: string): Observable<Sale> {
    return this.unwrap(this.http.patch<ApiResponseWithData<Sale>>(`/api/sales/${id}/items/${itemId}/cancel`, null));
  }

  delete(id: string): Observable<void> {
    return this.http.delete<ApiResponse>(`/api/sales/${id}`).pipe(map(() => undefined));
  }

  private unwrap(response: Observable<ApiResponseWithData<Sale>>): Observable<Sale> {
    return response.pipe(map(body => body.data));
  }
}

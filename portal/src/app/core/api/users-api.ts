import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiResponse, ApiResponseWithData, CreateUserRequest, User } from './api-models';

@Injectable({ providedIn: 'root' })
export class UsersApi {
  private readonly http = inject(HttpClient);

  signUp(body: CreateUserRequest): Observable<User> {
    return this.http.post<ApiResponseWithData<User>>('/api/users', body).pipe(map(response => response.data));
  }

  get(id: string): Observable<User> {
    return this.http.get<ApiResponseWithData<User>>(`/api/users/${id}`).pipe(map(response => response.data));
  }

  delete(id: string): Observable<void> {
    return this.http.delete<ApiResponse>(`/api/users/${id}`).pipe(map(() => undefined));
  }
}

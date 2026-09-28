import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiResponseWithData, AuthenticateRequest, AuthenticateResponse } from './api-models';

@Injectable({ providedIn: 'root' })
export class AuthApi {
  private readonly http = inject(HttpClient);

  signIn(body: AuthenticateRequest): Observable<AuthenticateResponse> {
    return this.http.post<ApiResponseWithData<AuthenticateResponse>>('/api/auth', body).pipe(map(response => response.data));
  }
}

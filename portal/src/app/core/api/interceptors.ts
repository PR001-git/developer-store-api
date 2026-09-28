import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { SessionStore } from '../auth/session-store';
import { toApiError } from './api-error';

const SIGN_IN_URL = '/api/auth';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const session = inject(SessionStore).session();
  if (!session || !request.url.startsWith('/api/') || request.url === SIGN_IN_URL) {
    return next(request);
  }
  return next(request.clone({ setHeaders: { Authorization: `Bearer ${session.token}` } }));
};

export const apiErrorInterceptor: HttpInterceptorFn = (request, next) => {
  const store = inject(SessionStore);
  const router = inject(Router);

  return next(request).pipe(
    catchError((failure: unknown) => {
      if (!(failure instanceof HttpErrorResponse)) {
        return throwError(() => failure);
      }
      if (failure.status === 401 && request.url !== SIGN_IN_URL && store.isSignedIn()) {
        store.end('rejected');
        void router.navigate(['/sign-in'], { queryParams: { returnUrl: router.url } });
      }
      return throwError(() => toApiError(failure));
    }),
  );
};

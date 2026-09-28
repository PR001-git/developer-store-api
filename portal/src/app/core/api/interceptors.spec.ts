import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { fakeJwt } from '../../testing/fake-jwt';
import { MemoryStorage } from '../../testing/memory-storage';
import { CLOCK, SESSION_STORAGE, SessionStore } from '../auth/session-store';
import { ApiError } from './api-error';
import { apiErrorInterceptor, authInterceptor } from './interceptors';

describe('interceptors', () => {
  let http: HttpClient;
  let backend: HttpTestingController;
  let store: SessionStore;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor, apiErrorInterceptor])),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: SESSION_STORAGE, useValue: new MemoryStorage() },
        { provide: CLOCK, useValue: () => 0 },
      ],
    });
    http = TestBed.inject(HttpClient);
    backend = TestBed.inject(HttpTestingController);
    store = TestBed.inject(SessionStore);
  });

  const signIn = () => store.start({
    token: fakeJwt({ nameid: '6f1c1c52-8a7e-4a4f-9c1d-2d8f0b9d3e11', exp: 9_999_999_999 }),
    email: 'ana@developerstore.test', name: 'Ana Souza', role: 'Customer',
  });

  it('sends the bearer token to the API', () => {
    const session = signIn();
    http.get('/api/sales').subscribe();

    expect(backend.expectOne('/api/sales').request.headers.get('Authorization')).toBe(`Bearer ${session.token}`);
  });

  it('sends no token to the sign-in endpoint', () => {
    signIn();
    http.post('/api/auth', {}).subscribe();

    expect(backend.expectOne('/api/auth').request.headers.has('Authorization')).toBe(false);
  });

  it('turns an HTTP failure into an ApiError', () => {
    let failure: unknown;
    http.get('/api/sales/1').subscribe({ error: error => (failure = error) });

    backend.expectOne('/api/sales/1').flush(
      { type: 'ResourceNotFound', error: 'Resource not found', detail: 'Sale with ID 1 not found' },
      { status: 404, statusText: 'Not Found' });

    expect(failure).toBeInstanceOf(ApiError);
    expect((failure as ApiError).type).toBe('ResourceNotFound');
  });

  it('ends a rejected session and sends the user to sign in', () => {
    signIn();
    const router = TestBed.inject(Router);
    const navigate = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    http.get('/api/sales').subscribe({ error: () => undefined });
    backend.expectOne('/api/sales').flush(
      { type: 'AuthenticationError', error: 'Authentication failed', detail: 'The token is invalid' },
      { status: 401, statusText: 'Unauthorized' });

    expect(store.isSignedIn()).toBe(false);
    expect(store.endReason()).toBe('rejected');
    expect(navigate).toHaveBeenCalledWith(['/sign-in'], { queryParams: { returnUrl: '/' } });
  });

  it('leaves the session alone when sign-in itself answers 401', () => {
    http.post('/api/auth', {}).subscribe({ error: () => undefined });
    backend.expectOne('/api/auth').flush(
      { type: 'AuthenticationError', error: 'Authentication failed', detail: 'Invalid credentials' },
      { status: 401, statusText: 'Unauthorized' });

    expect(store.endReason()).toBeNull();
  });
});

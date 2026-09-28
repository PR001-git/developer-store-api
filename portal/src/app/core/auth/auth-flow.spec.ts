import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { fakeJwt } from '../../testing/fake-jwt';
import { MemoryStorage } from '../../testing/memory-storage';
import { AuthFlow } from './auth-flow';
import { SESSION_STORAGE, SessionStore } from './session-store';

describe('AuthFlow', () => {
  let flow: AuthFlow;
  let backend: HttpTestingController;
  const USER_ID = '6f1c1c52-8a7e-4a4f-9c1d-2d8f0b9d3e11';
  const auth = { token: fakeJwt({ nameid: USER_ID, exp: 9_999_999_999 }), email: 'ana@developerstore.test', name: 'Ana Souza', role: 'Customer' };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), { provide: SESSION_STORAGE, useValue: new MemoryStorage() }],
    });
    flow = TestBed.inject(AuthFlow);
    backend = TestBed.inject(HttpTestingController);
  });

  it('signs up an active customer, then signs in with the same credentials', () => {
    flow.signUp({ username: 'Ana Souza', email: 'ana@developerstore.test', phone: '+5511987654321', password: 'Portal@2026' }).subscribe();

    const signUp = backend.expectOne('/api/users');
    expect(signUp.request.body).toEqual({
      username: 'Ana Souza', email: 'ana@developerstore.test', phone: '+5511987654321', password: 'Portal@2026',
      status: 'Active', role: 'Customer',
    });
    signUp.flush({ success: true, message: 'User created successfully', data: { id: USER_ID } }, { status: 201, statusText: 'Created' });

    const signIn = backend.expectOne('/api/auth');
    expect(signIn.request.body).toEqual({ email: 'ana@developerstore.test', password: 'Portal@2026' });
    signIn.flush({ success: true, message: 'User authenticated successfully', data: auth });

    expect(TestBed.inject(SessionStore).session()?.userId).toBe(USER_ID);
  });

  it('deletes the signed-in account and ends the session', () => {
    TestBed.inject(SessionStore).start(auth);
    flow.deleteAccount().subscribe();

    const request = backend.expectOne(`/api/users/${USER_ID}`);
    expect(request.request.method).toBe('DELETE');
    request.flush({ success: true, message: 'User deleted successfully' });

    expect(TestBed.inject(SessionStore).endReason()).toBe('account-deleted');
  });
});

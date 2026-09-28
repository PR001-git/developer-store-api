import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, provideRouter, Router, RouterStateSnapshot, UrlTree } from '@angular/router';
import { beforeEach, describe, expect, it } from 'vitest';
import { fakeJwt } from '../../testing/fake-jwt';
import { MemoryStorage } from '../../testing/memory-storage';
import { authGuard, guestGuard, safeReturnUrl } from './guards';
import { CLOCK, SESSION_STORAGE, SessionStore } from './session-store';

describe('guards', () => {
  let now: number;
  const route = {} as ActivatedRouteSnapshot;
  const state = { url: '/sales?customerName=silva' } as RouterStateSnapshot;
  const signIn = () => TestBed.inject(SessionStore).start({
    token: fakeJwt({ nameid: '6f1c1c52-8a7e-4a4f-9c1d-2d8f0b9d3e11', exp: 2_000 }),
    email: 'ana@developerstore.test', name: 'Ana Souza', role: 'Customer',
  });
  const serialize = (result: unknown) => TestBed.inject(Router).serializeUrl(result as UrlTree);

  beforeEach(() => {
    now = 1_000_000;
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: SESSION_STORAGE, useValue: new MemoryStorage() },
        { provide: CLOCK, useValue: () => now },
      ],
    });
  });

  it('sends a signed-out visitor to sign in, remembering where they were going', () => {
    const result = TestBed.runInInjectionContext(() => authGuard(route, state));
    expect(serialize(result)).toBe('/sign-in?returnUrl=%2Fsales%3FcustomerName%3Dsilva');
  });

  it('lets a signed-in user through', () => {
    signIn();
    expect(TestBed.runInInjectionContext(() => authGuard(route, state))).toBe(true);
  });

  it('ends an expired session and sends the user to sign in', () => {
    signIn();
    now = 3_000_000;

    const result = TestBed.runInInjectionContext(() => authGuard(route, state));

    expect(serialize(result)).toContain('/sign-in');
    expect(TestBed.inject(SessionStore).endReason()).toBe('expired');
  });

  it('sends a signed-in user away from the sign-in page', () => {
    signIn();
    expect(serialize(TestBed.runInInjectionContext(() => guestGuard(route, state)))).toBe('/sales');
  });

  it('only returns to paths inside the portal', () => {
    expect(safeReturnUrl('/sales/42')).toBe('/sales/42');
    expect(safeReturnUrl('//evil.example')).toBe('/sales');
    expect(safeReturnUrl('https://evil.example')).toBe('/sales');
    expect(safeReturnUrl('/sign-in')).toBe('/sales');
    expect(safeReturnUrl(undefined)).toBe('/sales');
  });
});

import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { fakeJwt } from '../../testing/fake-jwt';
import { MemoryStorage } from '../../testing/memory-storage';
import { SESSION_STORAGE_KEY } from './session';
import { CLOCK, SESSION_STORAGE, SessionStore } from './session-store';

describe('SessionStore', () => {
  let storage: MemoryStorage;
  let now: number;
  const auth = {
    token: fakeJwt({ nameid: '6f1c1c52-8a7e-4a4f-9c1d-2d8f0b9d3e11', exp: 2_000 }),
    email: 'ana@developerstore.test', name: 'Ana Souza', role: 'Customer',
  };

  beforeEach(() => {
    storage = new MemoryStorage();
    now = 1_000_000;
    TestBed.configureTestingModule({
      providers: [
        { provide: SESSION_STORAGE, useValue: storage },
        { provide: CLOCK, useValue: () => now },
      ],
    });
  });

  it('starts signed out', () => {
    expect(TestBed.inject(SessionStore).isSignedIn()).toBe(false);
  });

  it('starts a session and keeps it in storage', () => {
    const store = TestBed.inject(SessionStore);
    store.start(auth);

    expect(store.isSignedIn()).toBe(true);
    expect(store.session()?.name).toBe('Ana Souza');
    expect(storage.getItem(SESSION_STORAGE_KEY)).toContain('Ana Souza');
  });

  it('restores a stored session', () => {
    TestBed.inject(SessionStore).start(auth);
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({ providers: [{ provide: SESSION_STORAGE, useValue: storage }] });

    expect(TestBed.inject(SessionStore).session()?.email).toBe('ana@developerstore.test');
  });

  it('ends a session and remembers why', () => {
    const store = TestBed.inject(SessionStore);
    store.start(auth);
    store.end('expired');

    expect(store.isSignedIn()).toBe(false);
    expect(store.endReason()).toBe('expired');
    expect(storage.getItem(SESSION_STORAGE_KEY)).toBeNull();
  });

  it('knows when the token has expired', () => {
    const store = TestBed.inject(SessionStore);
    store.start(auth);
    expect(store.isExpired()).toBe(false);

    now = 2_000_000;
    expect(store.isExpired()).toBe(true);
  });
});

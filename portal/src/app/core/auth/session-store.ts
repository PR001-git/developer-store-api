import { computed, inject, Injectable, InjectionToken, signal } from '@angular/core';
import { AuthenticateResponse } from '../api/api-models';
import { parseStoredSession, Session, SESSION_STORAGE_KEY, sessionFromAuthResponse } from './session';

export type SessionEndReason = 'signed-out' | 'expired' | 'rejected' | 'account-deleted';

export const SESSION_STORAGE = new InjectionToken<Storage>('SESSION_STORAGE', {
  providedIn: 'root',
  factory: () => sessionStorage,
});

export const CLOCK = new InjectionToken<() => number>('CLOCK', {
  providedIn: 'root',
  factory: () => () => Date.now(),
});

@Injectable({ providedIn: 'root' })
export class SessionStore {
  private readonly storage = inject(SESSION_STORAGE);
  private readonly now = inject(CLOCK);
  private readonly state = signal<Session | null>(parseStoredSession(this.storage.getItem(SESSION_STORAGE_KEY)));
  private readonly endReasonState = signal<SessionEndReason | null>(null);

  readonly session = this.state.asReadonly();
  readonly endReason = this.endReasonState.asReadonly();
  readonly isSignedIn = computed(() => this.state() !== null);

  start(response: AuthenticateResponse): Session {
    const session = sessionFromAuthResponse(response);
    this.storage.setItem(SESSION_STORAGE_KEY, JSON.stringify(session));
    this.state.set(session);
    this.endReasonState.set(null);
    return session;
  }

  end(reason: SessionEndReason): void {
    this.storage.removeItem(SESSION_STORAGE_KEY);
    this.state.set(null);
    this.endReasonState.set(reason);
  }

  isExpired(): boolean {
    const session = this.state();
    return session !== null && session.expiresAt <= this.now();
  }
}

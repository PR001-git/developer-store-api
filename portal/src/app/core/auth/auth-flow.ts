import { inject, Injectable } from '@angular/core';
import { map, Observable, switchMap, tap, throwError } from 'rxjs';
import { AuthApi } from '../api/auth-api';
import { AuthenticateRequest, CreateUserRequest } from '../api/api-models';
import { UsersApi } from '../api/users-api';
import { Session } from './session';
import { SessionStore } from './session-store';

export type SignUpDetails = Pick<CreateUserRequest, 'username' | 'email' | 'phone' | 'password'>;

@Injectable({ providedIn: 'root' })
export class AuthFlow {
  private readonly authApi = inject(AuthApi);
  private readonly usersApi = inject(UsersApi);
  private readonly store = inject(SessionStore);

  signIn(credentials: AuthenticateRequest): Observable<Session> {
    return this.authApi.signIn(credentials).pipe(map(response => this.store.start(response)));
  }

  /** Public sign-up creates only active Customer accounts; the user is signed in straight after. */
  signUp(details: SignUpDetails): Observable<Session> {
    return this.usersApi.signUp({ ...details, status: 'Active', role: 'Customer' }).pipe(
      switchMap(() => this.signIn({ email: details.email, password: details.password })),
    );
  }

  signOut(): void {
    this.store.end('signed-out');
  }

  deleteAccount(): Observable<void> {
    const session = this.store.session();
    if (!session) {
      return throwError(() => new Error('Not signed in.'));
    }
    return this.usersApi.delete(session.userId).pipe(tap(() => this.store.end('account-deleted')));
  }
}

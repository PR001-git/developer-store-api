import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { SessionStore } from './session-store';

const HOME = '/sales';

export const authGuard: CanActivateFn = (_route, state) => {
  const store = inject(SessionStore);
  if (store.isSignedIn() && !store.isExpired()) {
    return true;
  }
  if (store.isSignedIn()) {
    store.end('expired');
  }
  return inject(Router).createUrlTree(['/sign-in'], { queryParams: { returnUrl: state.url } });
};

export const guestGuard: CanActivateFn = () =>
  inject(SessionStore).isSignedIn() ? inject(Router).parseUrl(HOME) : true;

/** Only paths inside the portal: never another origin, never the auth pages. */
export function safeReturnUrl(value: unknown): string {
  if (typeof value !== 'string' || !value.startsWith('/') || value.startsWith('//')
    || value.startsWith('/sign-in') || value.startsWith('/sign-up')) {
    return HOME;
  }
  return value;
}

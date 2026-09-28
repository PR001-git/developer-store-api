import { expect, type Page, test as base } from '@playwright/test';
import type { AuthenticateResponse } from '../../src/app/core/api/api-models';
import { SESSION_STORAGE_KEY, sessionFromAuthResponse } from '../../src/app/core/auth/session';
import { ApiClient, newTestUser, type TestUser } from './api';

/** Starts the page signed in by writing the session the portal would have written. */
export async function useSession(page: Page, auth: AuthenticateResponse): Promise<void> {
  const session = JSON.stringify(sessionFromAuthResponse(auth));
  await page.addInitScript(([key, value]) => window.sessionStorage.setItem(key, value), [SESSION_STORAGE_KEY, session] as const);
}

export const test = base.extend<{ api: ApiClient; user: TestUser; auth: AuthenticateResponse; signedInPage: Page }>({
  api: async ({}, use) => {
    const api = await ApiClient.create();
    await use(api);
    await api.dispose();
  },
  user: async ({ api }, use) => {
    const user = newTestUser();
    expect(await api.signUp(user)).toBe(201);
    await use(user);
  },
  auth: async ({ api, user }, use) => {
    await use(await api.signIn(user));
  },
  signedInPage: async ({ page, auth }, use) => {
    await useSession(page, auth);
    await use(page);
  },
});

export { expect };

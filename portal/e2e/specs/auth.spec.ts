import { newTestUser } from '../support/api';
import { expect, test, useSession } from '../support/fixtures';
import { fakeJwt } from '../../src/app/testing/fake-jwt';

test.describe('Sign up and sign in', () => {
  test('a new customer signs up and lands on the sales list', async ({ page }) => {
    const user = newTestUser();
    await page.goto('/sign-up');

    await page.getByLabel('Username').fill(user.username);
    await page.getByLabel('Email').fill(user.email);
    await page.getByLabel('Phone').fill(user.phone);
    await page.getByLabel('Password', { exact: true }).fill(user.password);
    await page.getByRole('button', { name: 'Create account' }).click();

    await expect(page).toHaveURL(/\/sales$/);
    await expect(page.getByRole('heading', { level: 1, name: 'Sales', exact: true })).toBeVisible();
  });

  test('the password rules stop a weak password before the API is called', async ({ page }) => {
    let signUps = 0;
    page.on('request', request => { if (request.url().endsWith('/api/users')) signUps++; });
    const user = newTestUser();
    await page.goto('/sign-up');

    await page.getByLabel('Username').fill(user.username);
    await page.getByLabel('Email').fill(user.email);
    await page.getByLabel('Phone').fill(user.phone);
    await page.getByLabel('Password', { exact: true }).fill('weak');
    await page.getByRole('button', { name: 'Create account' }).click();

    const rules = page.getByRole('list', { name: 'Password rules' });
    await expect(rules.getByRole('listitem').filter({ hasText: 'At least 8 characters' })).toContainText(/not met/i);
    await expect(page).toHaveURL(/\/sign-up$/);
    expect(signUps).toBe(0);
  });

  test('signing up twice with one email shows the API conflict', async ({ page, api }) => {
    const user = newTestUser();
    expect(await api.signUp(user)).toBe(201);
    await page.goto('/sign-up');

    await page.getByLabel('Username').fill(user.username);
    await page.getByLabel('Email').fill(user.email);
    await page.getByLabel('Phone').fill(user.phone);
    await page.getByLabel('Password', { exact: true }).fill(user.password);
    await page.getByRole('button', { name: 'Create account' }).click();

    await expect(page.getByRole('alert')).toContainText(`User with email ${user.email} already exists`);
  });

  test('a wrong password shows "Invalid credentials"', async ({ page, user }) => {
    await page.goto('/sign-in');
    await page.getByLabel('Email').fill(user.email);
    await page.getByLabel('Password', { exact: true }).fill('Wrong@2026');
    await page.getByRole('button', { name: 'Sign in' }).click();

    await expect(page.getByRole('alert')).toContainText('Invalid credentials');
  });

  test('signing in returns to the page the user asked for', async ({ page, user }) => {
    await page.goto('/sales/new');
    await expect(page).toHaveURL(/\/sign-in\?returnUrl=%2Fsales%2Fnew/);

    await page.getByLabel('Email').fill(user.email);
    await page.getByLabel('Password', { exact: true }).fill(user.password);
    await page.getByRole('button', { name: 'Sign in' }).click();

    await expect(page.getByRole('heading', { level: 1, name: 'New sale' })).toBeVisible();
  });

  test('signing out ends the session', async ({ signedInPage: page }) => {
    await page.goto('/sales');
    await page.getByRole('button', { name: 'Sign out' }).click();

    await expect(page).toHaveURL(/\/sign-in/);
    await page.goto('/sales');
    await expect(page).toHaveURL(/\/sign-in\?returnUrl=%2Fsales/);
  });

  test('a token the API rejects sends the user to sign in with a notice', async ({ page }) => {
    const forged = { token: fakeJwt({ nameid: crypto.randomUUID(), exp: 9_999_999_999 }), email: 'x@y.test', name: 'X', role: 'Customer' };
    await useSession(page, forged);

    await page.goto('/sales');

    await expect(page).toHaveURL(/\/sign-in/);
    await expect(page.getByText('Your session expired. Sign in again.')).toBeVisible();
  });
});

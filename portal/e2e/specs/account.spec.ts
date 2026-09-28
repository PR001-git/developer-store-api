import { expect, test } from '../support/fixtures';

test.describe('Account', () => {
  test('shows the signed-in user', async ({ signedInPage: page, user }) => {
    await page.goto('/account');

    await expect(page.getByRole('heading', { level: 1, name: 'Account' })).toBeVisible();
    await expect(page.getByText(user.email)).toBeVisible();
    await expect(page.getByText(user.phone)).toBeVisible();
    await expect(page.getByText('Customer', { exact: true })).toBeVisible();
    await expect(page.getByText('Active', { exact: true })).toBeVisible();
  });

  test('deleting the account signs the user out for good', async ({ signedInPage: page, user }) => {
    await page.goto('/account');
    await page.getByRole('button', { name: 'Delete account' }).click();
    await page.getByRole('alertdialog', { name: 'Delete your account?' }).getByRole('button', { name: 'Delete account' }).click();

    await expect(page).toHaveURL(/\/sign-in/);
    await expect(page.getByText('Your account was deleted.')).toBeVisible();

    await page.getByLabel('Email').fill(user.email);
    await page.getByLabel('Password', { exact: true }).fill(user.password);
    await page.getByRole('button', { name: 'Sign in' }).click();
    await expect(page.getByRole('alert')).toContainText('Invalid credentials');
  });
});

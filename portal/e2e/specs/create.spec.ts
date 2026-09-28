import { CUSTOMERS, BRANCHES, PRODUCTS } from '../../src/app/core/catalog/demo-catalog';
import { formatMoney } from '../../src/app/shared/format';
import { expect, test } from '../support/fixtures';
import { saleFor, uniqueCustomer } from '../support/sales';

test.describe('Create a sale', () => {
  test.beforeEach(async ({ signedInPage: page }) => {
    await page.goto('/sales/new');
    await page.getByLabel('Customer', { exact: true }).selectOption({ label: CUSTOMERS[0].name });
    await page.getByLabel('Branch', { exact: true }).selectOption({ label: BRANCHES[0].name });
    const first = page.getByRole('group', { name: 'Line 1' });
    await first.getByLabel('Product', { exact: true }).selectOption({ label: PRODUCTS[0].name });
  });

  test('previews 10% off for 5 items and lets the API number the sale', async ({ signedInPage: page }) => {
    const first = page.getByRole('group', { name: 'Line 1' });
    await expect(first.getByLabel('Unit price')).toHaveValue('4.5');
    await first.getByLabel('Quantity').fill('5');

    await expect(first.getByText('10% off')).toBeVisible();
    await expect(page.getByRole('region', { name: 'Summary' })).toContainText(formatMoney(20.25));

    await page.getByRole('button', { name: 'Create sale' }).click();

    await expect(page).toHaveURL(/\/sales\/[0-9a-f-]{36}$/);
    await expect(page.getByRole('heading', { level: 1 })).toHaveText(/^Sale S-\d{6}$/);
    await expect(page.getByRole('status')).toContainText(/Sale S-\d{6} created/);
    await expect(page.getByRole('region', { name: 'Totals' })).toContainText(formatMoney(20.25));
  });

  test('hints at the next discount tier', async ({ signedInPage: page }) => {
    const first = page.getByRole('group', { name: 'Line 1' });

    await first.getByLabel('Quantity').fill('3');
    await expect(first.getByText('No discount')).toBeVisible();
    await expect(first.getByText('Add 1 more for 10% off')).toBeVisible();

    await first.getByLabel('Quantity').fill('9');
    await expect(first.getByText('Add 1 more for 20% off')).toBeVisible();

    await first.getByLabel('Quantity').fill('10');
    await expect(first.getByText('20% off')).toBeVisible();
    await expect(first.getByText(/Add \d+ more/)).toHaveCount(0);
  });

  test('refuses more than 20 identical items without calling the API', async ({ signedInPage: page }) => {
    let creates = 0;
    page.on('request', request => { if (request.method() === 'POST' && request.url().endsWith('/api/sales')) creates++; });
    const first = page.getByRole('group', { name: 'Line 1' });

    await first.getByLabel('Quantity').fill('21');
    await page.getByRole('button', { name: 'Create sale' }).click();

    await expect(first.getByText("It's not possible to sell above 20 identical items.")).toBeVisible();
    expect(creates).toBe(0);
  });

  test('refuses the same product on two lines', async ({ signedInPage: page }) => {
    await page.getByRole('button', { name: 'Add line' }).click();
    await page.getByRole('group', { name: 'Line 2' }).getByLabel('Product', { exact: true }).selectOption({ label: PRODUCTS[0].name });
    await page.getByRole('button', { name: 'Create sale' }).click();

    await expect(page.getByText('Each product can appear only once in a sale.')).toBeVisible();
  });

  test('a sale number already taken is a conflict', async ({ signedInPage: page, api }) => {
    const taken = `E2E-${Date.now()}`;
    await api.createSale(saleFor(uniqueCustomer(), { saleNumber: taken }));

    await page.getByLabel('Sale number').fill(taken);
    await page.getByRole('button', { name: 'Create sale' }).click();

    await expect(page.getByRole('alert')).toContainText(`Sale number ${taken} already exists`);
  });

  test('accepts a customer outside the catalog', async ({ signedInPage: page }) => {
    await page.getByLabel('Customer', { exact: true }).selectOption({ label: 'Other customer…' });
    await page.getByLabel('Customer name').fill('Padaria Pão Quente');
    await expect(page.getByLabel('Customer ID')).toHaveValue(/^[0-9a-f-]{36}$/);

    await page.getByRole('button', { name: 'Create sale' }).click();

    await expect(page.getByRole('heading', { level: 1 })).toHaveText(/^Sale S-\d{6}$/);
    await expect(page.getByText('Padaria Pão Quente')).toBeVisible();
  });
});

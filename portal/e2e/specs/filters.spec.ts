import type { Page } from '@playwright/test';
import { PRODUCTS } from '../../src/app/core/catalog/demo-catalog';
import { expect, test } from '../support/fixtures';
import { line, saleFor, uniqueCustomer } from '../support/sales';

const rows = (page: Page) =>
  page.getByRole('table', { name: 'Sales' }).getByRole('row').filter({ has: page.getByRole('cell') });
const filters = (page: Page) => page.getByRole('search', { name: 'Filters' });

test.describe('Sales list: filters', () => {
  test('a customer filter matches anywhere in the name, ignoring case', async ({ signedInPage: page, api }) => {
    const token = Math.random().toString(36).slice(2, 8);
    const customer = uniqueCustomer();
    await api.createSale(saleFor(customer, { customerName: `Cliente ${token} Silva` }));
    await api.createSale(saleFor(customer, { customerName: `Cliente ${token} Souza` }));
    await page.goto('/sales');

    await filters(page).getByLabel('Customer', { exact: true }).fill(`${token} SILVA`);
    await filters(page).getByRole('button', { name: 'Apply filters' }).click();

    await expect(rows(page)).toHaveCount(1);
    await expect(rows(page).first()).toContainText(`Cliente ${token} Silva`);
  });

  test('a typed * is sent as a wildcard', async ({ signedInPage: page, api }) => {
    const token = Math.random().toString(36).slice(2, 8);
    const customer = uniqueCustomer();
    await api.createSale(saleFor(customer, { customerName: `${token} Silva` }));
    await api.createSale(saleFor(customer, { customerName: `Maria ${token}` }));
    await page.goto('/sales');

    await filters(page).getByLabel('Customer', { exact: true }).fill(`${token}*`);
    await filters(page).getByRole('button', { name: 'Apply filters' }).click();

    await expect(rows(page)).toHaveCount(1);
    await expect(rows(page).first()).toContainText(`${token} Silva`);
  });

  test('status shows only cancelled sales', async ({ signedInPage: page, api }) => {
    const customer = uniqueCustomer();
    await api.createSale(saleFor(customer));
    const cancelled = await api.createSale(saleFor(customer));
    await api.cancelSale(cancelled.id);
    await page.goto(`/sales?customerId=${customer.id}`);

    await filters(page).getByRole('radio', { name: 'Cancelled' }).check();
    await filters(page).getByRole('button', { name: 'Apply filters' }).click();

    await expect(page).toHaveURL(/isCancelled=true/);
    await expect(rows(page)).toHaveCount(1);
    await expect(rows(page).first()).toContainText(cancelled.saleNumber);
    await expect(rows(page).first()).toContainText('Cancelled');
  });

  test('a date range covers whole local days', async ({ signedInPage: page, api }) => {
    const customer = uniqueCustomer();
    // 23:30 in São Paulo on July 10 is already July 11 in UTC; the portal filters by local day.
    const lateOnTheTenth = await api.createSale(saleFor(customer, { saleDate: '2026-07-10T23:30:00-03:00' }));
    await api.createSale(saleFor(customer, { saleDate: '2026-07-11T09:00:00-03:00' }));
    await page.goto(`/sales?customerId=${customer.id}`);

    await filters(page).getByLabel('Sold from').fill('2026-07-10');
    await filters(page).getByLabel('Sold to').fill('2026-07-10');
    await filters(page).getByRole('button', { name: 'Apply filters' }).click();

    await expect(rows(page)).toHaveCount(1);
    await expect(rows(page).first()).toContainText(lateOnTheTenth.saleNumber);
  });

  test('a total range is inclusive', async ({ signedInPage: page, api }) => {
    const customer = uniqueCustomer();
    for (const price of [10, 20, 30]) {
      await api.createSale(saleFor(customer, { items: [line(PRODUCTS[0], 1, price)] }));
    }
    await page.goto(`/sales?customerId=${customer.id}`);

    await filters(page).getByLabel('Minimum total').fill('20');
    await filters(page).getByLabel('Maximum total').fill('30');
    await filters(page).getByRole('button', { name: 'Apply filters' }).click();

    await expect(rows(page)).toHaveCount(2);
  });

  test('a minimum above the maximum is caught before the API is called', async ({ signedInPage: page }) => {
    await page.goto('/sales');
    let listCalls = 0;
    page.on('request', request => { if (request.url().includes('/api/sales?')) listCalls++; });

    await filters(page).getByLabel('Minimum total').fill('50');
    await filters(page).getByLabel('Maximum total').fill('10');
    await filters(page).getByRole('button', { name: 'Apply filters' }).click();

    await expect(page.getByText('Minimum total must not be above maximum total.')).toBeVisible();
    expect(listCalls).toBe(0);
  });

  test('the branch ID filter lives under More filters', async ({ signedInPage: page, api }) => {
    const customer = uniqueCustomer();
    const branchId = crypto.randomUUID();
    await api.createSale(saleFor(customer, { branchId, branchName: 'Filial Teste' }));
    await api.createSale(saleFor(customer));
    await page.goto(`/sales?customerId=${customer.id}`);

    await filters(page).getByRole('button', { name: 'More filters' }).click();
    await filters(page).getByLabel('Branch ID').fill(branchId);
    await filters(page).getByRole('button', { name: 'Apply filters' }).click();

    await expect(rows(page)).toHaveCount(1);
    await expect(rows(page).first()).toContainText('Filial Teste');
  });

  test('filters survive a reload and Clear filters resets them', async ({ signedInPage: page, api }) => {
    const customer = uniqueCustomer();
    await api.createSale(saleFor(customer));
    await page.goto(`/sales?customerId=${customer.id}&isCancelled=false`);
    await page.reload();

    await expect(filters(page).getByRole('radio', { name: 'Open' })).toBeChecked();
    await expect(rows(page)).toHaveCount(1);

    await filters(page).getByRole('button', { name: 'Clear filters' }).click();
    await expect(page).toHaveURL(/\/sales$/);
  });
});

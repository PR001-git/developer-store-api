import type { Page } from '@playwright/test';
import { formatMoney } from '../../src/app/shared/format';
import { PRODUCTS } from '../../src/app/core/catalog/demo-catalog';
import { expect, test } from '../support/fixtures';
import { line, saleFor, uniqueCustomer } from '../support/sales';

const rows = (page: Page) =>
  page.getByRole('table', { name: 'Sales' }).getByRole('row').filter({ has: page.getByRole('cell') });

test.describe('Sales list: paging and ordering', () => {
  test('pages through 12 sales, newest first', async ({ signedInPage: page, api }) => {
    const customer = uniqueCustomer();
    for (let day = 1; day <= 12; day++) {
      await api.createSale(saleFor(customer, { saleDate: `2026-08-${String(day).padStart(2, '0')}T10:00:00-03:00` }));
    }

    await page.goto(`/sales?customerId=${customer.id}`);

    await expect(rows(page)).toHaveCount(10);
    await expect(rows(page).first()).toContainText('Aug 12, 2026');
    await expect(page.getByText('Page 1 of 2')).toBeVisible();
    await expect(page.getByText('12 sales')).toBeVisible();

    await page.getByRole('button', { name: 'Next page' }).click();

    await expect(page).toHaveURL(/_page=2/);
    await expect(rows(page)).toHaveCount(2);
    await expect(rows(page).last()).toContainText('Aug 1, 2026');
  });

  test('sorts by total, highest first, then flips', async ({ signedInPage: page, api }) => {
    const customer = uniqueCustomer();
    for (const price of [10, 30, 20]) {
      await api.createSale(saleFor(customer, { items: [line(PRODUCTS[0], 1, price)] }));
    }
    await page.goto(`/sales?customerId=${customer.id}`);

    const total = page.getByRole('columnheader', { name: 'Total' });
    await total.getByRole('button', { name: 'Total' }).click();

    await expect(total).toHaveAttribute('aria-sort', 'descending');
    await expect(rows(page).first()).toContainText(formatMoney(30));

    await total.getByRole('button', { name: 'Total' }).click();

    await expect(total).toHaveAttribute('aria-sort', 'ascending');
    await expect(rows(page).first()).toContainText(formatMoney(10));
    await expect(page).toHaveURL(/_order=totalAmount(%20|\+)asc/);
  });

  test('Shift+click adds a second sort', async ({ signedInPage: page, api }) => {
    const customer = uniqueCustomer();
    await api.createSale(saleFor(customer, { customerName: 'Ana', items: [line(PRODUCTS[0], 1, 10)] }));
    await api.createSale(saleFor(customer, { customerName: 'Ana', items: [line(PRODUCTS[0], 1, 30)] }));
    await api.createSale(saleFor(customer, { customerName: 'Bruno', items: [line(PRODUCTS[0], 1, 20)] }));
    await page.goto(`/sales?customerId=${customer.id}`);

    await page.getByRole('columnheader', { name: 'Customer' }).getByRole('button', { name: 'Customer' }).click();
    await page.getByRole('columnheader', { name: 'Total' }).getByRole('button', { name: 'Total' }).click({ modifiers: ['Shift'] });

    await expect(page).toHaveURL(/_order=customerName(%20|\+)asc(%2C|,)(%20|\+)totalAmount(%20|\+)desc/);
    await expect(rows(page).nth(0)).toContainText(formatMoney(30));
    await expect(rows(page).nth(1)).toContainText(formatMoney(10));
    await expect(rows(page).nth(2)).toContainText('Bruno');
  });

  test('changing rows per page goes back to page 1', async ({ signedInPage: page, api }) => {
    const customer = uniqueCustomer();
    for (let i = 0; i < 11; i++) await api.createSale(saleFor(customer));
    await page.goto(`/sales?customerId=${customer.id}&_page=2`);

    await page.getByLabel('Rows per page').selectOption('20');

    await expect(page).not.toHaveURL(/_page=/);
    await expect(rows(page)).toHaveCount(11);
  });
});

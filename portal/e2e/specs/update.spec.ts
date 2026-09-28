import { PRODUCTS } from '../../src/app/core/catalog/demo-catalog';
import { formatMoney } from '../../src/app/shared/format';
import { expect, test } from '../support/fixtures';
import { line, saleFor, uniqueCustomer } from '../support/sales';

test.describe('Update a sale', () => {
  test('re-prices a line and adds a product', async ({ signedInPage: page, api }) => {
    const sale = await api.createSale(saleFor(uniqueCustomer()));
    await page.goto(`/sales/${sale.id}`);
    await page.getByRole('link', { name: 'Edit sale' }).click();

    await expect(page.getByRole('heading', { level: 1 })).toHaveText(`Edit sale ${sale.saleNumber}`);
    await expect(page.getByLabel('Sale number')).not.toBeEditable();

    await page.getByRole('group', { name: 'Line 1' }).getByLabel('Quantity').fill('10');
    await page.getByRole('button', { name: 'Add line' }).click();
    const second = page.getByRole('group', { name: 'Line 2' });
    await second.getByLabel('Product', { exact: true }).selectOption({ label: PRODUCTS[1].name });
    await second.getByLabel('Quantity').fill('2');
    await page.getByRole('button', { name: 'Save changes' }).click();

    // 10 × 4.50 = 45.00, 20% off → 36.00; 2 × 7.90 = 15.80 → 51.80.
    await expect(page.getByRole('status')).toContainText('Changes saved');
    await expect(page.getByRole('region', { name: 'Totals' })).toContainText(formatMoney(51.8));
  });

  test('removing a line cancels it, and it stays as history', async ({ signedInPage: page, api }) => {
    const sale = await api.createSale(saleFor(uniqueCustomer(), { items: [line(PRODUCTS[0], 5), line(PRODUCTS[1], 2)] }));
    await page.goto(`/sales/${sale.id}/edit`);

    await page.getByRole('button', { name: 'Remove line 2' }).click();
    await page.getByRole('button', { name: 'Save changes' }).click();

    const row = page.getByRole('table', { name: 'Items' }).getByRole('row').filter({ hasText: PRODUCTS[1].name });
    await expect(row).toContainText('Cancelled');

    await page.getByRole('link', { name: 'Edit sale' }).click();
    await expect(page.getByRole('region', { name: 'Cancelled lines' })).toContainText(PRODUCTS[1].name);
  });

  test('a cancelled sale opens read-only', async ({ signedInPage: page, api }) => {
    const sale = await api.createSale(saleFor(uniqueCustomer()));
    await api.cancelSale(sale.id);

    await page.goto(`/sales/${sale.id}/edit`);

    await expect(page).toHaveURL(new RegExp(`/sales/${sale.id}$`));
    await expect(page.getByText('Cancelled sales are read-only. You can still delete it.')).toBeVisible();
  });
});

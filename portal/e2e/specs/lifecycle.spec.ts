import { PRODUCTS } from '../../src/app/core/catalog/demo-catalog';
import { formatMoney } from '../../src/app/shared/format';
import { expect, test } from '../support/fixtures';
import { line, saleFor, uniqueCustomer } from '../support/sales';

test.describe('Read, cancel and delete a sale', () => {
  test('cancelling one item drops it from the total and keeps the sale open', async ({ signedInPage: page, api }) => {
    const sale = await api.createSale(saleFor(uniqueCustomer(), { items: [line(PRODUCTS[0], 5), line(PRODUCTS[1], 2)] }));
    await page.goto(`/sales/${sale.id}`);

    await page.getByRole('button', { name: `Cancel item ${PRODUCTS[1].name}` }).click();
    const dialog = page.getByRole('alertdialog', { name: 'Cancel this item?' });
    await dialog.getByRole('button', { name: 'Cancel item' }).click();

    await expect(page.getByRole('status')).toContainText('Item cancelled');
    const row = page.getByRole('table', { name: 'Items' }).getByRole('row').filter({ hasText: PRODUCTS[1].name });
    await expect(row).toContainText('Cancelled');
    await expect(page.getByRole('region', { name: 'Totals' })).toContainText(formatMoney(20.25));
    await expect(page.getByText('Open', { exact: true })).toBeVisible();
  });

  test('cancelling the last active item cancels the sale', async ({ signedInPage: page, api }) => {
    const sale = await api.createSale(saleFor(uniqueCustomer()));
    await page.goto(`/sales/${sale.id}`);

    await page.getByRole('button', { name: `Cancel item ${PRODUCTS[0].name}` }).click();
    const dialog = page.getByRole('alertdialog', { name: 'Cancel this item?' });
    await expect(dialog).toContainText('This is the last active item, so the sale will be cancelled too.');
    await dialog.getByRole('button', { name: 'Cancel item' }).click();

    await expect(page.getByText('Cancelled', { exact: true }).first()).toBeVisible();
    await expect(page.getByRole('region', { name: 'Totals' })).toContainText(formatMoney(0));
    await expect(page.getByText('Cancelled sales are read-only. You can still delete it.')).toBeVisible();
    await expect(page.getByRole('link', { name: 'Edit sale' })).toHaveCount(0);
  });

  test('cancelling a sale keeps its items and total', async ({ signedInPage: page, api }) => {
    const sale = await api.createSale(saleFor(uniqueCustomer()));
    await page.goto(`/sales/${sale.id}`);

    await page.getByRole('button', { name: 'Cancel sale' }).click();
    const dialog = page.getByRole('alertdialog', { name: 'Cancel this sale?' });
    await dialog.getByRole('button', { name: 'Cancel sale' }).click();

    await expect(page.getByRole('status')).toContainText(`Sale ${sale.saleNumber} cancelled`);
    await expect(page.getByRole('region', { name: 'Totals' })).toContainText(formatMoney(20.25));
    await expect(page.getByRole('button', { name: /^Cancel item/ })).toHaveCount(0);
  });

  test('keeping the sale closes the dialog and changes nothing', async ({ signedInPage: page, api }) => {
    const sale = await api.createSale(saleFor(uniqueCustomer()));
    await page.goto(`/sales/${sale.id}`);

    await page.getByRole('button', { name: 'Delete sale' }).click();
    await page.getByRole('alertdialog', { name: 'Delete this sale?' }).getByRole('button', { name: 'Keep sale' }).click();

    await expect(page.getByRole('alertdialog')).toHaveCount(0);
    await expect(page.getByRole('heading', { level: 1 })).toHaveText(`Sale ${sale.saleNumber}`);
    await expect(page.getByRole('button', { name: 'Delete sale' })).toBeFocused();
  });

  test('deleting a sale hides it everywhere', async ({ signedInPage: page, api }) => {
    const customer = uniqueCustomer();
    const sale = await api.createSale(saleFor(customer));
    await page.goto(`/sales/${sale.id}`);

    await page.getByRole('button', { name: 'Delete sale' }).click();
    await page.getByRole('alertdialog', { name: 'Delete this sale?' }).getByRole('button', { name: 'Delete sale' }).click();

    await expect(page).toHaveURL(/\/sales$/);
    await expect(page.getByRole('status')).toContainText(`Sale ${sale.saleNumber} deleted`);

    await page.goto(`/sales?customerId=${customer.id}`);
    await expect(page.getByText('No sales match these filters.')).toBeVisible();

    await page.goto(`/sales/${sale.id}`);
    await expect(page.getByRole('heading', { level: 1, name: 'Sale not found' })).toBeVisible();
  });

  test('a cancelled sale can still be deleted', async ({ signedInPage: page, api }) => {
    const sale = await api.createSale(saleFor(uniqueCustomer()));
    await api.cancelSale(sale.id);
    await page.goto(`/sales/${sale.id}`);

    await page.getByRole('button', { name: 'Delete sale' }).click();
    await page.getByRole('alertdialog', { name: 'Delete this sale?' }).getByRole('button', { name: 'Delete sale' }).click();

    await expect(page.getByRole('status')).toContainText(`Sale ${sale.saleNumber} deleted`);
  });
});

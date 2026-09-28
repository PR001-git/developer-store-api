import { expect, test } from '../support/fixtures';
import { saleFor, uniqueCustomer } from '../support/sales';

test.describe('Error states the API cannot produce on demand', () => {
  // A predicate, not a glob: a ? in a URL glob isn't a reliable literal across Playwright versions.
  const isSalesList = (url: URL) => url.pathname === '/api/sales';

  test('the API being down shows a retry', async ({ signedInPage: page }) => {
    await page.route(isSalesList, route => route.abort('connectionrefused'));
    await page.goto('/sales');

    await expect(page.getByRole('alert')).toContainText("Can't reach the API");
    await page.unroute(isSalesList);
    await page.getByRole('button', { name: 'Try again' }).click();

    await expect(page.getByRole('table', { name: 'Sales' }).or(page.getByText('No sales yet.'))).toBeVisible();
  });

  test('a server error shows the API error title and detail', async ({ signedInPage: page }) => {
    await page.route(isSalesList, route => route.fulfill({
      status: 500,
      json: { type: 'InternalServerError', error: 'Internal server error', detail: 'An unexpected error occurred.' },
    }));
    await page.goto('/sales');

    await expect(page.getByRole('alert')).toContainText('Internal server error');
    await expect(page.getByRole('alert')).toContainText('An unexpected error occurred.');
  });

  test('a concurrency conflict offers to reload the sale', async ({ signedInPage: page, api }) => {
    const sale = await api.createSale(saleFor(uniqueCustomer()));
    await page.route(`**/api/sales/${sale.id}/cancel`, route => route.fulfill({
      status: 409,
      json: { type: 'ConcurrencyConflict', error: 'Concurrent modification', detail: 'The sale was changed by someone else.' },
    }));
    await page.goto(`/sales/${sale.id}`);

    await page.getByRole('button', { name: 'Cancel sale' }).click();
    await page.getByRole('alertdialog', { name: 'Cancel this sale?' }).getByRole('button', { name: 'Cancel sale' }).click();

    await expect(page.getByRole('alert')).toContainText('The sale was changed by someone else.');
    await expect(page.getByRole('button', { name: 'Reload sale' })).toBeVisible();
  });

  test('an unknown address shows Page not found', async ({ signedInPage: page }) => {
    await page.goto('/nowhere');

    await expect(page.getByRole('heading', { level: 1, name: 'Page not found' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Back to sales' })).toBeVisible();
  });
});

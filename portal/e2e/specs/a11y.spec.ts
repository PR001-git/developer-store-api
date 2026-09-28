import AxeBuilder from '@axe-core/playwright';
import type { Page } from '@playwright/test';
import { expect, test } from '../support/fixtures';
import { saleFor, uniqueCustomer } from '../support/sales';

const WCAG = ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'];
const expectNoViolations = async (page: Page) => {
  const results = await new AxeBuilder({ page }).withTags(WCAG).analyze();
  expect(results.violations, JSON.stringify(results.violations, null, 2)).toEqual([]);
};

test.describe('Accessibility (axe, WCAG 2.2 AA)', () => {
  test('sign in and sign up', async ({ page }) => {
    await page.goto('/sign-in');
    await expectNoViolations(page);
    await page.goto('/sign-up');
    await expectNoViolations(page);
  });

  test('the signed-in screens', async ({ signedInPage: page, api }) => {
    const sale = await api.createSale(saleFor(uniqueCustomer()));
    for (const path of ['/sales', '/sales/new', `/sales/${sale.id}`, `/sales/${sale.id}/edit`, '/account']) {
      await page.goto(path);
      await expect(page.getByRole('heading', { level: 1 })).toBeVisible();
      await expectNoViolations(page);
    }
  });

  test('a dialog traps focus and Escape closes it', async ({ signedInPage: page, api }) => {
    const sale = await api.createSale(saleFor(uniqueCustomer()));
    await page.goto(`/sales/${sale.id}`);

    await page.getByRole('button', { name: 'Delete sale' }).click();
    const dialog = page.getByRole('alertdialog', { name: 'Delete this sale?' });
    await expect(dialog.getByRole('button', { name: 'Keep sale' })).toBeFocused();
    await expectNoViolations(page);

    await page.keyboard.press('Escape');
    await expect(dialog).toHaveCount(0);
  });
});

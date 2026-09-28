import { expect, test } from '../support/fixtures';
import { saleFor, uniqueCustomer, line } from '../support/sales';
import { PRODUCTS } from '../../src/app/core/catalog/demo-catalog';

const WIDTHS = [1280, 390];
const OUT = 'e2e/shots/.output';

test('capture every screen at desktop and phone widths', async ({ browser, signedInPage: page, api }) => {
  const sale = await api.createSale(saleFor(uniqueCustomer(), { items: [line(PRODUCTS[0], 5), line(PRODUCTS[1], 12), line(PRODUCTS[2], 2)] }));
  for (let i = 0; i < 14; i++) await api.createSale(saleFor(uniqueCustomer()));
  const signedOut = await browser.newPage();

  for (const width of WIDTHS) {
    await page.setViewportSize({ width, height: 900 });
    await signedOut.setViewportSize({ width, height: 900 });
    for (const [name, target, path] of [
      ['sign-in', signedOut, '/sign-in'], ['sign-up', signedOut, '/sign-up'], ['sales', page, '/sales'],
      ['new-sale', page, '/sales/new'], ['sale', page, `/sales/${sale.id}`], ['edit-sale', page, `/sales/${sale.id}/edit`],
      ['account', page, '/account'], ['not-found', page, '/nowhere'],
    ] as const) {
      await target.goto(path);
      await expect(target.getByRole('heading', { level: 1 })).toBeVisible();
      await target.screenshot({ path: `${OUT}/${name}-${width}.png`, fullPage: true });
    }
  }
  await signedOut.close();
});

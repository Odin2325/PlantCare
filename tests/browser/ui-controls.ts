import { expect, type Page } from '@playwright/test';

// Exercise the same disclosures people use; don't force hidden controls visible.
export async function openCatalogTools(page: Page, expectedCatalog = false) {
  if (expectedCatalog) await expect(page).toHaveURL(/\/plants(?:\?|$)/);
  if (new URL(page.url()).pathname !== '/plants') return;
  await expect(page.locator('main h1')).toBeVisible();
  await expect(page.getByText('Loading plant catalog...', { exact: true })).toBeHidden();
  const filters = page.getByRole('button', { name: /^Filters/ });
  if (await filters.count() && await filters.getAttribute('aria-expanded') === 'false') await filters.click();
  const extras = page.locator('.catalog-extras');
  if (await extras.count() && !await extras.evaluate(element => (element as HTMLDetailsElement).open)) await extras.locator(':scope > summary').click();
}

export async function openMoreMenu(page: Page) {
  const menu = page.locator('.app-more');
  if (await menu.count() && !await menu.evaluate(element => (element as HTMLDetailsElement).open)) await menu.locator(':scope > summary').click();
}

export async function openCalendarTools(page: Page) {
  await expect(page.locator('.calendar-page')).toBeVisible();
  for (const selector of ['.calendar-filter-panel', '.calendar-connections']) {
    const panel = page.locator(selector);
    if (await panel.count() && !await panel.evaluate(element => (element as HTMLDetailsElement).open)) await panel.locator(':scope > summary').click();
  }
}

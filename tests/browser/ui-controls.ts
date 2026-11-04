import { expect, type Page } from '@playwright/test';

// Exercise the same disclosures people use; don't force hidden controls visible.
export async function openCatalogTools(page: Page, expectedCatalog = false) {
  if (expectedCatalog) await expect(page).toHaveURL(/\/plants(?:\?|$)/);
  if (new URL(page.url()).pathname !== '/plants') return;
  await expect(page.locator('.catalog-page')).toBeVisible();
  await expect(page.getByText('Loading plant catalog...', { exact: true })).toBeHidden();
  const filters = page.getByRole('button', { name: /^Filters/ });
  if (await filters.count() && await filters.getAttribute('aria-expanded') === 'false') await filters.click();
  if (await filters.count()) await expect(filters).toHaveAttribute('aria-expanded', 'true');
  const extras = page.locator('.catalog-extras');
  if (await extras.count() && !await extras.evaluate(element => (element as HTMLDetailsElement).open)) await extras.locator(':scope > summary').click();
}

export async function openNotificationSettings(page: Page) {
  const settings = page.locator('.notification-settings');
  await expect(settings).toBeVisible();
  if (!await settings.evaluate(element => (element as HTMLDetailsElement).open)) await settings.locator(':scope > summary').click();
}

export async function openMoreMenu(page: Page) {
  // Authentication can update the header before its redirect finishes. Opening
  // More during that gap would be immediately undone by NavigationEnd.
  await expect(page.locator('main:not(.auth-page)')).toBeVisible();
  const menu = page.locator('.app-more');
  if (await menu.count() && !await menu.evaluate(element => (element as HTMLDetailsElement).open)) await menu.locator(':scope > summary').click();
  if (await menu.count()) await expect(menu).toHaveJSProperty('open', true);
}

export async function openCalendarTools(page: Page) {
  await expect(page.locator('.calendar-page')).toBeVisible();
  for (const selector of ['.calendar-filter-panel', '.calendar-connections']) {
    const panel = page.locator(selector);
    if (await panel.count() && !await panel.evaluate(element => (element as HTMLDetailsElement).open)) await panel.locator(':scope > summary').click();
  }
}

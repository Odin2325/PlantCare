import { expect, test } from '@playwright/test';
import { auditAccessibility } from './accessibility';

// Keep this synthetic read-only API fixture separate from the real-worker PWA suite.
test.use({ serviceWorkers: 'block' });

test('garden collection groups owned crops, filters and links without writing care data', async ({ page }) => {
  const profile = (categories: string[]) => ({ categories, reviewedOn: '2026-10-25',
    careSourceUrl: 'https://example.org/guide', photo: null, harvestInstructions: 'Pick ripe fruit after checking local conditions.' });
  const make = (id: string, nickname: string, categories: string[], location: string | null) => ({
    id, nickname, speciesCommonName: id === 'fig' ? 'Fig tree' : 'Tomato', speciesScientificName: 'Reference plant',
    speciesCatalogProfile: profile(categories), location, tags: [], isActive: true, careSchedules: [],
  });
  const plants = [make('tomato', 'Balcony favourite', ['vegetable'], ' Balcony '),
    make('fig', 'My fig', ['fruit', 'tree'], null), make('houseplant', 'Indoor plant', ['houseplant'], 'Room')];
  let fail = true; const writes: string[] = [];
  await page.route('**/api/**', async route => {
    const path = new URL(route.request().url()).pathname;
    if (route.request().method() !== 'GET') { writes.push(path); return route.fulfill({ status: 405 }); }
    if (path === '/api/account/me') return route.fulfill({ json: { id: 'synthetic-owner', email: 'garden@example.test' } });
    if (path === '/api/antiforgery/token') return route.fulfill({ json: { requestToken: 'synthetic-token' } });
    if (path === '/api/my-plants') return route.fulfill(fail ? { status: 503, json: {} } : { json: plants });
    return route.fulfill({ status: 404, json: {} });
  });
  await page.goto('/garden');
  await expect(page.getByRole('alert')).toContainText('could not be loaded');
  fail = false; await page.getByRole('button', { name: 'Try again', exact: true }).click();
  await expect(page.locator('.garden-card h3')).toHaveText(['Balcony favourite', 'My fig']);
  await expect(page.locator('.location h2')).toHaveText(['Balcony', 'Location not set']);
  await expect(page.getByRole('link', { name: 'Balcony favourite', exact: true })).toHaveAttribute('href', '/my-plants/tomato');
  await page.getByRole('button', { name: 'Trees', exact: true }).click();
  await expect(page.locator('.garden-card h3')).toHaveText(['My fig']);
  await page.getByRole('searchbox', { name: 'Find a garden plant', exact: true }).fill('not-present');
  await expect(page.getByText('No garden plants match these filters.', { exact: false })).toBeVisible();
  await page.getByRole('button', { name: 'Clear garden filters', exact: true }).click();
  await page.locator('.garden-card').first().getByText('Harvest guidance', { exact: true }).click();
  await expect(page.locator('.garden-card').first()).toContainText('Pick ripe fruit');
  await page.setViewportSize({ width: 320, height: 900 });
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await auditAccessibility(page, 'Garden collection');
  expect(writes).toEqual([]);
});

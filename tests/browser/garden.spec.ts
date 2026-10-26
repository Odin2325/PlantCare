import { expect, test } from '@playwright/test';
import { auditAccessibility } from './accessibility';

// Keep this synthetic read-only API fixture separate from the real-worker PWA suite.
test.use({ serviceWorkers: 'block', timezoneId: 'America/Los_Angeles' });

test('garden collection groups owned crops, filters and links without writing care data', async ({ page }) => {
  const profile = (categories: string[]) => ({ categories, reviewedOn: '2026-10-25',
    careSourceUrl: 'https://example.org/guide', photo: null, harvestInstructions: 'Pick ripe fruit after checking local conditions.' });
  const make = (id: string, nickname: string, categories: string[], location: string | null) => ({
    id, nickname, speciesCommonName: id === 'fig' ? 'Fig tree' : 'Tomato', speciesScientificName: 'Reference plant',
    plannedSowingOn: id === 'tomato' ? '2027-03-01' : null, expectedHarvestOn: id === 'tomato' ? '2027-08-01' : null,
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
  await page.getByLabel('Planting plan', { exact: true }).selectOption('planned');
  await expect(page.locator('.garden-card h3')).toHaveText(['Balcony favourite']);
  await expect(page.locator('.garden-card .plan-dates')).toContainText('Aug 1, 2027');
  await page.getByLabel('Planting plan', { exact: true }).selectOption('unplanned');
  await expect(page.locator('.garden-card h3')).toHaveText(['My fig']);
  await page.getByLabel('Planting plan', { exact: true }).selectOption('');
  await page.getByRole('button', { name: 'Trees', exact: true }).click();
  await expect(page.locator('.garden-card h3')).toHaveText(['My fig']);
  await page.getByRole('searchbox', { name: 'Find a garden plant', exact: true }).fill('not-present');
  await expect(page.getByText('No garden plants match these filters.', { exact: false })).toBeVisible();
  await page.getByRole('button', { name: 'Clear garden filters', exact: true }).click();
  const calendar = page.getByRole('region', { name: 'Planting calendar', exact: true });
  await calendar.getByLabel('Go to month', { exact: true }).fill('2027-03');
  await expect(calendar.getByRole('link', { name: 'Balcony favourite · Planned sowing' })).toHaveAttribute('href', '/my-plants/tomato');
  await calendar.getByRole('button', { name: 'Next month', exact: true }).click();
  await expect(calendar.getByRole('status')).toContainText('No planting dates');
  await calendar.getByLabel('Go to month', { exact: true }).fill('2027-08');
  await expect(calendar.getByRole('link', { name: 'Balcony favourite · Expected harvest' })).toBeVisible();
  await page.getByRole('button', { name: 'Trees', exact: true }).click();
  await expect(calendar.getByRole('status')).toContainText('No planting dates');
  await page.getByRole('button', { name: 'Clear garden filters', exact: true }).click();
  await page.locator('.garden-card').first().getByText('Harvest guidance', { exact: true }).click();
  await expect(page.locator('.garden-card').first()).toContainText('Pick ripe fruit');
  await page.setViewportSize({ width: 320, height: 900 });
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await auditAccessibility(page, 'Garden collection');
  expect(writes).toEqual([]);
});

test('garden collection harvest overview filters complete per-unit totals and retries without writes', async ({ page }) => {
  const plants = [
    { id: 'tomato', nickname: 'Balcony favourite', speciesCommonName: 'Tomato', tags: [], isActive: true, careSchedules: [], speciesCatalogProfile: { categories: ['vegetable'] } },
    { id: 'fig', nickname: null, speciesCommonName: 'Fig tree', tags: [], isActive: true, careSchedules: [], speciesCatalogProfile: { categories: ['fruit', 'tree'] } },
  ];
  let fail = true; const writes: string[] = []; const requests: string[] = [];
  await page.route('**/api/**', async route => {
    const url = new URL(route.request().url());
    if (route.request().method() !== 'GET') { writes.push(url.pathname); return route.fulfill({ status: 405 }); }
    if (url.pathname === '/api/account/me') return route.fulfill({ json: { id: 'synthetic-owner', email: 'garden@example.test' } });
    if (url.pathname === '/api/antiforgery/token') return route.fulfill({ json: { requestToken: 'synthetic-token' } });
    if (url.pathname === '/api/my-plants') return route.fulfill({ json: plants });
    if (url.pathname === '/api/my-plants/harvest-overview') {
      requests.push(url.search); if (fail) return route.fulfill({ status: 503 });
      return route.fulfill({ json: { from: url.searchParams.get('from'), to: url.searchParams.get('to'), rows: [
        { id: 'r', userPlantId: 'tomato', harvestedOn: '2026-03-01', quantity: 1.001, unit: 'kg', notes: '<b>Plain notes</b>' }],
        plants: [ { userPlantId: 'tomato', recordCount: 102, unquantifiedCount: 1, quantities: { kg: '1.001', g: '3.000' } },
          { userPlantId: 'fig', recordCount: 1, unquantifiedCount: 0, quantities: { kg: '2.002' } } ] } });
    }
    return route.fulfill({ status: 404, json: {} });
  });
  await page.goto('/garden');
  await page.getByRole('button', { name: 'Harvest overview', exact: true }).click();
  const overview = page.getByRole('region', { name: 'Harvest overview', exact: true });
  await expect(overview.getByRole('alert')).toContainText('could not be loaded');
  fail = false; await overview.getByRole('button', { name: 'Retry harvest overview' }).click();
  await expect(overview.getByRole('status')).toHaveText('103 harvest records · 1 without quantity');
  await expect(overview.getByLabel('Harvest totals')).toHaveText('3 g3.003 kg');
  await expect(overview.locator('.records a')).toHaveAttribute('href', '/my-plants/tomato');
  await expect(overview.locator('.records a')).toHaveText('Balcony favourite');
  await expect(overview.locator('.records')).toContainText('Mar 1, 2026');
  await expect(overview.locator('.records b')).toHaveCount(0);
  await expect(overview.locator('.records')).toContainText('<b>Plain notes</b>');
  await page.getByRole('button', { name: 'Trees', exact: true }).click();
  await expect(overview.getByRole('status')).toHaveText('1 harvest records · 0 without quantity');
  await expect(overview.getByLabel('Harvest totals')).toHaveText('2.002 kg');
  await expect(overview.locator('.records li')).toHaveCount(0);
  await expect(overview.getByRole('link', { name: 'Fig tree' })).toHaveAttribute('href', '/my-plants/fig');
  await page.getByRole('button', { name: 'Clear garden filters' }).click();
  await overview.getByLabel('Harvest from', { exact: true }).fill('2026-01-01');
  await overview.getByLabel('Harvest to', { exact: true }).fill('2026-12-31');
  await overview.getByRole('button', { name: 'Apply harvest dates' }).click();
  await expect(overview.getByText('Jan 1, 2026 – Dec 31, 2026', { exact: true })).toBeVisible();
  expect(requests.at(-1)).toContain('to=2026-12-31');
  await page.setViewportSize({ width: 320, height: 900 });
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await auditAccessibility(page, 'Garden harvest overview');
  expect(writes).toEqual([]);
});

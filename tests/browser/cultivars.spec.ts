import { expect, test } from '@playwright/test';
import { auditAccessibility } from './accessibility';

test.use({ serviceWorkers: 'block' });

test('related cultivars preserve browsing context and show exact photos or named pending placeholders without writes', async ({ page }) => {
  const ids = { general: '30000000-0000-4000-8000-000000000001', tiny: '30000000-0000-4000-8000-000000000002', roma: '30000000-0000-4000-8000-000000000003' };
  const photo = (path: string) => ({ path, altText: 'Source-identified tomato photograph', title: 'Tomato reference',
    credit: 'Source photographer', sourceUrl: 'https://commons.wikimedia.org/wiki/File:Roma_tomatoes_(509115549).jpg',
    licenseName: 'CC BY-SA 2.0', licenseUrl: 'https://creativecommons.org/licenses/by-sa/2.0/' });
  const make = (id: string, commonName: string, cultivarName: string | null, path: string | null) => ({
    id, commonName, scientificName: 'Solanum lycopersicum' + (cultivarName ? ` '${cultivarName}'` : ''),
    description: 'Synthetic reviewed crop', sunlightRequirement: 'FullSun', sunlightInstructions: 'Sunny place',
    defaultWateringIntervalDays: 7, wateringInstructions: 'Check soil moisture', defaultFertilizingIntervalDays: null,
    fertilizingInstructions: null, soilInstructions: 'Drained soil', humidityInstructions: null,
    minimumTemperatureCelsius: null, maximumTemperatureCelsius: null, isToxicToPets: false,
    catalogProfile: { cultivarName, photo: path ? photo(path) : null, photoIsSpeciesReference: false,
      categories: ['vegetable'], careSourceUrl: 'https://www.rhs.org.uk/vegetables/tomatoes/grow-your-own', reviewedOn: '2026-10-30' },
  });
  const catalog = [make(ids.general, 'Tomato', null, '/images/plants/solanum-lycopersicum.jpg'),
    make(ids.tiny, 'Cherry tomato (Tiny Tim)', 'Tiny Tim', null),
    make(ids.roma, 'Plum tomato (Roma)', 'Roma', '/images/plants/cultivar-plum-tomato-roma.jpg')];
  const before = JSON.stringify(catalog); const writes: string[] = []; let fail = true;
  await page.route('**/api/**', async route => {
    const path = new URL(route.request().url()).pathname;
    if (route.request().method() !== 'GET') { writes.push(path); return route.fulfill({ status: 405 }); }
    if (path === '/api/account/me') return route.fulfill({ status: 401, json: {} });
    if (path === '/api/antiforgery/token') return route.fulfill({ json: { requestToken: 'synthetic' } });
    if (path === '/api/plant-species') return route.fulfill(fail ? { status: 503, json: {} } : { json: catalog });
    const plant = catalog.find(p => path === '/api/plant-species/' + p.id);
    return route.fulfill(plant ? { json: plant } : { status: 404, json: {} });
  });
  await page.goto('/plants/' + ids.tiny + '?category=vegetable&q=tomato&compare=' + ids.general);
  await expect(page.getByRole('heading', { level: 1 })).toHaveText('Cherry tomato (Tiny Tim)');
  const primary = page.locator('.plant-detail-page > app-plant-photo');
  await expect(primary).toContainText('Cultivar photo pending'); await expect(primary).toContainText('Tiny Tim');
  await expect(primary.locator('img')).toHaveCount(0);
  const relatives = page.getByRole('region', { name: 'Related varieties', exact: true });
  await expect(relatives.getByRole('alert')).toContainText('could not be loaded');
  fail = false; await relatives.getByRole('button', { name: 'Try related varieties again', exact: true }).click();
  await expect(relatives.locator('h3')).toHaveText(['Plum tomato (Roma)', 'Tomato']);
  await relatives.getByRole('link', { name: 'Plum tomato (Roma)', exact: true }).click();
  await expect(page.getByRole('heading', { level: 1 })).toHaveText('Plum tomato (Roma)');
  const url = new URL(page.url()); expect(url.pathname).toBe('/plants/' + ids.roma);
  expect(url.searchParams.get('category')).toBe('vegetable'); expect(url.searchParams.get('q')).toBe('tomato');
  expect(url.searchParams.get('compare')).toBe(ids.general);
  const image = primary.locator('img'); await image.scrollIntoViewIfNeeded();
  await expect(image).toHaveAttribute('src', '/images/plants/cultivar-plum-tomato-roma.jpg');
  await expect.poll(() => image.evaluate((i: HTMLImageElement) => i.complete && i.naturalWidth > 0)).toBe(true);
  await expect(primary.getByRole('link', { name: 'CC BY-SA 2.0', exact: true })).toBeVisible();
  await expect(relatives.locator('h3')).toHaveText(['Cherry tomato (Tiny Tim)', 'Tomato']);
  await expect(relatives).toContainText('Cultivar photo pending');
  await page.setViewportSize({ width: 320, height: 900 });
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await auditAccessibility(page, 'Related cultivar photos and pending placeholders');
  expect(writes).toEqual([]); expect(JSON.stringify(catalog)).toBe(before);
});

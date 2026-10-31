import { expect, test } from '@playwright/test';
import { auditAccessibility } from './accessibility';

test.use({ serviceWorkers: 'block' });

test('related cultivars: photo filters combine with search and preserve URL context and comparison without writes', async ({ page }) => {
  const make = (suffix: string, cultivarName: string | null, photo: object | null, photoIsSpeciesReference = false) => ({
    id: `30000000-0000-4000-8000-00000000000${suffix}`,
    commonName: cultivarName ? `Tomato (${cultivarName})` : 'Tomato',
    scientificName: 'Solanum lycopersicum' + (cultivarName ? ` '${cultivarName}'` : ''),
    description: 'Synthetic catalog fixture', sunlightRequirement: 'FullSun', sunlightInstructions: 'Sunny place',
    defaultWateringIntervalDays: 7, wateringInstructions: 'Check moisture', defaultFertilizingIntervalDays: null,
    fertilizingInstructions: null, soilInstructions: 'Drained soil', humidityInstructions: null,
    minimumTemperatureCelsius: null, maximumTemperatureCelsius: null, isToxicToPets: false,
    catalogProfile: { cultivarName, photo, photoIsSpeciesReference, categories: ['vegetable'],
      careSourceUrl: 'https://www.rhs.org.uk/vegetables/tomatoes/grow-your-own', reviewedOn: '2026-10-31' },
  });
  const photo = { path: '/images/plants/cultivar-plum-tomato-roma.jpg', altText: 'Roma tomato',
    title: 'Roma tomato', credit: 'Source photographer',
    sourceUrl: 'https://commons.wikimedia.org/wiki/File:Roma_tomatoes_(509115549).jpg',
    licenseName: 'CC BY-SA 2.0', licenseUrl: 'https://creativecommons.org/licenses/by-sa/2.0/' };
  const catalog = [make('1', null, photo), make('2', 'Tiny Tim', null),
    make('3', 'Roma', photo), make('4', 'Legacy', photo, true)];
  const writes: string[] = [];
  await page.route('**/api/**', async route => {
    const path = new URL(route.request().url()).pathname;
    if (route.request().method() !== 'GET') { writes.push(path); return route.fulfill({ status: 405 }); }
    if (path === '/api/account/me') return route.fulfill({ status: 401, json: {} });
    if (path === '/api/antiforgery/token') return route.fulfill({ json: { requestToken: 'synthetic' } });
    if (path === '/api/plant-species') return route.fulfill({ json: catalog });
    const plant = catalog.find(p => path === '/api/plant-species/' + p.id);
    return route.fulfill(plant ? { json: plant } : { status: 404, json: {} });
  });
  await page.goto('/plants?category=vegetable&q=tomato&cultivarPhoto=available&compare=' + catalog[0].id);
  const cards = page.locator('.plant-card');
  const filter = page.getByRole('combobox', { name: 'Cultivar photos', exact: true });
  await expect(filter).toHaveValue('available');
  await expect(cards.locator('h2')).toHaveText(['Tomato (Roma)']);
  await cards.getByRole('checkbox', { name: 'Compare Tomato (Roma)', exact: true }).check();
  await expect(page).toHaveURL(url => url.searchParams.get('compare') === catalog[0].id + ',' + catalog[2].id);
  await cards.getByRole('link', { name: 'View details', exact: true }).click();
  await expect(page.getByRole('heading', { level: 1 })).toHaveText('Tomato (Roma)');
  expect(new URL(page.url()).searchParams.get('cultivarPhoto')).toBe('available');
  await page.goBack();
  await expect(filter).toHaveValue('available');
  await filter.selectOption('pending');
  await expect(cards.locator('h2')).toHaveText(['Tomato (Legacy)', 'Tomato (Tiny Tim)']);
  await expect(cards.locator('img')).toHaveCount(0);
  await page.reload(); await expect(filter).toHaveValue('pending');
  await page.getByRole('searchbox', { name: 'Find a plant', exact: true }).fill('Tiny Tim');
  await expect(cards.locator('h2')).toHaveText(['Tomato (Tiny Tim)']);
  expect(new URL(page.url()).searchParams.get('category')).toBe('vegetable');
  await page.setViewportSize({ width: 320, height: 900 });
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await auditAccessibility(page, 'Cultivar photo filters');
  await page.getByRole('button', { name: 'Clear filters', exact: true }).click();
  await expect(cards).toHaveCount(4); await expect(filter).toHaveValue('');
  const url = new URL(page.url());
  expect(url.searchParams.has('cultivarPhoto')).toBe(false);
  expect(url.searchParams.get('compare')).toBe(catalog[0].id + ',' + catalog[2].id);
  await expect(page.getByRole('link', { name: 'Compare selected plants', exact: true })).toBeVisible();
  expect(writes).toEqual([]);
});

test('related cultivars preserve browsing context and show exact photos or named pending placeholders without writes', async ({ page }) => {
  await page.addInitScript(() => Object.defineProperty(navigator, 'clipboard', { configurable: true,
    value: { writeText: async (value: string) => { (window as any).__copiedComparison = value; } } }));
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
      growingNotes: cultivarName ? `Growing notes for ${cultivarName}.` : null,
      harvestInstructions: cultivarName ? `Harvest guidance for ${cultivarName}.` : null,
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
  await relatives.getByRole('link', { name: 'Compare Plum tomato (Roma) with Cherry tomato (Tiny Tim)', exact: true }).click();
  await expect(page.getByRole('heading', { level: 1 })).toHaveText('Compare plants');
  await expect(page.locator('.comparison-card h2')).toHaveText(['Plum tomato (Roma)', 'Cherry tomato (Tiny Tim)']);
  const comparisonCards = page.locator('.comparison-card');
  await expect(comparisonCards.nth(0)).toContainText('Growing notes for Roma.');
  await expect(comparisonCards.nth(0)).toContainText('Harvest guidance for Roma.');
  await expect(comparisonCards.nth(0)).not.toContainText('Harvest guidance for Tiny Tim.');
  await expect(comparisonCards.nth(1)).toContainText('Harvest guidance for Tiny Tim.');
  await expect(comparisonCards.nth(1).getByRole('link', { name: 'Care reference', exact: true }))
    .toHaveAttribute('href', 'https://www.rhs.org.uk/vegetables/tomatoes/grow-your-own');
  const comparisonUrl = new URL(page.url());
  expect(comparisonUrl.searchParams.get('compare')).toBe(ids.roma + ',' + ids.tiny);
  expect(comparisonUrl.searchParams.get('category')).toBe('vegetable');
  expect(comparisonUrl.searchParams.get('q')).toBe('tomato');
  const shareLink = page.getByRole('textbox', { name: 'Comparison link', exact: true });
  const sharedUrl = await shareLink.inputValue();
  expect([...new URL(sharedUrl).searchParams.entries()]).toEqual([['compare', ids.roma + ',' + ids.tiny]]);
  await page.getByRole('button', { name: 'Copy comparison link', exact: true }).click();
  await expect(page.getByText('Comparison link copied.', { exact: true })).toBeVisible();
  expect(await page.evaluate(() => (window as any).__copiedComparison)).toBe(sharedUrl);
  await page.setViewportSize({ width: 320, height: 900 });
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await auditAccessibility(page, 'Compared growing and harvest guidance');
  await page.goto(sharedUrl);
  await expect(page.locator('.comparison-card h2')).toHaveText(['Plum tomato (Roma)', 'Cherry tomato (Tiny Tim)']);
  await page.goBack();
  await page.goBack();
  await expect(page.getByRole('heading', { level: 1 })).toHaveText('Plum tomato (Roma)');
  expect(new URL(page.url()).searchParams.get('compare')).toBe(ids.general);
  await page.setViewportSize({ width: 320, height: 900 });
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await auditAccessibility(page, 'Related cultivar photos and pending placeholders');
  expect(writes).toEqual([]); expect(JSON.stringify(catalog)).toBe(before);
});

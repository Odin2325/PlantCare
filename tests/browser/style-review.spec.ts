import { expect, test } from '@playwright/test';
import { auditAccessibility } from './accessibility';

test.use({ serviceWorkers: 'block' });

// Entirely synthetic, read-only fixtures: this suite never reaches account data.
const speciesId = '30000000-0000-4000-8000-000000000001';
const secondSpeciesId = '30000000-0000-4000-8000-000000000002';
const plantId = '10000000-0000-4000-8000-000000000001';
const species = {
  id: speciesId, commonName: 'Tomato (Roma)', scientificName: "Solanum lycopersicum 'Roma'",
  description: 'A compact favourite for a sunny balcony and summer harvests.',
  sunlightRequirement: 'FullSun', sunlightInstructions: 'Choose a sunny spot.',
  defaultWateringIntervalDays: 7, wateringInstructions: 'Check the soil before watering.',
  defaultFertilizingIntervalDays: null, fertilizingInstructions: null,
  soilInstructions: 'Use well-drained soil.', humidityInstructions: null,
  minimumTemperatureCelsius: 15, maximumTemperatureCelsius: 30, isToxicToPets: true,
  catalogProfile: { cultivarName: 'Roma', categories: ['vegetable'], photo: null,
    careSourceUrl: 'https://www.rhs.org.uk/vegetables/tomatoes/grow-your-own', reviewedOn: '2026-10-31' },
};
const plant = {
  id: plantId, plantSpeciesId: speciesId, nickname: 'Sunny balcony tomato',
  speciesCommonName: species.commonName, speciesScientificName: species.scientificName,
  speciesCatalogProfile: species.catalogProfile, location: 'Balcony', tags: ['Summer'],
  acquiredOn: '2026-04-01', notes: 'Watch the new leaves.', isActive: true,
  createdAtUtc: '2026-04-01T12:00:00Z', defaultWateringIntervalDays: 7,
  defaultFertilizingIntervalDays: null, careSchedules: [],
};
const secondSpecies = { ...species, id: secondSpeciesId, commonName: 'Swiss cheese plant',
  scientificName: 'Monstera deliciosa', description: 'A climbing foliage plant with large split leaves.',
  catalogProfile: { categories: ['houseplant'], reviewedOn: '2026-10-22',
    careSourceUrl: 'https://plants.ces.ncsu.edu/plants/monstera-deliciosa/',
    photo: { path: '/images/plants/monstera-deliciosa.jpg', altText: 'Swiss cheese plant reference photo',
      credit: 'Maja Dumat', sourceUrl: 'https://plants.ces.ncsu.edu/plants/monstera-deliciosa/',
      licenseName: 'CC BY 2.0', licenseUrl: 'https://creativecommons.org/licenses/by/2.0/' } } };

test('page families reflow with visible focus and accessible controls', async ({ page }, testInfo) => {
  const writes: string[] = [], errors: string[] = [];
  let signedIn = true;
  page.on('pageerror', error => errors.push(error.message));
  await page.clock.setFixedTime(new Date('2026-11-03T12:00:00Z'));
  await page.route('**/api/**', async route => {
    const request = route.request(), path = new URL(request.url()).pathname;
    if (request.method() !== 'GET') { writes.push(path); return route.fulfill({ status: 405 }); }
    if (path === '/api/account/me') return route.fulfill(signedIn
      ? { json: { id: 'style-owner', email: 'style-review@example.test' } } : { status: 401 });
    if (path === '/api/antiforgery/token') return route.fulfill({ json: { requestToken: 'synthetic' } });
    if (path === '/api/plant-species') return route.fulfill({ json: [species, secondSpecies] });
    if (path === '/api/plant-species/' + speciesId) return route.fulfill({ json: species });
    if (path === '/api/my-plants') return route.fulfill({ json: [plant] });
    if (path === '/api/my-plants/' + plantId) return route.fulfill({ json: plant });
    if (path === '/api/dashboard/care') return route.fulfill({ json: [{ userPlantId: plantId,
      plantName: plant.nickname, speciesCommonName: species.commonName, actionType: 'Watering',
      dueAtUtc: '2026-11-03T10:00:00Z', status: 'DueToday' }] });
    if (path === '/api/calendar') return route.fulfill({ json: ['Scheduled', 'Completed'].map((kind, index) => ({ id: 'event-' + index, userPlantId: plantId,
      plantName: plant.nickname, actionType: index ? 'Fertilizing' : 'Watering', startsAtUtc: '2026-11-03T10:00:00Z', kind })) });
    if (path === '/api/calendar/subscription') return route.fulfill({ json: { isActive: false, createdAtUtc: null } });
    if (path.includes('/integrations/')) return route.fulfill({ json: { isConfigured: false, isConnected: false } });
    if (path.endsWith('/capabilities')) return route.fulfill({ json: { uploadsEnabled: false, canUpload: false, maximumFileBytes: 8388608 } });
    if (path === '/api/push/config') return route.fulfill({ json: { isEnabled: false, publicKey: '' } });
    if (path === '/api/notifications/preferences') return route.fulfill({ json: {
      inAppEnabled: true, pushEnabled: false, wateringEnabled: true, fertilizingEnabled: true,
      mistingEnabled: true, pruningEnabled: true, repottingEnabled: true, reminderLeadTimeHours: 0 } });
    if (path === '/api/notifications') return route.fulfill({ json: Array.from({ length: 13 }, (_, index) => ({ id: 'reminder-' + index, userPlantId: plantId,
      plantName: plant.nickname, actionType: 'Watering', dueAtUtc: '2026-11-03T10:00:00Z',
      createdAtUtc: '2026-11-03T09:00:00Z', readAtUtc: null, canSnooze: true })) });
    return route.fulfill({ json: [] });
  });
  const pages = ['/plants', '/plants/' + speciesId, '/plants/compare?compare=' + speciesId + ',' + secondSpeciesId,
    '/dashboard', '/my-plants', '/garden', '/calendar', '/notifications',
    '/my-plants/' + plantId, '/my-plants/' + plantId + '/care-history', '/account'];
  for (const width of [1440, 768, 320]) {
    await page.setViewportSize({ width, height: 960 });
    for (const path of pages) {
      await page.goto(path);
      await expect(page.locator('main h1')).toBeVisible();
      await expect(page.getByRole('main')).toBeVisible();
      const overflow = await page.evaluate(() => [...document.querySelectorAll('main *')]
        .filter(element => element.getBoundingClientRect().right > innerWidth + 1)
        .map(element => element.tagName + '.' + element.className));
      expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1), path + ' at ' + width + ': ' + overflow.join(', ')).toBe(true);
      if (width === 320) {
        const smallTouchFields = await page.evaluate(() => !matchMedia('(hover: none) and (pointer: coarse)').matches ? [] :
          [...document.querySelectorAll('input:not([type=checkbox], [type=radio], [type=range], [type=color]), select, textarea')]
            .filter(e => e.getBoundingClientRect().height > 0 && parseFloat(getComputedStyle(e).fontSize) < 16)
            .map(e => e.tagName + '.' + e.className));
        expect(smallTouchFields, path + ': small touch form text').toEqual([]);
      }
      if (width === 320) await auditAccessibility(page, path);
      if (process.env.STYLE_SCREENSHOTS && testInfo.project.name === 'desktop-chromium' && width !== 768) {
        await page.evaluate(() => (document.activeElement as HTMLElement)?.blur());
        const name = path.split('?')[0].replaceAll('/', '_');
        await page.screenshot({ path: `/private/style-${width}${name}.png`, fullPage: true });
      }
    }
  }
  await page.getByRole('navigation', { name: 'Main navigation' }).getByText('More', { exact: true }).click();
  await page.getByRole('button', { name: 'Log out', exact: true }).focus();
  expect(await page.getByRole('button', { name: 'Log out', exact: true }).evaluate(element => {
    const box = element.getBoundingClientRect();
    return box.left >= 0 && box.right <= innerWidth;
  })).toBe(true);
  await page.goto('/plants');
  const filters = page.getByRole('button', { name: /^Filters/ });
  await expect(filters).toHaveAttribute('aria-expanded', 'false');
  await expect(page.getByLabel('Entry type', { exact: true })).toBeHidden();
  await page.getByRole('searchbox', { name: 'Find a plant', exact: true }).fill('Tomato');
  await expect(page.locator('.plant-card')).toHaveCount(1);
  await filters.focus(); await page.keyboard.press('Enter');
  await page.getByLabel('Cats and dogs', { exact: true }).selectOption('warning');
  await filters.click();
  await expect(filters).toHaveText(/Filters\s+1/);
  await expect(page.locator('.plant-card')).toHaveCount(1);
  await filters.click(); await auditAccessibility(page, 'Expanded catalog filters');
  await page.goto('/notifications');
  await expect(page.locator('.notification-card')).toHaveCount(12);
  await page.getByRole('navigation', { name: 'Notification pages', exact: true }).getByRole('button', { name: 'Next', exact: true }).click();
  await expect(page.locator('.notification-card')).toHaveCount(1);
  await page.getByRole('navigation', { name: 'Notification pages', exact: true }).getByRole('button', { name: 'Previous', exact: true }).click();
  await expect(page.locator('.notification-card')).toHaveCount(12);
  await page.locator('.notification-settings > summary').click();
  await auditAccessibility(page, 'Expanded notification preferences');
  await page.goto('/calendar');
  await expect(page.getByRole('region', { name: 'Selected day care', exact: true })).toContainText('Sunny balcony tomato');
  expect(await page.locator('.month-calendar').evaluate(element => element.scrollWidth <= element.clientWidth + 1)).toBe(true);
  await page.getByRole('button', { name: /Wednesday, November 4, 2026, 0 care events/ }).click();
  await expect(page.getByRole('region', { name: 'Selected day care', exact: true })).toContainText('No care events');
  for (const view of ['Week', 'Agenda', 'Month']) {
    await page.getByRole('group', { name: 'Calendar view', exact: true }).getByRole('button', { name: view, exact: true }).click();
    await auditAccessibility(page, 'Calendar ' + view);
  }
  await page.locator('.calendar-connections > summary').click();
  await auditAccessibility(page, 'Calendar connections');
  signedIn = false;
  for (const path of ['/login', '/register', '/forgot-password', '/resend-confirmation', '/reset-password']) {
    await page.goto(path);
    await expect(page.locator('main h1')).toBeVisible();
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1)).toBe(true);
    await auditAccessibility(page, path);
    if (process.env.STYLE_SCREENSHOTS && testInfo.project.name === 'desktop-chromium' && path === '/login') {
      await page.evaluate(() => (document.activeElement as HTMLElement)?.blur());
      await page.screenshot({ path: '/private/style-320_login.png', fullPage: true });
    }
  }
  await page.goto('/login');
  await page.getByLabel('Email address', { exact: true }).focus();
  expect(await page.evaluate(() => {
    const style = getComputedStyle(document.activeElement!);
    return style.outlineStyle !== 'none' && parseFloat(style.outlineWidth) >= 3;
  })).toBe(true);
  expect(writes).toEqual([]);
  expect(errors).toEqual([]);
});


import { expect, test } from '@playwright/test';
import { auditAccessibility } from './accessibility';

test('English houseplants have working local photos, attribution and searchable care details', async ({ page, request }) => {
  const response = await request.get('/api/plant-species');
  expect(response.ok()).toBe(true);
  const species = await response.json();
  const reviewed = species.filter((plant: { catalogProfile?: { photo?: unknown } }) => plant.catalogProfile?.photo);
  expect(reviewed.length).toBeGreaterThanOrEqual(20);
  for (const plant of reviewed) {
    const photo = plant.catalogProfile.photo;
    expect(photo.path).toMatch(/^\/images\/plants\/[a-z0-9-]+\.(jpg|png|jpeg|webp)$/);
    const image = await request.get(photo.path);
    expect(image.ok(), `Image for ${plant.commonName}`).toBe(true);
    expect(image.headers()['content-type']).toMatch(/^image\//);
  }
  await page.goto('/plants');
  await page.getByRole('searchbox', { name: 'Find a plant' }).fill('Monstera deliciosa');
  const card = page.locator('.plant-card');
  await expect(card).toHaveCount(1);
  await expect(card.getByRole('heading', { name: 'Swiss cheese plant' })).toBeVisible();
  const image = card.getByRole('img');
  await expect(image).toBeVisible();
  await expect.poll(() => image.evaluate((element: HTMLImageElement) => element.naturalWidth)).toBeGreaterThan(0);
  await expect(card.getByRole('link', { name: 'CC BY 2.0', exact: true })).toHaveAttribute('href', 'https://creativecommons.org/licenses/by/2.0/');
  await auditAccessibility(page, 'Filtered English catalog');
  await card.getByRole('link', { name: 'View details' }).click();
  await expect(page.getByRole('heading', { name: 'Swiss cheese plant', exact: true })).toBeVisible();
  await expect(page.getByRole('link', { name: 'Care reference', exact: true })).toHaveAttribute('href', 'https://plants.ces.ncsu.edu/plants/monstera-deliciosa/');
  await expect(page.getByText('Suggested check: every', { exact: false })).toBeVisible();
  await auditAccessibility(page, 'English plant care detail');
  await page.goto('/plants');
  await page.getByRole('searchbox', { name: 'Find a plant' }).fill('no-such-houseplant-xyz');
  await expect(page.getByText('No plants match your search. Try another name or clear the filters.')).toBeVisible();
});

test('catalog light filters combine with search, survive detail navigation and reset together', async ({ page }) => {
  await page.goto('/plants?light=PartialSun&sort=name-desc');
  const cards = page.locator('.plant-card');
  const names = cards.getByRole('heading', { level: 2 });
  await expect(names).toHaveText(['Jade plant', 'Aloe vera']);
  await expect(page.getByLabel('Light requirement', { exact: true })).toHaveValue('PartialSun');
  await expect(page.getByLabel('Sort by', { exact: true })).toHaveValue('name-desc');
  const search = page.getByRole('searchbox', { name: 'Find a plant' });
  await search.pressSequentially('Aloe vera', { delay: 40 });
  await expect(names).toHaveText(['Aloe vera']);
  await expect(page).toHaveURL(/q=Aloe(?:\+|%20)vera/);
  await expect(search).toBeFocused();
  await cards.getByRole('link', { name: 'View details' }).click();
  await expect(page.getByRole('heading', { level: 1, name: 'Aloe vera', exact: true })).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Soil', exact: true })).toBeVisible();
  await page.getByRole('link', { name: 'Back to plant catalog', exact: false }).click();
  await expect(names).toHaveText(['Aloe vera']);
  await expect(page.getByRole('searchbox', { name: 'Find a plant' })).toHaveValue('Aloe vera');
  await page.reload();
  await expect(names).toHaveText(['Aloe vera']);
  await page.getByLabel('Light requirement', { exact: true }).selectOption('BrightIndirectLight');
  await expect(cards).toHaveCount(0);
  await expect(page.getByText('No plants match your search. Try another name or clear the filters.')).toBeVisible();
  await page.getByRole('button', { name: 'Clear filters', exact: true }).click();
  await expect(page).toHaveURL(/\/plants$/);
  await expect(page.getByRole('searchbox', { name: 'Find a plant' })).toHaveValue('');
  await expect(page.getByLabel('Light requirement', { exact: true })).toHaveValue('');
  await expect(page.getByLabel('Sort by', { exact: true })).toHaveValue('name-asc');
  await expect(names.first()).toHaveText('African violet');
  await expect(page.getByRole('button', { name: 'Clear filters', exact: true })).toBeDisabled();
  await auditAccessibility(page, 'Catalog filter controls and compact cards');
  await page.setViewportSize({ width: 320, height: 800 });
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true);
  // Unsupported URL values fall back to the complete, English A–Z catalog.
  await page.goto('/plants?light=invalid&sort=invalid');
  await expect(names.first()).toHaveText('African violet');
  await expect(page.getByLabel('Light requirement', { exact: true })).toHaveValue('');
});

test('flowering additions have identifiable photos, attributed care and seasonal guidance', async ({ page }) => {
  for (const [scientific, common, toxic] of [
    ['streptocarpus-ionanthus', 'African violet', false],
    ['anthurium-andraeanum', 'Flamingo flower', true],
    ['kalanchoe-blossfeldiana', 'Florist kalanchoe', true],
    ['schlumbergera-truncata', 'Thanksgiving cactus', false],
    ['cyclamen-persicum', "Florist's cyclamen", true],
  ] as const) {
    await page.goto('/plants?q=' + encodeURIComponent(common));
    const card = page.locator('.plant-card');
    await expect(card).toHaveCount(1);
    await expect(card.getByRole('heading', { name: common, exact: true })).toBeVisible();
    const photo = card.getByRole('img');
    await photo.scrollIntoViewIfNeeded();
    await expect.poll(() => photo.evaluate((img: HTMLImageElement) => img.naturalWidth)).toBeGreaterThan(0);
    await expect(card.locator('.warning-badge')).toHaveCount(toxic ? 1 : 0);
    await expect(card.locator('figcaption a').first()).toHaveAttribute('href', `https://plants.ces.ncsu.edu/plants/${scientific}/`);
    await card.getByRole('link', { name: 'View details' }).click();
    await expect(page.getByRole('heading', { level: 1, name: common, exact: true })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Care reference', exact: true })).toHaveAttribute('href', `https://plants.ces.ncsu.edu/plants/${scientific}/`);
    if (scientific === 'cyclamen-persicum') {
      await expect(page.getByText('Reduce water during summer dormancy', { exact: false })).toBeVisible();
    }
    if (scientific === 'schlumbergera-truncata') {
      await expect(page.getByText('Long uninterrupted autumn nights', { exact: false })).toBeVisible();
    }
  }
  await auditAccessibility(page, 'Flowering plant care detail');
});

test('growth references show selected species features with working photos, sources and mobile reflow', async ({ page, request }) => {
  const species = await (await request.get('/api/plant-species')).json();
  for (const name of ['Swiss cheese plant', 'Spider plant', 'Golden pothos', 'Aloe vera']) {
    const plant = species.find((item: { commonName: string }) => item.commonName === name);
    expect(plant.catalogProfile.growthReferences).toHaveLength(2);
    await page.goto('/plants/' + plant.id);
    const section = page.getByRole('region', { name: 'Growth references', exact: true });
    await section.scrollIntoViewIfNeeded();
    await expect(section).toBeVisible();
    await expect(section.getByText('These photos show different specimens', { exact: false })).toBeVisible();
    for (const reference of plant.catalogProfile.growthReferences) {
      await expect(section.getByRole('heading', { name: reference.title, exact: true })).toBeVisible();
      const photo = section.getByRole('img', { name: reference.photo.altText, exact: true });
      await photo.scrollIntoViewIfNeeded();
      await expect.poll(() => photo.evaluate((image: HTMLImageElement) => image.naturalWidth)).toBeGreaterThan(0);
      const article = photo.locator('xpath=ancestor::article');
      await expect(article.getByRole('link', { name: reference.photo.credit, exact: true })).toHaveAttribute('href', reference.photo.sourceUrl);
      await expect(article.getByRole('link', { name: reference.photo.licenseName, exact: true })).toHaveAttribute('href', reference.photo.licenseUrl);
    }
    const source = section.getByRole('link', { name: 'Growth reference source', exact: true });
    await expect(source).toHaveAttribute('href', plant.catalogProfile.careSourceUrl);
    await source.focus(); await expect(source).toBeFocused();
    await page.setViewportSize({ width: 320, height: 800 });
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
    await auditAccessibility(page, 'Growth references at narrow width');
  }
  const zz = species.find((item: { commonName: string }) => item.commonName === 'ZZ plant');
  await page.goto('/plants/' + zz.id);
  await expect(page.getByRole('heading', { level: 1, name: 'ZZ plant', exact: true })).toBeVisible();
  await expect(page.getByRole('region', { name: 'Growth references', exact: true })).toHaveCount(0);
});

test('familiar aliases tolerate punctuation, preserve navigation and keep ambiguous species separate', async ({ page }) => {
  for (const [query, common] of [
    ['Devils ivy', 'Golden pothos'], ['DEVIL’S IVY', 'Golden pothos'],
    ['mother in laws tongue', 'Snake plant'], ['Zanzibar gem', 'ZZ plant'],
    ['Indian rubber plant', 'Rubber plant'], ['Medicinal aloe', 'Aloe vera'],
  ]) {
    await page.goto('/plants?q=' + encodeURIComponent(query));
    await expect(page.locator('.plant-card h2')).toHaveText([common]);
  }
  await page.goto('/plants?q=Devils%20ivy&light=BrightIndirectLight');
  await page.locator('.plant-card').getByRole('link', { name: 'View details' }).click();
  await expect(page.getByRole('heading', { level: 1, name: 'Golden pothos', exact: true })).toBeVisible();
  await expect(page.locator('.common-name-aliases')).toHaveText("Also known as: Devil's ivy, Pothos.");
  await expect(page.getByText('Common names can refer to different plants.', { exact: false })).toBeVisible();
  await page.getByRole('link', { name: 'Back to plant catalog', exact: false }).click();
  await expect(page.getByRole('searchbox', { name: 'Find a plant' })).toHaveValue('Devils ivy');
  await expect(page.getByLabel('Light requirement', { exact: true })).toHaveValue('BrightIndirectLight');
  await page.reload();
  await expect(page.locator('.plant-card h2')).toHaveText(['Golden pothos']);
  await page.goto('/plants?q=Ribbon%20plant');
  await expect(page.locator('.plant-card h2')).toHaveText(['Corn plant', 'Spider plant']);
  await expect(page.locator('.plant-card .scientific-name')).toHaveText(['Dracaena fragrans', 'Chlorophytum comosum']);
  await page.goto('/plants?q=Money%20plant');
  await expect(page.locator('.plant-card h2')).toHaveText(['Chinese money plant', 'Jade plant']);
  await page.setViewportSize({ width: 320, height: 800 });
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await auditAccessibility(page, 'Alternative-name search results');
});

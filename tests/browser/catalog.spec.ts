import { expect, test } from '@playwright/test';
import { auditAccessibility } from './accessibility';

test('English houseplants have working local photos, attribution and searchable care details', async ({ page, request }) => {
  const response = await request.get('/api/plant-species');
  expect(response.ok()).toBe(true);
  const species = await response.json();
  const reviewed = species.filter((plant: { catalogProfile?: { photo?: unknown } }) => plant.catalogProfile?.photo);
  expect(reviewed.length).toBeGreaterThanOrEqual(15);
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
  await expect(names.first()).toHaveText('Aloe vera');
  await expect(page.getByRole('button', { name: 'Clear filters', exact: true })).toBeDisabled();
  await auditAccessibility(page, 'Catalog filter controls and compact cards');
  await page.setViewportSize({ width: 320, height: 800 });
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true);
  // Unsupported URL values fall back to the complete, English A–Z catalog.
  await page.goto('/plants?light=invalid&sort=invalid');
  await expect(names.first()).toHaveText('Aloe vera');
  await expect(page.getByLabel('Light requirement', { exact: true })).toHaveValue('');
});

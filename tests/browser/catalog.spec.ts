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
  await expect(page.getByText('No plants match your search. Try another name.')).toBeVisible();
});

import { expect, test } from '@playwright/test';
import { auditAccessibility } from './accessibility';

test('garden catalog categories combine with search and preserve navigation and comparison', async ({ page }) => {
  const categoryLabels: Record<string,string> = { '': 'All plants', houseplant: 'Houseplants', fruit: 'Fruit', vegetable: 'Vegetables', 'root-tuber': 'Roots & tubers', herb: 'Herbs', tree: 'Trees' };
  const categoryLink = (value: string) => page.getByRole('navigation', { name: 'Catalog categories' }).getByRole('link', { name: categoryLabels[value], exact: true });
  await page.goto('/plants?category=tree&kind=general');
  await expect(categoryLink('tree')).toHaveAttribute('aria-current', 'page');
  await expect(page.locator('.plant-card h2')).toHaveText(['Apple tree', 'Fig tree', 'Peach tree', 'Pear tree', 'Plum tree', 'Sour cherry tree', 'Sweet cherry tree']);
  await page.getByLabel('Compare Apple tree', { exact: true }).check();
  await page.getByLabel('Compare Fig tree', { exact: true }).check();
  await page.getByRole('link', { name: 'Compare selected plants', exact: true }).click();
  await expect(page.locator('.comparison-card h2')).toHaveText(['Apple tree', 'Fig tree']);
  await page.getByRole('link', { name: 'Back to plant catalog', exact: false }).click();
  await expect(categoryLink('tree')).toHaveAttribute('aria-current', 'page');
  await expect(page.getByLabel('Entry type', { exact: true })).toHaveValue('general');
  await page.getByLabel('Entry type', { exact: true }).selectOption('');
  for (const [category, count] of [['houseplant', 107], ['fruit', 49], ['vegetable', 72], ['root-tuber', 24], ['herb', 16]] as const) {
    await categoryLink(category).click();
    await expect(page.locator('.plant-card')).toHaveCount(count);
  }
  await page.getByLabel('Entry type', { exact: true }).selectOption('cultivar');
  await categoryLink('houseplant').click();
  await expect(page.locator('.plant-card')).toHaveCount(2);
  await categoryLink('fruit').click();
  await expect(page.locator('.plant-card')).toHaveCount(38);
  await categoryLink('root-tuber').click();
  await expect(page.locator('.plant-card')).toHaveCount(20);
  await page.getByLabel('Entry type', { exact: true }).selectOption('general');
  await categoryLink('fruit').click();
  await page.getByRole('searchbox', { name: 'Find a plant', exact: true }).fill('Fig tree');
  await page.getByLabel('Light requirement', { exact: true }).selectOption('FullSun');
  await page.getByLabel('Cats and dogs', { exact: true }).selectOption('unknown');
  await expect(page.locator('.plant-card')).toHaveCount(0);
  await page.getByLabel('Cats and dogs', { exact: true }).selectOption('warning');
  await page.locator('.plant-card').getByRole('link', { name: 'View details', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Growing and harvest', exact: true })).toBeVisible();
  await page.getByRole('link', { name: 'Back to plant catalog', exact: false }).click();
  await page.reload();
  await expect(categoryLink('fruit')).toHaveAttribute('aria-current', 'page');
  await expect(page.locator('.plant-card h2')).toHaveText(['Fig tree']);
  await page.setViewportSize({ width: 320, height: 900 });
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await auditAccessibility(page, 'Garden category filters');
  await page.getByRole('button', { name: 'Clear filters', exact: true }).click();
  await expect(categoryLink('')).toHaveAttribute('aria-current', 'page');
  await expect(page.getByRole('link', { name: 'Compare selected plants', exact: true })).toBeVisible();
});

test('garden catalog crop profiles distinguish cultivars and show sourced harvest guidance', async ({ page, request }) => {
  const species = await (await request.get('/api/plant-species')).json();
  const garden = species.filter((p: any) => p.catalogProfile?.harvestInstructions);
  expect(garden).toHaveLength(137);
  expect(garden.filter((p: any) => p.scientificName.startsWith('Solanum lycopersicum'))).toHaveLength(3);
  expect(garden.filter((p: any) => p.scientificName.startsWith('Capsicum annuum'))).toHaveLength(4);
  for (const plant of garden) {
    expect(plant.catalogProfile.categories.length).toBeGreaterThan(0);
    expect(plant.catalogProfile.harvestInstructions.length).toBeGreaterThan(0);
    expect(plant.defaultFertilizingIntervalDays).toBeNull();
  }
  expect(species.filter((p: any) => p.catalogProfile?.cultivarName)).toHaveLength(102);
  // Exercise this batch and representative regressions; API assertions above cover every profile.
  for (const name of ["Cherry tomato (Tiny Tim)", "Potato", "Grape vine (Pinot Noir)", "Lettuce (Lollo Rossa)", "Zucchini (Defender)", "Zucchini (Parador)", "Zucchini (Astia)", "Redcurrant (Rovada)", "Blackcurrant (Ben Sarek)"]) {
    const plant = species.find((p: any) => p.commonName === name);
    expect(plant).toBeTruthy();
    await page.goto('/plants/' + plant.id + '?category=' + plant.catalogProfile.categories[0]);
    await expect(page.getByRole('heading', { level: 1 })).toHaveText(name);
    await expect(page.getByRole('heading', { name: plant.catalogProfile.harvestInstructions ? 'Growing and harvest' : 'Growing guide', exact: true })).toBeVisible();
    if (plant.catalogProfile.harvestInstructions) await expect(page.getByRole('heading', { name: 'Harvest guidance', exact: true })).toBeVisible();
    expect(plant.catalogProfile.photoIsSpeciesReference ?? false).toBe(false);
    await expect(page.getByRole('link', { name: 'Growing guide source', exact: true })).toHaveAttribute('href', plant.catalogProfile.careSourceUrl);
    const primary = page.locator('.plant-detail-page > app-plant-photo');
    if (plant.catalogProfile.photo) {
      const image = primary.locator('img'); await image.scrollIntoViewIfNeeded();
      await expect.poll(() => image.evaluate((i: HTMLImageElement) => i.complete && i.naturalWidth > 0)).toBe(true);
    } else {
      await expect(primary).toContainText('Cultivar photo pending');
      await expect(primary.locator('img')).toHaveCount(0);
    }
    if (name === 'Potato') await expect(page.getByText('never eat the berries or foliage', { exact: false })).toBeVisible();
  }
  await auditAccessibility(page, 'Garden growing and harvest guide');
});

test('English houseplants have working local photos, attribution and searchable care details', async ({ page, request }) => {
  const response = await request.get('/api/plant-species');
  expect(response.ok()).toBe(true);
  const species = await response.json();
  const reviewed = species.filter((plant: { catalogProfile?: unknown }) => plant.catalogProfile);
  expect(reviewed).toHaveLength(244);
  expect(reviewed.filter((plant: { commonName: string }) => plant.commonName === 'African mask plant'))
    .toHaveLength(1);
  for (const plant of reviewed) {
    const photo = plant.catalogProfile.photo;
    if (!photo) { expect(plant.catalogProfile.cultivarName).toBeTruthy(); continue; }
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

test('catalog light filters combine with search, survive detail navigation and reset together', async ({ page, request }) => {
  const firstName = (await (await request.get('/api/plant-species')).json())
    .sort((a: {commonName:string}, b: {commonName:string}) => a.commonName.localeCompare(b.commonName, 'en', {sensitivity:'base'}))[0].commonName;
  await page.goto('/plants?light=PartialSun&sort=name-desc');
  const cards = page.locator('.plant-card');
  const names = cards.getByRole('heading', { level: 2 });
  await expect(names).toHaveText(['Purple shamrock', 'Purple heart', 'Jade plant', 'Aloe vera']);
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
  await expect(names.first()).toHaveText(firstName);
  await expect(page.getByRole('button', { name: 'Clear filters', exact: true })).toBeDisabled();
  await auditAccessibility(page, 'Catalog filter controls and compact cards');
  await page.setViewportSize({ width: 320, height: 800 });
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true);
  // Unsupported URL values fall back to the complete, English A–Z catalog.
  await page.goto('/plants?light=invalid&sort=invalid');
  await expect(names.first()).toHaveText(firstName);
  await expect(page.getByLabel('Light requirement', { exact: true })).toHaveValue('');
});

test('sourced cat-and-dog filter excludes unknown plants and preserves combined filters through details and reload', async ({ page, request }) => {
  const species = await (await request.get('/api/plant-species')).json();
  const expected = species.filter((p: any) => !p.isToxicToPets && p.catalogProfile?.nonToxicToCatsAndDogsSourceUrl)
    .map((p: any) => p.commonName).sort((a: string, b: string) => a.localeCompare(b, 'en'));
  expect(expected).toEqual(['African violet', 'Baby rubber plant', 'Boston fern', 'Cast iron plant', 'Kentia palm', 'Norfolk Island pine', 'Parlor palm', 'Ponytail palm', 'Rattlesnake plant', 'Spider plant', 'Watermelon peperomia', 'Wax plant', 'Zebra haworthia']);
  await page.goto('/plants?pets=non-toxic');
  const cards = page.locator('.plant-card');
  const names = cards.getByRole('heading', { level: 2 });
  await expect(names).toHaveText(expected);
  await expect(page.getByLabel('Cats and dogs', { exact: true })).toHaveValue('non-toxic');
  await expect(page.getByText('An absent warning is not a safety assessment.', { exact: false })).toBeVisible();
  await page.getByRole('searchbox', { name: 'Find a plant' }).fill('Parlour palm');
  await page.getByLabel('Light requirement', { exact: true }).selectOption('BrightIndirectLight');
  await page.getByLabel('Sort by', { exact: true }).selectOption('name-desc');
  await expect(names).toHaveText(['Parlor palm']);
  await cards.getByRole('link', { name: 'View details' }).click();
  await expect(page.getByRole('heading', { level: 1 })).toHaveText('Parlor palm');
  await expect(page.getByRole('link', { name: 'Pet toxicity reference', exact: true })).toHaveAttribute('href', /aspca.org\/.*bamboo-palm$/);
  await page.getByRole('link', { name: 'Back to plant catalog', exact: false }).click();
  await page.reload();
  await expect(names).toHaveText(['Parlor palm']);
  await expect(page.getByLabel('Cats and dogs', { exact: true })).toHaveValue('non-toxic');
  await expect(page.getByLabel('Light requirement', { exact: true })).toHaveValue('BrightIndirectLight');
  await expect(page.getByLabel('Sort by', { exact: true })).toHaveValue('name-desc');
  await page.getByLabel('Cats and dogs', { exact: true }).selectOption('warning');
  await expect(cards).toHaveCount(0);
  await page.getByRole('button', { name: 'Clear filters', exact: true }).click();
  await expect(page).toHaveURL(/\/plants$/);
  await expect(page.getByLabel('Cats and dogs', { exact: true })).toHaveValue('');
  await page.getByLabel('Cats and dogs', { exact: true }).selectOption('warning');
  await expect(names).toHaveText(species.filter((p: any) => p.isToxicToPets).map((p: any) => p.commonName)
    .sort((a: string, b: string) => a.localeCompare(b, 'en')));
  await page.goto('/plants?pets=invalid');
  await expect(cards).toHaveCount(species.length);
  await expect(page.getByLabel('Cats and dogs', { exact: true })).toHaveValue('');
  await page.setViewportSize({ width: 320, height: 800 });
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await auditAccessibility(page, 'Cat-and-dog catalog filter at narrow width');
  for (const common of ['Baby rubber plant', 'Cast iron plant', 'Parlor palm']) {
    await page.goto('/plants?q=' + encodeURIComponent(common));
    await expect(cards).toHaveCount(1);
    const image = cards.getByRole('img');
    await image.scrollIntoViewIfNeeded();
    await expect.poll(() => image.evaluate((img: HTMLImageElement) => img.naturalWidth)).toBeGreaterThan(0);
    await expect(cards.locator('figcaption a')).toHaveCount(2);
    await cards.getByRole('link', { name: 'View details' }).click();
    await expect(page.getByRole('heading', { level: 1 })).toHaveText(common);
    await expect(page.getByRole('link', { name: 'Pet toxicity reference', exact: true })).toBeVisible();
    if (common === 'Baby rubber plant')
      await expect(page.getByText('different from the much larger rubber plant', { exact: false })).toBeVisible();
  }
  await auditAccessibility(page, 'New houseplant detail with sourced classification');
});

test('large houseplant batch has distinct common-name pages, source credits and working reference photos', async ({ page, request }) => {
  const all = await (await request.get('/api/plant-species')).json();
  for (const name of ['Fiddle-leaf fig', 'Chinese banyan', 'Madagascar dragon tree', 'Dwarf umbrella tree',
    'Money tree', 'Spineless yucca', 'Boston fern', "Bird's nest fern", 'Prayer plant', 'Round-leaf calathea',
    'Rattlesnake plant', 'Dieffenbachia', 'Chinese evergreen', 'Arrowhead plant', 'Inch plant', 'Wax plant',
    'String of hearts', 'Ponytail palm', 'Nerve plant', 'Heartleaf philodendron',
    'Swiss cheese vine', 'Satin pothos', 'Mini monstera', 'African mask plant', 'Rex begonia',
    'Asparagus fern', 'Kentia palm', 'Lady palm', 'Coffee plant', 'Clivia', 'String of pearls',
    'Mexican snowball', 'Zebra haworthia', 'Watermelon peperomia', 'Panda plant',
    'Polka dot plant', 'Purple passion plant', 'Scarlet star', 'Japanese aralia',
    'Norfolk Island pine', 'Croton', 'English ivy', 'Umbrella sedge', 'Lipstick plant', 'Goldfish plant',
    'Iron cross begonia', 'Peacock plant', 'Rose-painted calathea', 'Stromanthe', 'Pinstripe plant',
    'Blushing philodendron', 'Purple shamrock', 'Persian violet', 'Creeping fig', 'Gardenia',
    'Air plant', 'Staghorn fern', 'Mistletoe cactus', 'Aluminum plant', 'Purple heart',
    'Ripple peperomia', 'Friendship plant', "Baby's tears", 'Maidenhair fern', 'African milk tree',
    'Strawberry begonia', 'Lucky bamboo', 'Gold dust dracaena',
    'Crown of thorns', 'Desert rose', 'Silver jade', 'Sweetheart hoya', 'Jewel orchid',
    'Dragon-tail plant', 'Tree philodendron', 'Ti plant', 'Sago palm', 'Grape ivy', "Rabbit's foot fern", 'Flame violet', 'False African violet', 'Kangaroo vine']) {
    const plant = all.find((p: { commonName: string }) => p.commonName === name);
    expect(plant, `New catalog plant: ${name}`).toBeTruthy();
    await page.goto('/plants/' + plant.id);
    await expect(page.getByRole('heading', { level: 1 })).toHaveText(name);
    await expect(page.getByRole('link', { name: 'Care reference', exact: true })).toHaveAttribute('href', plant.catalogProfile.careSourceUrl);
    const photo = page.locator('app-plant-photo').first().getByRole('img');
    await photo.scrollIntoViewIfNeeded();
    await expect.poll(() => photo.evaluate((img: HTMLImageElement) => img.naturalWidth)).toBeGreaterThan(0);
    await expect(page.locator('.care-grid')).toContainText(plant.wateringInstructions);
    await expect(page.locator('app-plant-photo').first().getByRole('link', { name: plant.catalogProfile.photo.licenseName, exact: true })).toHaveAttribute('href', plant.catalogProfile.photo.licenseUrl);
    if (plant.catalogProfile.nonToxicToCatsAndDogsSourceUrl)
      await expect(page.getByRole('link', { name: 'Pet toxicity reference', exact: true })).toHaveAttribute('href', plant.catalogProfile.nonToxicToCatsAndDogsSourceUrl);
  }
  await page.setViewportSize({ width: 320, height: 800 });
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await auditAccessibility(page, 'Expanded houseplant catalog detail');
});

test('comparison selection survives filters, details and reload, limits three plants and shows sourced care', async ({ page }) => {
  await page.goto('/plants');
  for (const name of ['Spider plant', 'Snake plant', 'ZZ plant'])
    await page.getByRole('checkbox', { name: 'Compare ' + name, exact: true }).check();
  await expect(page.getByRole('checkbox', { name: 'Compare Golden pothos', exact: true })).toBeDisabled();
  await page.getByRole('searchbox', { name: 'Find a plant' }).fill('Spider plant');
  await expect(page.locator('.plant-card')).toHaveCount(1);
  await expect(page.getByRole('region', { name: 'Plant comparison selection' })).toContainText('3 of 3 plants selected');
  await page.locator('.plant-card').getByRole('link', { name: 'View details', exact: true }).click();
  await page.getByRole('link', { name: 'Back to plant catalog', exact: false }).click();
  await expect(page.getByRole('searchbox', { name: 'Find a plant' })).toHaveValue('Spider plant');
  await page.getByRole('link', { name: 'Compare selected plants', exact: true }).click();
  await expect(page.getByRole('heading', { level: 1 })).toHaveText('Compare plants');
  await expect(page.locator('.comparison-card h2')).toHaveText(['Spider plant', 'Snake plant', 'ZZ plant']);
  await expect(page.locator('.comparison-card').first()).toContainText('Reviewed as non-toxic to cats and dogs');
  await expect(page.locator('.comparison-card').nth(1)).toContainText('Toxicity warning');
  await expect(page.locator('.comparison-card').first().getByRole('link', { name: 'Care reference', exact: true })).toHaveAttribute('href', 'https://plants.ces.ncsu.edu/plants/chlorophytum-comosum/');
  await page.reload();await expect(page.locator('.comparison-card')).toHaveCount(3);
  await page.getByRole('button', { name: 'Remove ZZ plant from comparison', exact: true }).click();
  await expect(page.locator('.comparison-card')).toHaveCount(2);
  await page.setViewportSize({width:320,height:800});
  expect(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth)).toBe(true);
  await auditAccessibility(page,'Plant comparison at narrow width');
  await page.getByRole('link', { name: 'Back to plant catalog', exact: false }).click();
  await page.getByRole('button', { name: 'Clear filters', exact: true }).click();
  await expect(page.getByRole('checkbox', { name: 'Compare Spider plant', exact: true })).toBeChecked();
  await expect(page.getByRole('checkbox', { name: 'Compare ZZ plant', exact: true })).not.toBeChecked();
  await page.getByRole('button', { name: 'Clear comparison', exact: true }).click();
  await expect(page.getByRole('link', { name: 'Compare selected plants', exact: true })).toHaveCount(0);
});

test.describe('comparison network failure isolation', () => {
// Exercise an injected network failure directly. A controlling service worker
// can bypass page.route (particularly in Firefox); real-worker behavior has its
// own PWA suite and remains enabled in all other catalog/comparison checks.
test.use({ serviceWorkers: 'block' });
test('comparison handles malformed or missing choices, unknown assessment and retryable API failure', async ({ page, request }) => {
  const species=await (await request.get('/api/plant-species')).json();
  const unknown=species.find((p: {commonName:string})=>p.commonName==='Lady palm');
  const fake='00000000-0000-0000-0000-000000000000';
  await page.goto('/plants/compare?compare='+encodeURIComponent(`bad,${unknown.id},${unknown.id},${fake}`));
  await expect(page.locator('.comparison-card')).toHaveCount(1);
  await expect(page.getByText('Some selected plants are no longer available.',{exact:false})).toBeVisible();
  await expect(page.getByText('Not assessed for this comparison',{exact:true})).toBeVisible();
  await page.getByRole('button',{name:'Remove Lady palm from comparison',exact:true}).click();
  await expect(page.getByText('Choose at least two plants',{exact:false})).toBeVisible();
  await page.route('**/api/plant-species',route=>route.fulfill({status:503,body:'Unavailable'}),{times:1});
  await page.goto('/plants/compare?compare='+unknown.id);
  await expect(page.getByRole('alert')).toContainText('could not be loaded');
  await page.getByRole('button',{name:'Try again',exact:true}).click();
  await expect(page.locator('.comparison-card')).toHaveCount(1);
  await expect(page.getByRole('heading',{level:1})).toHaveText('Compare plants');
});
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
    const card = page.locator('.plant-card').filter({ has: page.getByRole('heading', { name: common, exact: true }) });
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

test('growth photo discovery combines filters and preserves comparison through details and reload', async ({ page, request }) => {
  const all = await (await request.get('/api/plant-species')).json();
  await page.goto('/plants?growth=available');
  const filter = page.getByLabel('Growth references', { exact: true });
  const names = page.locator('.plant-card h2');
  await expect(filter).toHaveValue('available');
  await expect(names).toHaveText(['African violet', 'Aloe vera', 'Boston fern', 'Golden pothos', 'Jade plant', 'Moth orchid',
    'Peace lily', 'Rubber plant', 'Spider plant', 'Swiss cheese plant', 'Thanksgiving cactus', 'Wax plant']);
  await expect(page.locator('.growth-badge')).toHaveCount(12);
  await page.getByLabel('Cats and dogs', { exact: true }).selectOption('non-toxic');
  await expect(names).toHaveText(['African violet', 'Boston fern', 'Spider plant', 'Wax plant']);
  for (const name of ['Spider plant', 'Wax plant'])
    await page.getByRole('checkbox', { name: 'Compare ' + name, exact: true }).check();
  await page.getByRole('searchbox', { name: 'Find a plant' }).fill('Spider');
  await page.getByLabel('Light requirement', { exact: true }).selectOption('MediumIndirectLight');
  await expect(names).toHaveText(['Spider plant']);
  await page.locator('.plant-card').getByRole('link', { name: 'View details', exact: true }).click();
  await expect(page.getByRole('region', { name: 'Growth references', exact: true })).toBeVisible();
  await page.getByRole('link', { name: 'Back to plant catalog', exact: false }).click();
  await page.reload();
  await expect(filter).toHaveValue('available');
  await expect(names).toHaveText(['Spider plant']);
  await page.getByRole('link', { name: 'Compare selected plants', exact: true }).click();
  await expect(page.locator('.comparison-card h2')).toHaveText(['Spider plant', 'Wax plant']);
  await page.getByRole('link', { name: 'Back to plant catalog', exact: false }).click();
  await expect(filter).toHaveValue('available');
  await expect(names).toHaveText(['Spider plant']);
  await page.getByRole('searchbox', { name: 'Find a plant' }).fill('ZZ plant');
  await expect(page.locator('.plant-card')).toHaveCount(0);
  await expect(page.getByText('No plants match your search. Try another name or clear the filters.')).toBeVisible();
  await page.getByRole('button', { name: 'Clear filters', exact: true }).click();
  await expect(filter).toHaveValue('');
  await expect(page.locator('.plant-card')).toHaveCount(all.length);
  await expect(page.getByRole('region', { name: 'Plant comparison selection' })).toContainText('2 of 3');
  await page.setViewportSize({ width: 320, height: 800 });
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await auditAccessibility(page, 'Growth reference discovery at narrow width');
  await page.goto('/plants?growth=invalid');
  await expect(filter).toHaveValue('');
  await expect(page.locator('.plant-card')).toHaveCount(all.length);
  await expect(page.getByRole('button', { name: 'Clear filters', exact: true })).toBeDisabled();
});

test('growth references show selected species features with working photos, sources and mobile reflow', async ({ page, request }) => {
  const species = await (await request.get('/api/plant-species')).json();
  for (const name of ['Swiss cheese plant', 'Spider plant', 'Golden pothos', 'Aloe vera',
    'Jade plant', 'Moth orchid', 'Peace lily', 'Wax plant', 'African violet', 'Thanksgiving cactus', 'Rubber plant', 'Boston fern']) {
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
  await expect(page.locator('.plant-card h2')).toHaveText(['Corn plant', 'Lucky bamboo', 'Spider plant']);
  await expect(page.locator('.plant-card .scientific-name')).toHaveText(['Dracaena fragrans', 'Dracaena sanderiana', 'Chlorophytum comosum']);
  await page.goto('/plants?q=Money%20plant');
  await expect(page.locator('.plant-card h2')).toHaveText(['Chinese money plant', 'Jade plant', 'Money tree']);
  await page.setViewportSize({ width: 320, height: 800 });
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await auditAccessibility(page, 'Alternative-name search results');
});

test('unknown pet assessment stays distinct and combines with gallery, light and comparison navigation', async ({ page, request }) => {
  const species = await (await request.get('/api/plant-species')).json();
  const expected = species.filter((p: any) => !p.isToxicToPets && !p.catalogProfile?.nonToxicToCatsAndDogsSourceUrl)
    .map((p: any) => p.commonName).sort((a: string, b: string) => a.localeCompare(b, 'en'));
  expect(expected).toContain('Ripple peperomia');
  expect(expected).not.toContain('African violet');
  expect(expected).not.toContain('African milk tree');
  await page.goto('/plants?pets=unknown');
  const cards = page.locator('.plant-card');
  const filter = page.getByLabel('Cats and dogs', { exact: true });
  await expect(filter).toHaveValue('unknown');
  await expect(cards.locator('h2')).toHaveText(expected);
  await expect(cards.locator('.unknown-badge')).toHaveCount(expected.length);
  await expect(cards.locator('.warning-badge, .pet-badge')).toHaveCount(0);
  for (const name of ['Moth orchid', 'Thanksgiving cactus'])
    await page.getByRole('checkbox', { name: 'Compare ' + name, exact: true }).check();
  await page.getByLabel('Growth references', { exact: true }).selectOption('available');
  await expect(cards.locator('h2')).toHaveText(['Moth orchid', 'Thanksgiving cactus']);
  await page.getByLabel('Light requirement', { exact: true }).selectOption('BrightIndirectLight');
  await page.getByRole('searchbox', { name: 'Find a plant' }).fill('Phalaenopsis (houseplant hybrids)');
  await expect(cards.locator('h2')).toHaveText(['Moth orchid']);
  await cards.getByRole('link', { name: 'View details', exact: true }).click();
  await page.getByRole('link', { name: 'Back to plant catalog', exact: false }).click();
  await page.reload();
  await expect(filter).toHaveValue('unknown');
  await expect(cards.locator('h2')).toHaveText(['Moth orchid']);
  await page.getByRole('link', { name: 'Compare selected plants', exact: true }).click();
  await expect(page.locator('.comparison-card h2')).toHaveText(['Moth orchid', 'Thanksgiving cactus']);
  await expect(page.locator('.comparison-card').first()).toContainText('Not assessed for this comparison');
  await page.getByRole('link', { name: 'Back to plant catalog', exact: false }).click();
  await expect(filter).toHaveValue('unknown');
  await page.getByRole('button', { name: 'Clear filters', exact: true }).click();
  await expect(filter).toHaveValue('');
  await expect(cards).toHaveCount(species.length);
  await expect(page.getByRole('region', { name: 'Plant comparison selection' })).toContainText('2 of 3');
  await filter.selectOption('unknown');
  await page.setViewportSize({ width: 320, height: 800 });
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await auditAccessibility(page, 'Unknown pet assessment filter and labels');
});

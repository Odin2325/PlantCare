import { expect, test, type Page, type BrowserContext, type APIRequestContext } from '@playwright/test';

const origin = 'http://localhost:4173';
async function offline(page: Page, context: BrowserContext, request: APIRequestContext, value: boolean) {
  expect((await request.post(`${origin}/__test/network`, { data: value ? 'offline' : 'online' })).ok()).toBe(true);
  await page.bringToFront();
  await context.setOffline(value);
  if (!value) {
    await expect.poll(() => page.evaluate(() => navigator.onLine)).toBe(true);
    // Firefox offline emulation may omit online events after a cold reload.
    // Explicitly exercise the app's foreground reconciliation in every engine.
    await page.evaluate(() => document.dispatchEvent(new Event('visibilitychange')));
  }
}
test.beforeEach(async ({ request }) => {
  expect((await request.post(`${origin}/__test/version`, { data: 'A' })).ok()).toBe(true);
});

async function ready(page: Page) {
  await page.goto(`${origin}/login`);
  await expect(page.getByRole('heading', { name: 'Welcome back', exact: true })).toBeVisible();
  await expect.poll(() => page.evaluate(() => Boolean(navigator.serviceWorker.controller)), { timeout: 60_000 }).toBe(true);
  // Registration/claim alone does not mean the first document was served by
  // the worker. Open a controlled document before testing installed behavior.
  await page.reload();
  await expect(page.getByRole('heading', { name: 'Welcome back', exact: true })).toBeVisible();
  // Wait for the installed version, not merely the worker registration.
  await expect.poll(() => page.evaluate(async () => (await caches.keys()).some(key => key.includes('assets'))), { timeout: 30_000 }).toBe(true);
}

test('manifest icons, offline cold reload and uncached account API', async ({ page, context, request }) => {
  await ready(page);
  const manifest = await (await request.get(`${origin}/manifest.webmanifest`)).json();
  expect(manifest.display).toBe('standalone');
  expect(manifest.id).toBe('/');
  for (const size of [192, 512]) {
    const icon = manifest.icons.find((item: { sizes: string; type: string }) => item.sizes === `${size}x${size}` && item.type === 'image/png');
    expect(icon).toBeTruthy();
    const response = await request.get(`${origin}/${icon.src}`);
    const bytes = await response.body();
    expect(response.ok()).toBe(true);
    expect(bytes.subarray(1, 4).toString()).toBe('PNG');
    expect(bytes.readUInt32BE(16)).toBe(size); expect(bytes.readUInt32BE(20)).toBe(size);
  }
  await offline(page, context, request, true);
  await page.reload();
  await expect(page.locator('.offline-banner')).toContainText('Care changes need a connection', { timeout: 3000 });
  await expect(page.getByRole('heading', { name: 'Welcome back', exact: true })).toBeVisible();
  const status = await page.evaluate(async () => { try { return (await fetch('/api/account/me')).status; } catch { return 0; } });
  expect(status).not.toBe(200);
  const cachedApi = await page.evaluate(async () => {
    for (const name of await caches.keys()) for (const key of await (await caches.open(name)).keys()) {
      if (new URL(key.url).pathname.startsWith('/api/')) return true;
    }
    return false;
  });
  expect(cachedApi).toBe(false);
  await offline(page, context, request, false);
  await expect(page.locator('.offline-banner')).toBeHidden();
  await offline(page, context, request, true);
  const apiPage = await context.newPage();
  let navigationStatus = 0;
  try { navigationStatus = (await apiPage.goto(`${origin}/api/account/me`))?.status() ?? 0; }
  catch { /* An offline API navigation must fail rather than return cached HTML. */ }
  finally { await apiPage.close(); }
  expect(navigationStatus).not.toBe(200);
});

test('real worker stages an update and changes version only after consent', async ({ page, context, request }) => {
  await ready(page);
  await page.getByLabel('Email address', { exact: true }).fill('unfinished@example.test');
  expect((await request.post(`${origin}/__test/version`, { data: 'B' })).ok()).toBe(true);
  await offline(page, context, request, true);
  await expect(page.locator('.offline-banner')).toBeVisible();
  await offline(page, context, request, false);
  await expect(page.getByRole('button', { name: 'Update now', exact: true })).toBeVisible({ timeout: 30_000 });
  await expect(page.locator('meta[name="plantcare-pwa-probe"]')).toHaveAttribute('content', 'A');
  await expect(page.getByLabel('Email address', { exact: true })).toHaveValue('unfinished@example.test');
  await offline(page, context, request, true);
  await expect(page.getByRole('button', { name: 'Update now', exact: true })).toBeDisabled();
  await offline(page, context, request, false);
  await page.getByRole('button', { name: 'Update now', exact: true }).click();
  await expect(page.locator('meta[name="plantcare-pwa-probe"]')).toHaveAttribute('content', 'B');
  await expect(page.getByRole('heading', { name: 'Welcome back', exact: true })).toBeVisible();
  await page.reload();
  await expect(page.locator('meta[name="plantcare-pwa-probe"]')).toHaveAttribute('content', 'B');
});

test('a corrupt candidate preserves the open version and a corrected candidate recovers', async ({ page, context, request }) => {
  await page.addInitScript(() => {
    (window as Window & { pwaEvents: string[] }).pwaEvents = [];
    navigator.serviceWorker.addEventListener('message', event => {
      if (event.data?.type) (window as Window & { pwaEvents: string[] }).pwaEvents.push(event.data.type);
    });
  });
  await ready(page);
  expect((await request.post(`${origin}/__test/version`, { data: 'broken' })).ok()).toBe(true);
  await offline(page, context, request, true);
  await expect(page.locator('.offline-banner')).toBeVisible();
  await offline(page, context, request, false);
  await expect.poll(() => page.evaluate(() => (window as Window & { pwaEvents: string[] }).pwaEvents), { timeout: 30_000 })
    .toContain('VERSION_INSTALLATION_FAILED');
  await expect(page.getByRole('button', { name: 'Update now', exact: true })).toBeHidden();
  await expect(page.locator('meta[name="plantcare-pwa-probe"]')).toHaveAttribute('content', 'A');
  // Angular degrades to existing clients after a bad manifest. Correct the
  // deployment before requiring cold launches; do not promise offline recovery
  // from a version that could not be installed.
  expect((await request.post(`${origin}/__test/version`, { data: 'B' })).ok()).toBe(true);
  await offline(page, context, request, true);
  await expect(page.locator('.offline-banner')).toBeVisible();
  await offline(page, context, request, false);
  await expect(page.getByRole('button', { name: 'Update now', exact: true })).toBeVisible({ timeout: 30_000 });
  await page.getByRole('button', { name: 'Update now', exact: true }).click();
  await expect(page.locator('meta[name="plantcare-pwa-probe"]')).toHaveAttribute('content', 'B');
  await offline(page, context, request, true); await page.reload();
  await expect(page.locator('.offline-banner')).toBeVisible({ timeout: 3000 });
  await expect(page.locator('meta[name="plantcare-pwa-probe"]')).toHaveAttribute('content', 'B');
});

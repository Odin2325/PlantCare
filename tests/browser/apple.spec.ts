import { expect, test } from '@playwright/test';
import { auditAccessibility } from './accessibility';

test.use({ serviceWorkers: 'block' });

test('Apple install metadata, unsupported push and touch layout remain usable', async ({ page, request }, testInfo) => {
  const errors: string[] = [], writes: string[] = [];
  page.on('pageerror', error => errors.push(error.message));
  // Model an iOS browser tab with no Notification/PushManager APIs, regardless
  // of what this desktop WebKit build exposes. No real permissions or sends.
  await page.addInitScript(() => {
    Object.defineProperty(window, 'Notification', { configurable: true, value: undefined });
    Object.defineProperty(window, 'PushManager', { configurable: true, value: undefined });
  });
  await page.route('**/api/**', route => {
    const req = route.request(), path = new URL(req.url()).pathname;
    if (req.method() !== 'GET') { writes.push(path); return route.fulfill({ status: 405 }); }
    if (path === '/api/account/me') return route.fulfill({ json: { id: 'apple-fixture', email: 'apple@example.test' } });
    if (path === '/api/notifications') return route.fulfill({ json: [] });
    if (path === '/api/push/config') return route.fulfill({ json: { isEnabled: true, publicKey: 'fixture' } });
    if (path === '/api/notifications/preferences') return route.fulfill({ json: { inAppEnabled: true, pushEnabled: true,
      wateringEnabled: true, fertilizingEnabled: true, mistingEnabled: true, pruningEnabled: true,
      repottingEnabled: true, reminderLeadTimeHours: 24 } });
    return route.fulfill({ json: [] });
  });
  await page.goto('/notifications');
  await expect(page.locator('meta[name="viewport"]')).toHaveAttribute('content', /viewport-fit=cover/);
  const iconPath = await page.locator('link[rel="apple-touch-icon"]').getAttribute('href');
  const icon = await (await request.get('/' + iconPath)).body();
  expect(icon.subarray(1, 4).toString()).toBe('PNG');
  expect([icon.readUInt32BE(16), icon.readUInt32BE(20)]).toEqual([180, 180]);
  await expect(page.getByRole('heading', { name: 'You are all caught up' })).toBeVisible();
  await page.locator('.notification-settings > summary').click();
  await expect(page.getByRole('button', { name: 'Enable push reminders', exact: true })).toHaveCount(0);
  await page.locator('.apple-install-help > summary').click();
  await expect(page.locator('.apple-install-help')).toContainText('Add to Home Screen');
  await expect(page.locator('.apple-install-help')).toContainText('16.4');
  await auditAccessibility(page, 'Apple install help and notification fallback');

  const original = page.viewportSize()!;
  const sizes = testInfo.project.name === 'iphone-webkit'
    ? [original, { width: original.height, height: original.width }, { width: 844, height: 390 }] : [original];
  for (const size of sizes) {
    await page.setViewportSize(size);
    // Desktop navigation scrolls with the document; mobile/rail navigation is
    // fixed. Return to the header before comparing either layout's bounds.
    await page.evaluate(() => window.scrollTo(0, 0));
    await expect.poll(() => page.evaluate(() => window.scrollY)).toBe(0);
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1)).toBe(true);
    for (const button of await page.getByRole('navigation', { name: 'Main navigation' }).locator(':scope > a, :scope > details > summary').all()) {
      const box = (await button.boundingBox())!;
      expect(box.x).toBeGreaterThanOrEqual(0); expect(box.y).toBeGreaterThanOrEqual(0);
      expect(box.x + box.width).toBeLessThanOrEqual(size.width + 1);
      expect(box.y + box.height).toBeLessThanOrEqual(size.height + 1);
    }
    await page.locator('.app-more > summary').click();
    const menu = await page.locator('.more-panel').boundingBox();
    expect(menu!.y).toBeGreaterThanOrEqual(0);
    expect(menu!.y + menu!.height).toBeLessThanOrEqual(size.height + 1);
    if (process.env.APPLE_SCREENSHOTS) {
      await page.screenshot({ path: '/private/apple-' + testInfo.project.name + '-' + size.width + '.png' });
    }
    await page.locator('.app-more > summary').click();
  }
  if (testInfo.project.name !== 'mac-webkit') {
    const select = page.getByRole('combobox', { name: /^Remind me/ });
    expect(await select.evaluate(e => parseFloat(getComputedStyle(e).fontSize))).toBeGreaterThanOrEqual(16);
  }
  expect(writes).toEqual([]); expect(errors).toEqual([]);
});

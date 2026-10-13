import { expect, test, type Page } from '@playwright/test';
import { auditAccessibility } from './accessibility';

async function tabTo(page: Page, target: ReturnType<Page['getByRole']>, backwards = false) {
  for (let index = 0; index < 60; index++) {
    await page.keyboard.press(backwards ? 'Shift+Tab' : 'Tab');
    if (await target.evaluate(element => element === document.activeElement)) return;
  }
  throw new Error(`Keyboard target was not reachable within 60 Tab presses: ${target.toString()}`);
}

test('public pages support keyboard landmarks, route focus and accessible forms', async ({ page }) => {
  await page.addInitScript(() => {
    (window as Window & { policyViolations: string[] }).policyViolations = [];
    document.addEventListener('securitypolicyviolation', event => {
      (window as Window & { policyViolations: string[] }).policyViolations.push(event.violatedDirective);
    });
  });
  await page.setViewportSize({ width: 320, height: 800 });
  for (const path of ['/login', '/register', '/forgot-password', '/resend-confirmation', '/reset-password', '/plants']) {
    await page.goto(path);
    await expect(page.locator('main h1')).toBeVisible();
    await auditAccessibility(page, `Public page ${path}`);
    await expect(page.locator('main h1')).toBeFocused();
    const skip = page.getByRole('link', { name: 'Skip to main content', exact: true });
    await tabTo(page, skip, true);
    await expect(skip).toBeVisible();
    await page.keyboard.press('Enter');
    await expect(page.getByRole('main')).toBeFocused();
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth + 1)).toBe(true);
    expect(await page.evaluate(() => (window as Window & { policyViolations: string[] }).policyViolations)).toEqual([]);
  }
  await page.goto('/login');
  const register = page.getByRole('link', { name: 'Register', exact: true });
  await tabTo(page, register, true); await page.keyboard.press('Enter');
  await expect(page.getByRole('heading', { name: 'Create your account', exact: true })).toBeFocused();
  await page.goto('/login');
  await tabTo(page, page.getByRole('textbox', { name: 'Email address', exact: true }));
  expect(await page.evaluate(() => {
    const style = getComputedStyle(document.activeElement!);
    return style.outlineStyle !== 'none' && parseFloat(style.outlineWidth) >= 2;
  })).toBe(true);
  await page.keyboard.type('invalid'); await page.keyboard.press('Tab');
  const email = page.getByRole('textbox', { name: 'Email address', exact: true });
  await expect(email).toHaveAttribute('aria-invalid', 'true');
  await expect(email).toHaveAccessibleDescription('Enter a valid email address.');
  await auditAccessibility(page, 'Login inline validation');
});

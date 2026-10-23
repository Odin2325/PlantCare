import { randomUUID, randomBytes } from 'node:crypto';
import { expect, test, type Page } from '@playwright/test';
import { auditAccessibility } from './accessibility';

test('calendar sharing sends mail and opens only the invited account calendar', async ({ page, browser, request }) => {
  test.setTimeout(300_000);
  const origin = 'https://localhost:8443';
  const password = 'Pc9!' + randomBytes(20).toString('hex');
  const owner = `sharing-owner-${randomUUID()}@example.test`;
  const recipient = `sharing-recipient-${randomUUID()}@gmail.com`;
  // Mailpit is the isolated local SMTP sink; no Gmail messages leave this test.
  async function emailLink(address: string, subject: string) {
    let id: string | undefined;
    await expect.poll(async () => {
      const messages = await (await request.get('http://mailpit:8025/api/v1/search', { params: { query: `to:${address}` } })).json();
      id = messages.messages?.find((m: { Subject: string }) => m.Subject === subject)?.ID;
      return Boolean(id);
    }, { timeout: 20_000 }).toBe(true);
    const message = await (await request.get('http://mailpit:8025/api/v1/message/' + id)).json();
    const href = /href="([^"]+)"/.exec(message.HTML)?.[1]?.replaceAll('&amp;', '&');
    expect(Boolean(href), 'Captured message has a link').toBe(true);
    const link = new URL(href!); expect(link.origin === origin, 'Configured local mail origin').toBe(true);
    return link;
  }
  async function auth(p: Page, button: string, action: string) {
    for (let i = 0; i < 4; i++) {
      await expect(p.locator('form')).toHaveClass(/\bng-valid\b/);
      const pending = p.waitForResponse(r => new URL(r.url()).pathname === '/api/auth/' + action && r.request().method() === 'POST');
      await p.getByRole('button', { name: button, exact: true }).click();
      const response = await pending;
      if (response.status() !== 429) { expect(response.status()).toBe(200); return; }
      await p.waitForTimeout(20_000);
    }
    throw new Error('Authentication remains rate limited.');
  }
  async function register(p: Page, address: string, keepLocation = false) {
    if (!keepLocation) await p.goto(origin + '/register');
    await expect(p.getByRole('heading', { name: 'Create your account', exact: true })).toBeVisible();
    await p.getByLabel('Email address', { exact: true }).fill(address);
    await p.getByLabel('Password', { exact: true }).fill(password);
    await p.getByLabel('Confirm password', { exact: true }).fill(password);
    await auth(p, 'Create account', 'register');
    await expect.poll(() => new URL(p.url()).pathname === '/login').toBe(true);
    const returnLogin = p.url();
    const link = await emailLink(address, 'Confirm your PlantCare account');
    let confirmed = false;
    for (let i = 0; i < 4; i++) {
      let response;
      try { response = await p.goto(link.href); }
      catch { throw new Error('Confirmation navigation failed; secret URL withheld.'); }
      if (response?.status() === 429) { await p.waitForTimeout(20_000); continue; }
      expect(response?.status(), 'Confirmation response').toBe(200); confirmed = true; break;
    }
    expect(confirmed, 'Email confirmation completed within production rate limits').toBe(true);
    await p.goto(returnLogin);
  }
  const other = await browser.newContext({ ignoreHTTPSErrors: true });
  const invited = await other.newPage();
  try {
    await register(page, owner);
    await page.goto(origin + '/login');
    await page.getByLabel('Email address', { exact: true }).fill(owner);
    await page.getByLabel('Password', { exact: true }).fill(password);
    await auth(page, 'Log in', 'login');
    await page.goto(origin + '/calendar');
    await page.getByLabel('Recipient email', { exact: true }).fill(recipient);
    await page.getByRole('button', { name: 'Share calendar', exact: true }).click();
    await expect(page.getByText('Invitation emailed.', { exact: false })).toBeVisible();
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), 'Sharing controls fit the viewport').toBe(true);
    const link = await emailLink(recipient, 'A PlantCare calendar was shared with you');
    expect(link.pathname).toBe('/calendar'); expect(link.searchParams.has('invitation')).toBe(true);
    await invited.goto(link.href);
    await expect.poll(() => new URL(invited.url()).pathname === '/login').toBe(true);
    await invited.getByRole('link', { name: 'Create an account', exact: true }).click();
    await register(invited, recipient, true);
    await invited.getByLabel('Email address', { exact: true }).fill(recipient);
    await invited.getByLabel('Password', { exact: true }).fill(password);
    await auth(invited, 'Log in', 'login');
    await expect(invited.getByText('You are viewing a read-only shared calendar.')).toBeVisible();
    expect(await invited.evaluate(() => document.documentElement.scrollWidth <= innerWidth), 'Read-only calendar fits the viewport').toBe(true);
    await auditAccessibility(invited, 'Invited read-only calendar');
    await expect.poll(() => new URL(invited.url()).searchParams.has('share')).toBe(true);
    await page.reload();
    await page.getByRole('button', { name: 'Send sharing email to ' + recipient, exact: true }).click();
    await expect(page.getByText('The invitation email was sent.', { exact: false })).toBeVisible();
    await page.getByRole('button', { name: 'Revoke', exact: true }).click();
    await expect(page.getByText('Shared access revoked.')).toBeVisible();
    await invited.reload();
    await expect(invited.getByText('This shared calendar is unavailable.', { exact: false })).toBeVisible();
    await expect(invited.getByText('You are viewing a read-only shared calendar.')).toHaveCount(0);
  } finally { await other.close(); }
});

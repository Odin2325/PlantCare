import { readFileSync, writeFileSync } from 'node:fs';
import { randomUUID } from 'node:crypto';
import { expect, test, type Page } from '@playwright/test';
import { auditAccessibility } from './accessibility';
const origin = 'https://staging.plantacare.org';
const phase = process.env.STAGING_PHASE ?? 'public';
const privatePath = '/private/accounts.json';
function stage(name: string) {
  test.info().annotations.push({ type: 'stage', description: name });
  writeFileSync('/private/active-stage.json', JSON.stringify({ project: test.info().project.name, stage: name }));
}
type Account = { email: string; password: string; confirmed: boolean; created?: boolean; resetLink?: string };
type Accounts = { a: Account; b: Account; nextPassword: string; approvedRecipients: string[] };
function accounts(): Accounts {
  const value = JSON.parse(readFileSync(privatePath, 'utf8')) as Accounts;
  if (value.approvedRecipients?.length !== 2 || value.a.email === value.b.email || !value.approvedRecipients.includes(value.a.email) || !value.approvedRecipients.includes(value.b.email)) throw new Error('Unapproved staging recipient configuration.');
  return value;
}
async function submit(page: Page, button: string, endpoint: string, status = 200) {
  for (let attempt = 0; attempt < 4; attempt++) {
    const pending = page.waitForResponse(r => new URL(r.url()).pathname === `/api/auth/${endpoint}` && r.request().method() === 'POST');
    await page.getByRole('button', { name: button, exact: true }).click();
    const response = await pending;
    if (response.status() !== 429 || attempt === 3) { expect(response.status(), `Browser ${endpoint} status`).toBe(status); return; }
    await page.waitForTimeout(20_000);
  }
}
async function login(page: Page, account: Account) {
  await page.goto('/login');
  await page.getByLabel('Email address', { exact: true }).fill(account.email);
  await page.getByLabel('Password', { exact: true }).fill(account.password);
  await submit(page, 'Log in', 'login');
  await expect(page.getByRole('heading', { name: 'Your plants', exact: true })).toBeVisible();
}

test('register the two explicitly approved staging accounts', async ({ page }) => {
  test.skip(phase !== 'register');
  const data = accounts();
  for (const account of [data.a, data.b]) {
    if (account.created) continue;
    await page.goto('/register');
    await page.getByLabel('Email address', { exact: true }).fill(account.email);
    await page.getByLabel('Password', { exact: true }).fill(account.password);
    await page.getByLabel('Confirm password', { exact: true }).fill(account.password);
    await submit(page, 'Create account', 'register');
    await expect(page.getByText('Your account was created.', { exact: false })).toBeVisible();
    account.created = true;
    writeFileSync(privatePath, JSON.stringify(data));
  }
});

test('public staging core journey, sampled accessibility and two-account isolation', async ({ page, context, browser }) => {
  test.skip(phase !== 'journey');
  const data = accounts();
  if (!data.a.confirmed || !data.b.confirmed) throw new Error('Both approved test accounts must be confirmed first.');
  const nickname = `Staging browser probe ${randomUUID().slice(0, 8)}`;
  const errors: string[] = [];
  page.on('pageerror', error => errors.push(error.name));
  let plantId: string | undefined;
  let cleanup: Awaited<ReturnType<typeof browser.newContext>> | undefined;
  try {
    stage('login');
    await page.goto('/my-plants');
    await expect(page).toHaveURL(/\/login\?returnUrl=/);
    await login(page, data.a);
    cleanup = await browser.newContext({ baseURL: origin, ignoreHTTPSErrors: false, storageState: await context.storageState() });
    stage('dashboard-deep-link');
    const cold = await browser.newContext({ baseURL: origin, ignoreHTTPSErrors: false, storageState: await context.storageState() });
    try {
      const launch = await cold.newPage();
      await launch.goto('/dashboard');
      await expect(launch).toHaveURL(`${origin}/dashboard`);
      await expect(launch.getByRole('heading', { name: 'Your plants', exact: true })).toBeVisible();
    } finally { await cold.close(); }
    stage('catalog');
    await page.goto('/plants');
    await auditAccessibility(page, 'Public staging catalog');
    const species = page.getByRole('article').filter({ has: page.getByRole('heading', { name: 'Demo houseplant', exact: true }) });
    await species.getByRole('link', { name: 'View details' }).click();
    await page.getByLabel('Nickname', { exact: true }).fill(nickname);
    await page.getByLabel('Location', { exact: false }).fill('Synthetic browser shelf');
    await page.getByLabel('Tags', { exact: false }).fill('staging-browser, synthetic');
    await page.getByLabel('Water every (days)', { exact: true }).fill('1');
    const past = new Date(Date.now() - 3 * 86_400_000);
    const local = new Intl.DateTimeFormat('sv-SE', { timeZone: 'Europe/Berlin', year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', hourCycle: 'h23' }).format(past).replace(' ', 'T');
    await page.getByLabel('Last watered', { exact: false }).fill(local);
    stage('create-plant');
    const created = page.waitForResponse(r => new URL(r.url()).pathname === '/api/my-plants' && r.request().method() === 'POST');
    await page.getByRole('button', { name: 'Add to My Plants', exact: true }).click();
    expect((await created).status(), 'Create fixture status').toBe(201);
    const card = page.getByRole('article').filter({ has: page.getByRole('heading', { name: nickname, exact: true }) });
    await expect(card).toBeVisible();
    // Resolve the unique fixture through the authenticated API. Firefox may
    // discard a response body after UI navigation, and service-worker-owned
    // requests bypass page.route interception. Keep the real worker enabled.
    const collection = await context.request.get('/api/my-plants');
    expect(collection.status()).toBe(200);
    plantId = (await collection.json()).find((item: { nickname: string }) => item.nickname === nickname)?.id;
    expect(plantId, 'Synthetic plant identifier').toMatch(/^[a-f0-9-]{36}$/i);
    writeFileSync('/private/current-fixture.json', JSON.stringify({ plantId, nickname }));
    await auditAccessibility(page, 'Public staging collection');
    stage('tags');
    await card.getByLabel('Tags', { exact: true }).fill('staging-browser, updated');
    await card.getByRole('button', { name: 'Save tags', exact: true }).click();
    await expect(card.locator('.tag-list')).toContainText('updated');
    await page.reload();
    await expect(card.locator('.tag-list')).toContainText('updated');

    stage('account-isolation');
    const other = await browser.newContext({ baseURL: origin, ignoreHTTPSErrors: false });
    try {
      const b = await other.newPage(); await login(b, data.b);
      const token = (await (await other.request.get('/api/antiforgery/token')).json()).requestToken;
      const headers = { 'X-XSRF-TOKEN': token };
      expect((await other.request.get(`/api/my-plants/${plantId}`)).status()).toBe(404);
      expect((await other.request.get(`/api/my-plants/${plantId}/care/history`)).status()).toBe(404);
      expect((await other.request.post(`/api/my-plants/${plantId}/care/Watering/complete`, { headers, data: { completedAtUtc: new Date().toISOString() } })).status()).toBe(404);
      expect((await other.request.delete(`/api/my-plants/${plantId}`, { headers })).status()).toBe(404);
      const list = await (await other.request.get('/api/my-plants')).json();
      expect(list.some((item: { id: string }) => item.id === plantId), 'Other account cannot list probe').toBe(false);
      await submit(b, 'Log out', 'logout', 204);
    } finally { await other.close(); }

    stage('notifications');
    await page.getByRole('navigation', { name: 'Main navigation' }).getByRole('link', { name: 'Notifications', exact: true }).click();
    await expect(page.getByRole('checkbox', { name: 'Show reminders in PlantCare', exact: true })).toBeVisible();
    await expect(page.getByRole('checkbox', { name: 'Show reminders in PlantCare', exact: true })).toBeChecked();
    stage('notification-audit');
    await auditAccessibility(page, 'Public staging notifications');
    stage('worker-reminder');
    await expect.poll(async () => {
      const response = await context.request.get('/api/notifications');
      expect(response.status(), 'Reminder API status').toBe(200);
      return (await response.json()).some((n: { userPlantId: string }) => n.userPlantId === plantId);
    }, { timeout: 135_000, intervals: [5_000] }).toBe(true);
    stage('dashboard-completion');
    await page.getByRole('navigation', { name: 'Main navigation' }).getByRole('link', { name: 'Dashboard', exact: true }).click();
    const overdue = page.locator('.care-section.overdue'); await expect(overdue).toContainText(nickname);
    await overdue.locator('.care-card').filter({ hasText: nickname }).getByRole('button', { name: 'Water', exact: true }).click();
    stage('history');
    await page.getByRole('navigation', { name: 'Main navigation' }).getByRole('link', { name: 'My plants', exact: true }).click();
    await card.getByRole('link', { name: 'View care history', exact: true }).click();
    await expect(page.locator('.history-entry')).toHaveCount(1);
    await auditAccessibility(page, 'Public staging care history');
    stage('calendar');
    await page.getByRole('navigation', { name: 'Main navigation' }).getByRole('link', { name: 'Calendar', exact: true }).click();
    await expect(page.getByRole('status').filter({ hasText: 'Loading calendar' })).toBeHidden();
    await page.getByRole('button', { name: 'Agenda', exact: true }).click();
    await expect(page.getByRole('status').filter({ hasText: 'Loading calendar' })).toBeHidden();
    await page.getByLabel(/^Status/).selectOption('Completed');
    await expect(page.getByRole('button').filter({ hasText: nickname })).toHaveCount(1);
    await auditAccessibility(page, 'Public staging calendar');
    stage('archive-restore');
    await page.goto('/my-plants'); await card.getByRole('button', { name: 'Archive plant', exact: true }).click();
    await expect(card).toBeHidden();
    await page.getByRole('button', { name: 'Show archived', exact: true }).click();
    const archived = page.locator('.archived-card').filter({ hasText: nickname });
    await archived.getByRole('button', { name: 'Restore plant', exact: true }).click();
    await expect(card).toBeVisible();
    await card.getByRole('button', { name: 'Archive plant', exact: true }).click();
    stage('logout');
    await submit(page, 'Log out', 'logout', 204);
    await login(page, data.b); await page.goto('/my-plants');
    await expect(page.getByRole('heading', { name: nickname, exact: true })).toBeHidden();
    await submit(page, 'Log out', 'logout', 204);
    await page.goto('/my-plants'); await expect(page.getByRole('heading', { name: 'Welcome back', exact: true })).toBeVisible();
    expect(errors, 'Uncaught public browser errors').toEqual([]);
  } finally {
    if (cleanup) {
      if (plantId) {
        const token = (await (await cleanup.request.get('/api/antiforgery/token')).json()).requestToken;
        const result = await cleanup.request.delete(`/api/my-plants/${plantId}`, { headers: { 'X-XSRF-TOKEN': token } });
        expect([204, 404].includes(result.status()), 'Owned synthetic probe cleanup').toBe(true);
      }
      await cleanup.close();
    }
  }
});

test('request the approved account reset through the browser', async ({ page }) => {
  test.skip(phase !== 'request-reset');
  const data = accounts();
  await page.goto('/forgot-password');
  await page.getByLabel('Email address', { exact: true }).fill(data.a.email);
  await submit(page, 'Send reset link', 'forgotPassword');
  await expect(page.getByText('If an account exists for that address', { exact: false })).toBeVisible();
});

test('resend the missing approved account confirmation', async ({ page }) => {
  test.skip(phase !== 'resend');
  const data = accounts();
  await page.goto('/resend-confirmation');
  await page.getByLabel('Email address', { exact: true }).fill(data.a.email);
  await submit(page, 'Send confirmation email', 'resendConfirmationEmail');
});

test('consume real reset link, reject old password, sign in and log out', async ({ page, context, browser }) => {
  test.skip(phase !== 'reset');
  const data = accounts();
  const oldPassword = data.a.password;
  const link = new URL(data.a.resetLink ?? 'https://invalid.invalid');
  if (link.origin !== origin || link.pathname !== '/reset-password' || link.searchParams.get('email') !== data.a.email || !link.searchParams.get('code') || oldPassword === data.nextPassword) throw new Error('Unexpected private reset route or account configuration.');
  stage('reset-old-session');
  const oldContext = await browser.newContext({ baseURL: origin, ignoreHTTPSErrors: false });
  try {
  await login(await oldContext.newPage(), data.a);
  stage('reset-consume');
  try { await page.goto(link.href); } catch { throw new Error('Private reset navigation failed; link withheld.'); }
  await page.getByLabel('New password', { exact: true }).fill(data.nextPassword);
  await page.getByLabel('Confirm new password', { exact: true }).fill(data.nextPassword);
  await submit(page, 'Reset password', 'resetPassword');
  data.a.password = data.nextPassword; delete data.a.resetLink;
  writeFileSync(privatePath, JSON.stringify(data));
  stage('reset-old-session-rejected');
  expect((await oldContext.request.get('/api/account/me')).status()).toBe(401);
  stage('reset-code-reuse');
  const token = (await (await context.request.get('/api/antiforgery/token')).json()).requestToken;
  const reused = await context.request.post('/api/auth/resetPassword', { headers: { 'X-XSRF-TOKEN': token }, data: { email: data.a.email, resetCode: link.searchParams.get('code'), newPassword: data.nextPassword } });
  expect(reused.status(), 'Used reset link is rejected').toBe(400);
  stage('reset-old-password');
  await page.getByLabel('Email address', { exact: true }).fill(data.a.email);
  await page.getByLabel('Password', { exact: true }).fill(oldPassword);
  await submit(page, 'Log in', 'login', 401);
  stage('reset-new-password');
  await login(page, data.a);
  await submit(page, 'Log out', 'logout', 204);
  } finally { await oldContext.close(); }
});

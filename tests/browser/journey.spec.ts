import { randomBytes, randomUUID } from 'node:crypto';
import { expect, test, type APIRequestContext, type Page } from '@playwright/test';

const origin = 'https://localhost:8443';
const mailpit = 'http://mailpit:8025';

async function step(title: string, body: () => Promise<void>) {
  console.info(`Browser step: ${title}`);
  await test.step(title, body);
}

// Submit actual browser forms. Respect the production IP rate limit without
// adding a test-only bypass or replaying writes through the API.
async function submitAuth(page: Page, button: string, endpoint: string, expected = 200) {
  for (let attempt = 0; attempt < 4; attempt++) {
    if (endpoint !== 'logout') {
      await expect(page.locator('form'), `Valid ${endpoint} browser form`).toHaveClass(/\bng-valid\b/);
    }
    const responsePromise = page.waitForResponse(response =>
      new URL(response.url()).pathname === `/api/auth/${endpoint}` &&
      response.request().method() === 'POST');
    await page.getByRole('button', { name: button, exact: true }).click();
    const response = await responsePromise;
    if (response.status() !== 429 || attempt === 3) {
      expect(response.status(), `Browser ${endpoint} response`).toBe(expected);
      return;
    }
    await page.waitForTimeout(20_000);
  }
}

async function mailLink(request: APIRequestContext, email: string, subject: string, path: string) {
  let messageId: string | undefined;
  await expect.poll(async () => {
    const response = await request.get(`${mailpit}/api/v1/search`, { params: { query: `to:${email}` } });
    expect(response.ok()).toBeTruthy();
    const result = await response.json();
    messageId = result.messages?.find((message: { Subject: string }) => message.Subject === subject)?.ID;
    return Boolean(messageId);
  }, { message: 'Expected captured email', timeout: 20_000 }).toBe(true);
  const response = await request.get(`${mailpit}/api/v1/message/${messageId}`);
  expect(response.ok()).toBeTruthy();
  const message = await response.json();
  const href = /href="([^"]+)"/.exec(message.HTML)?.[1];
  if (!href) throw new Error('Captured email has no link.');
  const link = new URL(href.replaceAll('&amp;', '&'));
  // Check without printing secret query parameters on failure.
  expect(link.origin === origin && link.pathname === path, 'Local email link target').toBe(true);
  return link.href;
}

async function visitSecretLink(page: Page, link: string) {
  try { await page.goto(link); }
  catch { throw new Error('Local email-link navigation failed; secret URL withheld.'); }
}

async function login(page: Page, email: string, password: string, expected = 200) {
  await expect(page.getByRole('heading', { name: 'Welcome back', exact: true })).toBeVisible();
  await page.getByLabel('Email address', { exact: true }).fill(email);
  await page.getByLabel('Password', { exact: true }).fill(password);
  await submitAuth(page, 'Log in', 'login', expected);
}

test('register, confirm, care for a plant, recover password and log out', async ({ page, request, browser }) => {
  const email = `browser-${randomUUID()}@example.test`;
  const password = `Pc9!${randomBytes(20).toString('hex')}`;
  const newPassword = `Pc9!${randomBytes(20).toString('hex')}`;
  const nickname = 'Browser journey fern';
  const pageErrors: string[] = [];
  page.on('pageerror', error => pageErrors.push(error.name));

  await step('Protected route, registration and real confirmation email', async () => {
    await page.goto('/my-plants');
    await expect(page).toHaveURL(/\/login\?returnUrl=/);
    await page.getByRole('link', { name: 'Create an account', exact: true }).click();
    await expect(page.getByRole('heading', { name: 'Create your account', exact: true })).toBeVisible();
    await page.getByLabel('Email address', { exact: true }).fill(email);
    await page.getByLabel('Password', { exact: true }).fill(password);
    await page.getByLabel('Confirm password', { exact: true }).fill(password);
    await submitAuth(page, 'Create account', 'register');
    await expect(page.getByText('Your account was created.', { exact: false })).toBeVisible();
    await login(page, email, password, 401);
    await expect(page.getByText('The credentials are incorrect, or the email address has not been confirmed.')).toBeVisible();
    const confirmation = await mailLink(request, email, 'Confirm your PlantCare account', '/api/auth/confirmEmail');
    await visitSecretLink(page, confirmation);
    await page.goto('/my-plants');
    await login(page, email, password);
    await expect(page).toHaveURL(`${origin}/my-plants`);
    await expect(page.getByRole('heading', { name: 'No plants yet' })).toBeVisible();
    await page.reload();
    await expect(page.getByRole('heading', { name: 'Your plant collection' })).toBeVisible();
  });

  await step('Catalog, plant creation, schedule and persisted tags', async () => {
    await page.getByRole('link', { name: 'Browse the plant catalog', exact: true }).click();
    const species = page.getByRole('article').filter({ has: page.getByRole('heading', { name: 'Demo houseplant', exact: true }) });
    await species.getByRole('link', { name: 'View details' }).click();
    await page.getByLabel('Nickname', { exact: true }).fill(nickname);
    await page.getByLabel('Location', { exact: false }).fill('Browser test room');
    await page.getByLabel('Tags', { exact: false }).fill('browser, favorite');
    await page.getByLabel('Water every (days)', { exact: true }).fill('1');
    // A past timestamp makes the dashboard due-state deterministic without
    // altering host clocks or depending on month/day-boundary formatting.
    const past = new Date(Date.now() - 3 * 86_400_000);
    const local = new Intl.DateTimeFormat('sv-SE', {
      timeZone: 'Europe/Berlin', year: 'numeric', month: '2-digit', day: '2-digit',
      hour: '2-digit', minute: '2-digit', hourCycle: 'h23',
    }).format(past).replace(' ', 'T');
    await page.getByLabel('Last watered', { exact: false }).fill(local);
    await page.getByRole('button', { name: 'Add to My Plants', exact: true }).click();
    await expect(page).toHaveURL(`${origin}/my-plants`);
    const plant = page.getByRole('article').filter({ has: page.getByRole('heading', { name: nickname, exact: true }) });
    await expect(plant).toContainText('Browser test room');
    await plant.getByLabel('Tags', { exact: true }).fill('browser, updated');
    await plant.getByRole('button', { name: 'Save tags', exact: true }).click();
    await expect(plant.locator('.tag-list')).toContainText('updated');
    const watering = plant.locator('.care-schedule').filter({ hasText: 'Watering' });
    await watering.getByLabel('Interval', { exact: false }).fill('2');
    await watering.getByRole('button', { name: 'Save schedule', exact: true }).click();
    await expect(watering.locator('.interval')).toHaveText('Every 2 days');
    await page.reload();
    await expect(plant.locator('.tag-list')).toContainText('updated');
    await expect(watering.locator('.interval')).toHaveText('Every 2 days');
  });

  await step('Overdue dashboard completion, history and calendar', async () => {
    const nav = page.getByRole('navigation', { name: 'Main navigation' });
    await nav.getByRole('link', { name: 'Dashboard', exact: true }).click();
    const overdue = page.locator('.care-section.overdue');
    await expect(overdue).toContainText(nickname);
    await overdue.getByRole('button', { name: 'Water', exact: true }).click();
    await expect(page.getByRole('heading', { name: 'Everything is taken care of!' })).toBeVisible();
    await nav.getByRole('link', { name: 'My plants', exact: true }).click();
    await page.getByRole('link', { name: 'View care history', exact: true }).click();
    await expect(page.locator('.history-entry')).toHaveCount(1);
    await expect(page.locator('.history-entry')).toContainText('Watering');
    await page.getByLabel('Notes', { exact: true }).fill('Recorded in the browser');
    await page.getByRole('button', { name: 'Record care', exact: true }).click();
    const entry = page.locator('.history-entry').filter({ hasText: 'Recorded in the browser' });
    await expect(entry).toBeVisible();
    await entry.getByRole('button', { name: 'Edit', exact: true }).click();
    await entry.getByLabel('Notes', { exact: true }).fill('Edited in the browser');
    await entry.getByRole('button', { name: 'Save changes', exact: true }).click();
    const edited = page.locator('.history-entry').filter({ hasText: 'Edited in the browser' });
    await expect(edited).toBeVisible();
    page.once('dialog', dialog => dialog.accept());
    await edited.getByRole('button', { name: 'Delete', exact: true }).click();
    await expect(page.locator('.history-entry')).toHaveCount(1);
    await nav.getByRole('link', { name: 'Calendar', exact: true }).click();
    // Wait for each calendar load before changing view/selecting an entry.
    // A pending response clears the selection when it replaces the entries.
    const calendarLoading = page.getByRole('status').filter({ hasText: 'Loading calendar' });
    await expect(calendarLoading).toBeHidden();
    await page.getByRole('button', { name: 'Agenda', exact: true }).click();
    await expect(calendarLoading).toBeHidden();
    await page.getByLabel(/^Status/).selectOption('Completed');
    const event = page.getByRole('button').filter({ hasText: nickname });
    await expect(event).toHaveCount(1);
    await event.click();
    await expect(page.locator('.details')).toContainText('Completed');
  });

  await step('Weekday settings retain their time zone in a travelling browser', async () => {
    await page.getByRole('navigation').getByRole('link', { name: 'My plants', exact: true }).click();
    const watering = page.locator('.care-schedule').filter({ hasText: 'Watering' });
    await watering.getByRole('combobox', { name: /Schedule/ }).selectOption('Weekdays');
    await watering.getByRole('checkbox', { name: 'Mon', exact: true }).check();
    await watering.getByLabel('Local reminder time', { exact: true }).fill('09:00');
    await watering.getByRole('button', { name: 'Save schedule', exact: true }).click();
    await expect(watering.locator('.interval')).toHaveText('Mon at 09:00');
    await expect(watering).toContainText('Reminder time zone: Europe/Berlin');
    const travelling = await browser.newContext({
      baseURL: origin, ignoreHTTPSErrors: true, timezoneId: 'America/New_York',
    });
    try {
      const otherPage = await travelling.newPage();
      otherPage.on('pageerror', error => pageErrors.push(error.name));
      await otherPage.goto('/my-plants');
      await login(otherPage, email, password);
      await expect(otherPage.getByRole('heading', { name: 'Your plant collection' })).toBeVisible();
      const otherWatering = otherPage.locator('.care-schedule').filter({ hasText: 'Watering' });
      await expect(otherWatering).toContainText('Reminder time zone: Europe/Berlin');
      const saved = otherPage.waitForResponse(response =>
        response.url().endsWith('/care/Watering/schedule') && response.request().method() === 'PUT');
      await otherWatering.getByRole('button', { name: 'Pause', exact: true }).click();
      const result = await saved;
      expect(result.status()).toBe(200);
      const schedule = await result.json();
      expect(schedule.timeZoneId).toBe('Europe/Berlin');
      expect(schedule.isEnabled).toBe(false);
      await expect(otherWatering.getByRole('button', { name: 'Resume', exact: true })).toBeVisible();
      await submitAuth(otherPage, 'Log out', 'logout', 204);
      await expect(otherPage.getByRole('heading', { name: 'Welcome back' })).toBeVisible();
    } finally { await travelling.close(); }
    await page.reload();
    await watering.getByRole('button', { name: 'Resume', exact: true }).click();
    await expect(watering.getByRole('button', { name: 'Pause', exact: true })).toBeVisible();
    await expect(watering).toContainText('Reminder time zone: Europe/Berlin');
  });

  await step('Archive and restore survive reload', async () => {
    await page.getByRole('navigation').getByRole('link', { name: 'My plants', exact: true }).click();
    await page.getByRole('button', { name: 'Archive plant', exact: true }).click();
    await expect(page.getByRole('heading', { name: 'No plants yet' })).toBeVisible();
    await page.getByRole('button', { name: 'Show archived', exact: true }).click();
    await expect(page.locator('.archived-card')).toContainText(nickname);
    await page.getByRole('button', { name: 'Restore plant', exact: true }).click();
    await expect(page.locator('.plant-card').getByRole('heading', { name: nickname, exact: true })).toBeVisible();
    await page.reload();
    await expect(page.getByRole('heading', { name: nickname, exact: true })).toBeVisible();
    await submitAuth(page, 'Log out', 'logout', 204);
    await expect(page.getByRole('heading', { name: 'Welcome back', exact: true })).toBeVisible();
  });

  await step('Forgot/reset forms, confirmation validation and new-password login', async () => {
    await page.goto('/login');
    await page.getByRole('link', { name: 'Forgot your password?', exact: true }).click();
    await expect(page.getByRole('heading', { name: 'Reset your password', exact: true })).toBeVisible();
    await page.getByLabel('Email address', { exact: true }).fill(email);
    await submitAuth(page, 'Send reset link', 'forgotPassword');
    await expect(page.getByText('If an account exists for that address', { exact: false })).toBeVisible();
    const reset = await mailLink(request, email, 'Reset your PlantCare password', '/reset-password');
    await visitSecretLink(page, reset);
    await page.getByLabel('New password', { exact: true }).fill(newPassword);
    await page.getByLabel('Confirm new password', { exact: true }).fill('Different9!');
    await page.getByRole('button', { name: 'Reset password', exact: true }).click();
    await expect(page.getByText('The password confirmation does not match.')).toBeVisible();
    await page.getByLabel('Confirm new password', { exact: true }).fill(newPassword);
    await submitAuth(page, 'Reset password', 'resetPassword');
    await expect(page.getByText('Your password has been reset. You can log in now.')).toBeVisible();
    await login(page, email, password, 401);
    await expect(page.locator('.form-error')).toContainText('credentials are incorrect');
    await login(page, email, newPassword);
    await expect(page.getByRole('heading', { name: 'Your plants', exact: true })).toBeVisible();
    await page.getByRole('navigation').getByRole('link', { name: 'My plants', exact: true }).click();
    await expect(page.getByRole('heading', { name: nickname, exact: true })).toBeVisible();
    // Leave successful runs with only an archived synthetic plant.
    await page.getByRole('button', { name: 'Archive plant', exact: true }).click();
    await expect(page.getByRole('heading', { name: 'No plants yet' })).toBeVisible();
    await submitAuth(page, 'Log out', 'logout', 204);
    await page.goto('/my-plants');
    await expect(page.getByRole('heading', { name: 'Welcome back', exact: true })).toBeVisible();
    await page.goto('/reset-password');
    await expect(page.getByText('This reset link is incomplete.', { exact: false })).toBeVisible();
  });
  expect(pageErrors, 'Uncaught browser exceptions').toEqual([]);
});

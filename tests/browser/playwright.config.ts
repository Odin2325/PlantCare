import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: '.',
  testMatch: ['journey.spec.ts', 'sharing.spec.ts', 'pwa.spec.ts', 'accessibility.spec.ts', 'catalog.spec.ts', 'garden.spec.ts', 'cultivars.spec.ts'],
  webServer: {
    command: 'node pwa-server.mjs',
    url: 'http://127.0.0.1:4173/__test/ready',
    reuseExistingServer: false,
    timeout: 60_000,
  },
  fullyParallel: false,
  workers: 1,
  retries: 0,
  timeout: 240_000,
  expect: { timeout: 15_000 },
  reporter: 'list',
  use: {
    baseURL: 'https://localhost:8443',
    ignoreHTTPSErrors: true, // Local Caddy CA only; the target is fixed below.
    timezoneId: 'Europe/Berlin',
    actionTimeout: 20_000,
    navigationTimeout: 30_000,
    // Reset links, passwords and authentication cookies must not enter traces.
    trace: 'off',
    screenshot: 'off',
    video: 'off',
  },
  projects: [
    { name: 'desktop-chromium', use: { ...devices['Desktop Chrome'] } },
    { name: 'mobile-chromium', use: { ...devices['Pixel 7'] } },
    { name: 'desktop-firefox', use: { ...devices['Desktop Firefox'] } },
  ],
});

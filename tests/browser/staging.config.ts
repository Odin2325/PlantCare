import { defineConfig } from '@playwright/test';
import base from './playwright.config';
export default defineConfig({
  ...base, webServer: undefined,
  testMatch: ['accessibility.spec.ts', 'staging.spec.ts'],
  timeout: 300_000,
  reporter: [['./staging-reporter.ts']],
  outputDir: '/tmp/plantcare-staging-results',
  use: { ...base.use, baseURL: 'https://staging.plantacare.org', ignoreHTTPSErrors: false },
});

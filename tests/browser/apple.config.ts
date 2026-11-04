import { defineConfig, devices } from '@playwright/test';
import base from './playwright.config';

// Focused, synthetic fixtures; these checks are not real iOS device acceptance.
export default defineConfig({
  ...base,
  testMatch: ['apple.spec.ts', 'style-review.spec.ts'],
  use: { ...base.use, baseURL: 'http://localhost:4173' },
  projects: [
    { name: 'iphone-webkit', use: { ...devices['iPhone 13'] } },
    { name: 'ipad-webkit', use: { ...devices['iPad Pro 11'] } },
    { name: 'mac-webkit', use: { ...devices['Desktop Safari'] } },
  ],
});

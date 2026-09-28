import { defineConfig, devices } from '@playwright/test';

/** Set PORTAL_URL to test a running portal (compose serves it on :8081); otherwise `ng serve` is started. */
const PORTAL_URL = process.env['PORTAL_URL'] ?? 'http://localhost:4200';
const CI = !!process.env['CI'];

export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
  forbidOnly: CI,
  retries: CI ? 1 : 0,
  reporter: CI ? [['github'], ['html', { open: 'never' }]] : [['list'], ['html', { open: 'never' }]],
  globalSetup: './e2e/support/global-setup.ts',
  use: {
    baseURL: PORTAL_URL,
    locale: 'en-US',
    timezoneId: 'America/Sao_Paulo',
    trace: 'retain-on-failure',
    video: 'retain-on-failure',
    screenshot: 'only-on-failure',
  },
  projects: [
    { name: 'e2e', testMatch: /specs\/.*\.spec\.ts/, use: { ...devices['Desktop Chrome'] } },
    { name: 'e2e-mobile', testMatch: /specs\/(auth|create|lifecycle)\.spec\.ts/, use: { ...devices['Pixel 7'] } },
    { name: 'shots', testMatch: /shots\/.*\.shots\.ts/, use: { ...devices['Desktop Chrome'] } },
    {
      name: 'demo',
      testMatch: /demos\/.*\.demo\.ts/,
      fullyParallel: false,
      retries: 0,
      use: {
        ...devices['Desktop Chrome'],
        viewport: { width: 1280, height: 720 },
        deviceScaleFactor: 1,
        video: { mode: 'on', size: { width: 1280, height: 720 } },
      },
    },
  ],
  webServer: process.env['PORTAL_URL']
    ? undefined
    : { command: 'npm run start -- --port 4200', url: PORTAL_URL, reuseExistingServer: !CI, timeout: 120_000 },
});

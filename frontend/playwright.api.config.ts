import { defineConfig } from '@playwright/test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
const path =
  process.env['SNP_E2E_ENV'] ??
  resolve(__dirname, '../.superpowers/sdd/2026-10-21-fe-be-integration/e2e-config.json');
const env = JSON.parse(readFileSync(path, 'utf8'));
if (!/^snp-integration-e2e-/.test(env.project) || env.accessTokenMinutes !== 1)
  throw new Error('Run scripts/prepare-api-e2e.ps1 for an isolated one-minute-token stack.');
export default defineConfig({
  testDir: './e2e-api',
  fullyParallel: false,
  workers: 1,
  retries: 0,
  timeout: 120000,
  expect: { timeout: 15000 },
  reporter: [
    ['list'],
    ['json', { outputFile: '../.superpowers/sdd/2026-10-21-fe-be-integration/e2e-results.json' }],
  ],
  use: {
    baseURL: env.baseUrl,
    viewport: { width: 1440, height: 1000 },
    screenshot: 'only-on-failure',
    trace: 'retain-on-failure',
  },
  webServer: {
    command: 'npm run start -- --host 127.0.0.1 --port 14202 --proxy-config "' + env.proxy + '"',
    url: env.baseUrl,
    reuseExistingServer: false,
    timeout: 180000,
  },
});

import { test as base, expect, Page, APIRequestContext } from '@playwright/test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
export const env = JSON.parse(
  readFileSync(
    process.env['SNP_E2E_ENV'] ??
      resolve(__dirname, '../../../.superpowers/sdd/2026-10-21-fe-be-integration/e2e-config.json'),
    'utf8',
  ),
);
export type Role = 'admin' | 'yard' | 'order' | 'gate';
export interface Session {
  userName: string;
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
}
export type Sessions = Record<Role, Session>;
export const test = base.extend<
  { role: Role; rolePage: Page },
  { sessions: Sessions; api: APIRequestContext }
>({
  role: ['admin', { option: true }],
  api: [
    async ({ playwright }, use) => {
      const api = await playwright.request.newContext({ baseURL: env.apiUrl });
      await use(api);
      await api.dispose();
    },
    { scope: 'worker' },
  ],
  sessions: [
    async ({ api }, use) => {
      const sessions = {} as Sessions;
      for (const role of ['admin', 'yard', 'order', 'gate'] as Role[]) {
        const response = await api.post('/api/auth/login', {
          data: {
            userName: role === 'admin' ? env.adminUserName : role,
            password: role === 'admin' ? env.adminPassword : env.demoPassword,
          },
        });
        expect(response.status()).toBe(200);
        sessions[role] = await response.json();
      }
      await use(sessions);
    },
    { scope: 'worker' },
  ],
  rolePage: async ({ browser, role, sessions }, use) => {
    const context = await browser.newContext({
      baseURL: env.baseUrl,
      viewport: { width: 1440, height: 1000 },
    });
    await context.addInitScript((session) => {
      if (!sessionStorage.getItem('snp-api-session'))
        sessionStorage.setItem('snp-api-session', JSON.stringify(session));
    }, sessions[role]);
    const page = await context.newPage();
    await page.goto(env.baseUrl + '/dashboard');
    await page.getByRole('button', { name: 'Đăng xuất', exact: true }).waitFor();
    await use(page);
    const current = await page
      .evaluate(() => sessionStorage.getItem('snp-api-session'))
      .catch(() => null);
    if (current) sessions[role] = JSON.parse(current);
    await context.close();
  },
});
export { expect };
export function containerNumber(index: number): string {
  const owner =
    index >= 37
      ? 'MSK'
      : index >= 35
        ? 'MAE'
        : index >= 17 && index < 20
          ? 'MED'
          : index >= 10 && index < 13
            ? 'MAE'
            : index >= 3 && index < 10
              ? 'MED'
              : 'MSC';
  const raw = owner + 'U' + String(900000 + index);
  let sum = 0;
  for (let i = 0; i < 10; i++) {
    let n = /\d/.test(raw[i]) ? Number(raw[i]) : raw.charCodeAt(i) - 65 + 10;
    if (/[A-Z]/.test(raw[i])) {
      if (n >= 11) n++;
      if (n >= 22) n++;
      if (n >= 33) n++;
    }
    sum += n * 2 ** i;
  }
  return raw + ((sum % 11) % 10);
}

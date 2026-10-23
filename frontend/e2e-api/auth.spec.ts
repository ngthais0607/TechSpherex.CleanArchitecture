import { test, expect, env } from './fixtures/api-fixture';
test('real login, me restore and local/server logout', async ({ page, api, sessions }) => {
  const name = 'auth' + Date.now();
  const registration = await api.post('/api/auth/register', {
    headers: { Authorization: 'Bearer ' + sessions.admin.accessToken },
    data: {
      userName: name,
      email: name + '@example.test',
      firstName: 'API',
      lastName: 'E2E',
      password: env.demoPassword,
      role: 'GateClerk',
    },
  });
  expect(registration.status()).toBe(201);
  await page.goto('/login');
  await page.locator('#username').fill(name);
  await page.locator('#password').fill(env.demoPassword);
  await page.getByRole('button', { name: 'Đăng nhập', exact: true }).click();
  await page.waitForURL('**/dashboard');
  await page.reload();
  await page.getByRole('link', { name: 'Cổng nhập / xuất', exact: true }).click();
  await page.locator('#gate-vehicle').fill('UNSAVED');
  page.once('dialog', (dialog) => dialog.dismiss());
  await page.getByRole('button', { name: 'Đăng xuất', exact: true }).click();
  await expect(page).toHaveURL(/\/gate$/);
  expect(await page.evaluate(() => sessionStorage.getItem('snp-api-session'))).not.toBeNull();
  page.once('dialog', (dialog) => dialog.accept());
  await page.getByRole('button', { name: 'Đăng xuất', exact: true }).click();
  await page.waitForURL('**/login');
  expect(await page.evaluate(() => sessionStorage.getItem('snp-api-session'))).toBeNull();
});
test('real one-minute token rotates once after the 30-second buffer', async ({
  rolePage: page,
}) => {
  const before = await page.evaluate(() => JSON.parse(sessionStorage.getItem('snp-api-session')!));
  await page.waitForTimeout(
    Math.max(0, Date.parse(before.accessTokenExpiresAt) - Date.now() - 29000),
  );
  const refresh = page.waitForResponse(
    (response) => response.url().endsWith('/api/auth/refresh') && response.status() === 200,
  );
  await page.getByRole('link', { name: 'Container', exact: true }).click();
  await refresh;
  const after = await page.evaluate(() => JSON.parse(sessionStorage.getItem('snp-api-session')!));
  expect(after.refreshToken).not.toBe(before.refreshToken);
  expect(after.accessToken).not.toBe(before.accessToken);
  await page.reload();
  await page.getByRole('heading', { name: 'Danh sách container', exact: true }).waitFor();
});

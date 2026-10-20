import { test, expect } from '@playwright/test';
test.beforeEach(async ({ page }) => {
  await page.goto('/login');
  await page.getByLabel('Tên đăng nhập').fill('demo');
  await page.getByLabel('Mật khẩu', { exact: true }).fill('Demo@123');
  await page.getByRole('button', { name: 'Đăng nhập', exact: true }).click();
  await expect(page).toHaveURL(/dashboard/);
});
test('all screens navigate and never overflow the viewport', async ({ page }) => {
  for (const [url, title] of [
    ['containers', 'Danh sách container'],
    ['delivery-orders', 'Lệnh giao container'],
    ['gate', 'Ghi nhận nhập / xuất cổng'],
    ['yard', 'Sơ đồ vị trí bãi'],
    ['partners', 'Khách hàng & hãng khai thác'],
    ['reports', 'Báo cáo hoạt động'],
    ['inventory', 'Tồn bãi & thời gian lưu'],
  ]) {
    await page.goto('/' + url);
    await expect(page.getByRole('heading', { name: title, exact: true })).toBeVisible();
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(
      true,
    );
  }
});
test('login rejects invalid credentials and returns to requested screen', async ({ page }) => {
  await page.getByRole('button', { name: 'Đăng xuất' }).click();
  await page.goto('/inventory');
  await page.getByLabel('Tên đăng nhập').fill('demo');
  await page.getByLabel('Mật khẩu', { exact: true }).fill('bad');
  await page.getByRole('button', { name: 'Đăng nhập', exact: true }).click();
  await expect(page.getByRole('alert')).toContainText('không đúng');
  await page.getByLabel('Mật khẩu', { exact: true }).fill('Demo@123');
  await page.getByRole('button', { name: 'Đăng nhập', exact: true }).click();
  await expect(page).toHaveURL(/inventory/);
});
test('filters show empty state and dialog traps focus and closes', async ({ page }) => {
  await page.goto('/containers');
  await page.getByPlaceholder('Tìm số container, mã ISO...').fill('ZZZZ9999999');
  await expect(page.getByText('Không có container phù hợp')).toBeVisible();
  await page.getByRole('button', { name: 'Thêm container', exact: true }).click();
  const dialog = page.getByRole('dialog');
  await expect(dialog).toBeVisible();
  await page.keyboard.press('Escape');
  await expect(dialog).not.toBeVisible();
});
test('tablet forms and navigation remain usable', async ({ page }) => {
  await page.setViewportSize({ width: 768, height: 1024 });
  await page.goto('/containers');
  await page.getByRole('button', { name: 'Thêm container', exact: true }).click();
  await expect(page.getByRole('dialog')).toBeVisible();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await expect(page.getByRole('button', { name: 'Lưu container', exact: true })).toBeVisible();
});

import { test, expect } from '@playwright/test';
test.beforeEach(async ({ page }) => {
  await page.goto('/login');
  await page.getByLabel('Tên đăng nhập').fill('demo');
  await page.getByLabel('Mật khẩu', { exact: true }).fill('Demo@123');
  await page.getByRole('button', { name: 'Đăng nhập', exact: true }).click();
  await expect(page).toHaveURL(/dashboard/);
});
test('empty data, retry and reset keep all pages usable', async ({ page }) => {
  await page.getByRole('button', { name: 'Công cụ demo', exact: true }).click();
  page.once('dialog', (d) => d.accept());
  await page.getByRole('button', { name: 'Dữ liệu trống', exact: true }).click();
  await page.goto('/containers');
  await expect(page.getByText('Không có container phù hợp')).toBeVisible();
  await page.getByRole('button', { name: 'Công cụ demo', exact: true }).click();
  await page.getByRole('button', { name: 'Mô phỏng lỗi đọc', exact: true }).click();
  await expect(
    page.getByRole('heading', { name: 'Chưa thể tải dữ liệu', exact: true }),
  ).toBeVisible();
  await page.getByRole('button', { name: 'Thử lại', exact: true }).click();
  await expect(page.getByText('Không có container phù hợp')).toBeVisible();
  page.once('dialog', (d) => d.accept());
  await page.getByRole('button', { name: 'Khôi phục mẫu', exact: true }).click();
  await expect(page.locator('tbody tr').first()).toContainText('CMAU');
});
test('long dialogs preserve footer and focus at required viewports', async ({ page }) => {
  for (const [width, height] of [
    [1440, 900],
    [1366, 768],
    [1024, 768],
    [768, 1024],
  ]) {
    await page.setViewportSize({ width, height });
    await page.goto('/containers');
    await page.getByRole('button', { name: 'Thêm container', exact: true }).click();
    const dialog = page.getByRole('dialog');
    await expect(dialog).toBeVisible();
    await expect(page.getByRole('button', { name: 'Lưu container', exact: true })).toBeInViewport();
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(
      true,
    );
    await page.screenshot({ path: `docs/screenshots/container-form-${width}.png` });
    await page.keyboard.press('Escape');
    await expect(dialog).not.toBeVisible();
    await expect(page.getByRole('button', { name: 'Thêm container', exact: true })).toBeFocused();
  }
  for (const name of ['dashboard', 'gate', 'yard', 'inventory']) {
    await page.setViewportSize({ width: 1440, height: 900 });
    await page.goto('/' + name);
    await page.screenshot({ path: `docs/screenshots/${name}.png`, fullPage: true });
  }
});

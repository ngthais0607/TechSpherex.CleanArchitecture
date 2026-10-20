import { test, expect } from '@playwright/test';
test('long Vietnamese names and equivalent 125 percent viewport do not hide actions', async ({
  page,
}) => {
  await page.goto('/partners');
  await page.getByRole('button', { name: 'Sửa', exact: true }).first().click();
  await page.locator('#entry-form-name').fill('Công ty vận tải và dịch vụ logistics '.repeat(5));
  await page.getByRole('button', { name: 'Lưu đối tác', exact: true }).click();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await page.setViewportSize({ width: 1093, height: 614 });
  await page.goto('/containers');
  await page.getByRole('button', { name: 'Thêm container', exact: true }).click();
  await expect(page.getByRole('button', { name: 'Lưu container', exact: true })).toBeInViewport();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await page.screenshot({ path: 'docs/screenshots/container-form-125-equivalent.png' });
});
test('gate navigation and logout preserve data when discard is cancelled', async ({ page }) => {
  await page.goto('/gate');
  await page.locator('#gate-number').fill('CSQU3054383');
  page.on('dialog', (d) => d.dismiss());
  await page.locator('nav').getByRole('link', { name: 'Container', exact: true }).click();
  await expect(page).toHaveURL(/\/gate/);
  await expect(page.locator('#gate-number')).toHaveValue('CSQU3054383');
  await page.getByRole('button', { name: 'Đăng xuất', exact: true }).click();
  await expect(page).toHaveURL(/\/gate/);
  await expect(page.locator('#gate-number')).toHaveValue('CSQU3054383');
});
test('order line-only changes require discard confirmation', async ({ page }) => {
  await page.goto('/delivery-orders');
  await page.getByRole('button', { name: 'Thêm lệnh giao', exact: true }).click();
  await page.locator('.order-line input').fill('2');
  page.on('dialog', (d) => d.dismiss());
  await page.getByRole('dialog').getByLabel('Đóng', { exact: true }).click();
  await expect(page.locator('.order-line input')).toHaveValue('2');
  await page
    .getByRole('dialog')
    .locator('footer')
    .getByRole('button', { name: 'Hủy', exact: true })
    .click();
  await expect(page.locator('.order-line input')).toHaveValue('2');
});

test('footer close preserves dirty movement form when discard is cancelled', async ({ page }) => {
  await page.goto('/login');
  await page.getByLabel('Tên đăng nhập').fill('demo');
  await page.getByLabel('Mật khẩu', { exact: true }).fill('Demo@123');
  await page.getByRole('button', { name: 'Đăng nhập', exact: true }).click();
  await page.goto('/yard');
  await page.getByRole('button', { name: 'Di chuyển', exact: true }).click();
  await page.locator('#move-form-bay').fill('11');
  page.on('dialog', (d) => d.dismiss());
  await page
    .getByRole('dialog')
    .locator('footer')
    .getByRole('button', { name: 'Đóng', exact: true })
    .click();
  await expect(page.getByRole('dialog')).toBeVisible();
  await expect(page.locator('#move-form-bay')).toHaveValue('11');
});
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

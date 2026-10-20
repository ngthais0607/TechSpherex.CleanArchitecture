import { test, expect } from '@playwright/test';
test('complete container lifecycle persists across screens and reload', async ({ page }) => {
  await page.goto('/login');
  await page.getByLabel('Tên đăng nhập').fill('demo');
  await page.getByLabel('Mật khẩu', { exact: true }).fill('Demo@123');
  await page.getByRole('button', { name: 'Đăng nhập', exact: true }).click();
  await page.goto('/containers');
  await page.getByRole('button', { name: 'Thêm container', exact: true }).click();
  await page.locator('#entry-form-number').fill('CSQU3054383');
  await page.locator('#entry-form-operatorId').selectOption({ label: 'Maersk' });
  await page.locator('#entry-form-iso').fill('22G1');
  await page.getByRole('button', { name: 'Lưu container', exact: true }).click();
  await expect(page.getByRole('dialog')).not.toBeVisible();
  await page.goto('/gate');
  await page.locator('#gate-number').fill('CSQU3054383');
  await page.locator('#gate-vehicle').fill('51C-999.88');
  await page.locator('#gate-driver').fill('Nguyễn Văn Demo');
  await page.locator('#gate-block').selectOption('VB-01');
  await expect(page.getByRole('button', { name: 'Ghi nhận nhập bãi', exact: true })).toBeEnabled();
  await page.getByRole('button', { name: 'Ghi nhận nhập bãi', exact: true }).click();
  await expect(page.getByRole('status')).toContainText('Đã ghi nhận nhập');
  await page.goto('/delivery-orders');
  await page.getByRole('button', { name: 'Thêm lệnh giao', exact: true }).click();
  await page.locator('#entry-form-number').fill('DO-UI-TEST');
  await page.locator('#entry-form-customerId').selectOption({ index: 1 });
  await page.locator('#entry-form-operatorId').selectOption({ label: 'Maersk' });
  await page
    .locator('#entry-form-expiry')
    .fill(new Date(Date.now() + 10 * 86400000).toISOString().slice(0, 10));
  await page.getByRole('button', { name: 'Tạo lệnh giao', exact: true }).click();
  await expect(
    page.getByRole('dialog').getByRole('heading', { name: 'DO-UI-TEST', exact: true }),
  ).toBeVisible();
  await page.getByRole('button', { name: 'Gán container', exact: true }).click();
  await page.getByRole('checkbox', { name: 'Chọn CSQU3054383', exact: true }).check();
  await page.getByRole('button', { name: 'Gán 1 container', exact: true }).click();
  await expect(
    page.getByRole('dialog').getByRole('link', { name: 'CSQU3054383', exact: true }),
  ).toBeVisible();
  await page.goto('/yard');
  await page.getByRole('button', { name: 'Di chuyển', exact: true }).click();
  await page.locator('#move-form-number').selectOption({ label: 'CSQU3054383 · VB-01' });
  await page.locator('#move-form-blockId').selectOption({ label: 'Block A' });
  await page.locator('#move-form-bay').fill('11');
  await page.locator('#move-form-row').fill('D');
  await page.locator('#move-form-tier').fill('1');
  await page.getByRole('button', { name: 'Xác nhận di chuyển', exact: true }).click();
  await expect(page.getByRole('dialog')).not.toBeVisible();
  await expect(page.getByRole('heading', { name: 'A-11-D-1', exact: true })).toBeVisible();
  await page.goto('/gate?tab=out');
  await page.locator('#gate-number').fill('CSQU3054383');
  await page.locator('#gate-vehicle').fill('51C-999.88');
  await page.locator('#gate-driver').fill('Nguyễn Văn Demo');
  await expect(page.getByRole('button', { name: 'Ghi nhận xuất bãi', exact: true })).toBeEnabled();
  await page.getByRole('button', { name: 'Ghi nhận xuất bãi', exact: true }).click();
  await expect(page.getByRole('status')).toContainText('Đã ghi nhận xuất');
  await page.goto('/containers?search=CSQU3054383');
  await expect(page.locator('tbody')).toContainText('Đã xuất');
  await page.reload();
  await expect(page.locator('tbody')).toContainText('Đã xuất');
  await page.goto('/gate?tab=history');
  await page.getByPlaceholder('Tìm container, phương tiện, tài xế...').fill('CSQU3054383');
  await page.getByRole('button', { name: 'Chi tiết', exact: true }).first().click();
  page.once('dialog', (d) => d.accept());
  await page.getByRole('button', { name: 'Hủy giao dịch', exact: true }).click();
  await expect(page.getByRole('dialog')).toContainText('Đã hủy');
  await page.goto('/containers?search=CSQU3054383');
  await expect(page.locator('tbody')).toContainText('A-11-D-1');
  await expect(page.locator('tbody')).toContainText('Chờ xuất');
  await page.goto('/inventory');
  const download = page.waitForEvent('download');
  await page.getByRole('button', { name: 'Xuất báo cáo', exact: true }).click();
  expect((await download).suggestedFilename()).toMatch(/\.xlsx$/);
});

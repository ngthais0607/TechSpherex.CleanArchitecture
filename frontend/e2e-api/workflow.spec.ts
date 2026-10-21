import { test, expect, containerNumber } from './fixtures/api-fixture';
test('gate-in35 -> assign DO002 -> gate-out35 and reload history', async ({ rolePage: page }) => {
  const number = containerNumber(35);
  await page.getByRole('link', { name: 'Cổng nhập / xuất', exact: true }).click();
  await page.locator('#gate-number').fill(number);
  await page.locator('#gate-block').selectOption('VB-01');
  await page.locator('#gate-vehicle').fill('E2E35-IN');
  await expect(page.getByRole('button', { name: 'Ghi nhận giao dịch', exact: true })).toBeEnabled();
  const gateIn = page.waitForResponse((r) => r.url().endsWith('/api/gate/in'));
  await page.getByRole('button', { name: 'Ghi nhận giao dịch', exact: true }).click();
  expect((await gateIn).status()).toBe(201);
  await page.getByRole('link', { name: 'Lệnh giao', exact: true }).click();
  await page.getByRole('textbox', { name: 'Tìm lệnh giao', exact: true }).fill('DO-2026-002');
  await page.getByRole('button', { name: 'DO-2026-002', exact: true }).click();
  await page.locator('#assign-numbers').fill(number);
  const assign = page.waitForResponse((r) => r.url().endsWith('/assign-containers'));
  await page.getByRole('button', { name: 'Gán container đã chọn', exact: true }).click();
  expect((await assign).status()).toBe(200);
  await page.locator('dialog button[aria-label="Đóng"]').click();
  await page.getByRole('link', { name: 'Cổng nhập / xuất', exact: true }).click();
  await page.locator('#gate-type').selectOption('GateOut');
  await page.locator('#gate-number').fill(number);
  await page.locator('#gate-order').fill('DO-2026-002');
  await page.locator('#gate-vehicle').fill('E2E35-OUT');
  await expect(page.getByRole('button', { name: 'Ghi nhận giao dịch', exact: true })).toBeEnabled();
  const gateOut = page.waitForResponse((r) => r.url().endsWith('/api/gate/out'));
  await page.getByRole('button', { name: 'Ghi nhận giao dịch', exact: true }).click();
  expect((await gateOut).status()).toBe(204);
  await page.reload();
  await page.getByRole('textbox', { name: 'Tìm xe hoặc tài xế', exact: true }).fill('E2E35');
  await expect(page.locator('tbody tr').filter({ hasText: number })).toHaveCount(2);
});
test('expired reservation is reassigned by backend', async ({ rolePage: page, api }) => {
  const token = await page.evaluate(
    () => JSON.parse(sessionStorage.getItem('snp-api-session')!).accessToken,
  );
  const number = 'E2E-REASSIGN-' + Date.now();
  const created = await api.post('/api/delivery-orders', {
    headers: { Authorization: 'Bearer ' + token },
    data: {
      orderNumber: number,
      lineOperator: 'MAEU',
      customerTaxCode: '0102030405',
      customerName: 'Samsung Electronics Vietnam',
      expiryDate: '2099-01-01T00:00:00Z',
      outboundVessel: 'E2E Vessel',
      items: [{ size: 'Ft20', type: 'Dry', quantity: 1 }],
    },
  });
  expect(created.status()).toBe(201);
  const { id } = await created.json();
  await page.goto('/delivery-orders?order=' + id);
  await page.locator('#assign-numbers').fill(containerNumber(10));
  const assign = page.waitForResponse((r) => r.url().endsWith('/assign-containers'));
  await page.getByRole('button', { name: 'Gán container đã chọn', exact: true }).click();
  expect((await assign).status()).toBe(200);
  await page.reload();
  await expect(
    page.locator('dialog tbody tr').filter({ hasText: containerNumber(10) }),
  ).toContainText('Reserved');
});
test('virtual -> physical and reload, resize controls', async ({ rolePage: page }) => {
  await page.getByRole('link', { name: 'Sơ đồ bãi', exact: true }).click();
  await page.getByRole('button', { name: /^VB-01 ·/ }).click();
  const row = page.locator('tbody tr').filter({ hasText: containerNumber(17) });
  await row.getByRole('button', { name: 'Phân vị trí', exact: true }).click();
  await page.locator('#yard-move-form-targetBlock').selectOption({ label: 'B · Standard Block B' });
  await page.locator('#yard-move-form-bay').fill('1');
  const moved = page.waitForResponse((r) => r.url().endsWith('/place'));
  await page.getByRole('button', { name: 'Xác nhận di chuyển', exact: true }).click();
  expect((await moved).status()).toBe(204);
  await page.reload();
  await page
    .locator('.yard-cell.occupied')
    .filter({ hasText: containerNumber(17) })
    .waitFor();
  await expect(page.getByRole('button', { name: 'Mở rộng bãi', exact: true })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Thu hẹp bãi', exact: true })).toBeVisible();
});

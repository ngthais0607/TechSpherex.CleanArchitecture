import { test, expect } from './fixtures/api-fixture';
test.describe('yard reports', () => {
  test.use({ role: 'yard' });
  test('dashboard and inventory match API after reload without forbidden calls', async ({
    rolePage: page,
    api,
  }) => {
    const forbidden: string[] = [];
    page.on('request', (request) => {
      if (
        request.url().includes('/expiring-orders') ||
        request.url().includes('/api/gate/transactions')
      )
        forbidden.push(request.url());
    });
    await page.reload();
    const token = await page.evaluate(
      () => JSON.parse(sessionStorage.getItem('snp-api-session')!).accessToken,
    );
    const headers = { Authorization: 'Bearer ' + token };
    const summary = await (await api.get('/api/reports/summary', { headers })).json();
    await expect(page.getByTestId('yard-total')).toHaveText(String(summary.totalContainersInYard));
    await page.getByRole('link', { name: 'Tồn bãi', exact: true }).click();
    const inventory = await (await api.get('/api/reports/yard-inventory', { headers })).json();
    await expect(page.getByTestId('inventory-total')).toHaveText(String(inventory.totalContainers));
    const download = page.waitForEvent('download');
    await page.getByRole('button', { name: 'Xuất Excel', exact: true }).click();
    expect((await download).suggestedFilename()).toMatch(/\.xlsx$/);
    expect(forbidden).toEqual([]);
  });
});

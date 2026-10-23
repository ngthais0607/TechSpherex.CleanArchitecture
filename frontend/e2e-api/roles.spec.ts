import { test, expect, Role } from './fixtures/api-fixture';
for (const role of ['admin', 'yard', 'order', 'gate'] as Role[]) {
  test.describe(role, () => {
    test.use({ role });
    test('role menus and direct URL/API policies', async ({ rolePage: page, api }) => {
      const hasGate = role === 'admin' || role === 'gate',
        hasYard = role !== 'order';
      await expect(page.getByRole('link', { name: 'Cổng nhập / xuất', exact: true })).toHaveCount(
        hasGate ? 1 : 0,
      );
      await expect(page.getByRole('link', { name: 'Sơ đồ bãi', exact: true })).toHaveCount(
        hasYard ? 1 : 0,
      );
      if (!hasGate) {
        await page.goto('/gate');
        await expect(page).toHaveURL(/\/dashboard$/);
      }
      const token = await page.evaluate(
        () => JSON.parse(sessionStorage.getItem('snp-api-session')!).accessToken,
      );
      if (!hasYard)
        expect(
          (
            await api.get('/api/yard/blocks', { headers: { Authorization: 'Bearer ' + token } })
          ).status(),
        ).toBe(403);
      if (role === 'yard')
        expect(
          (
            await api.get('/api/reports/expiring-orders', {
              headers: { Authorization: 'Bearer ' + token },
            })
          ).status(),
        ).toBe(403);
      if (role === 'gate') {
        await page.getByRole('link', { name: 'Sơ đồ bãi', exact: true }).click();
        await page.getByRole('heading', { name: 'Sơ đồ bãi', exact: true }).waitFor();
        await expect(page.getByRole('button', { name: /^Mở rộng bãi$|^Thu hẹp bãi$/ })).toHaveCount(
          0,
        );
      }
    });
  });
}

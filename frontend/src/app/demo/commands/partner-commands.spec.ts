import { describe, it, expect } from 'vitest';
import { createDemoSnapshot } from '../seed';
import { saveCustomer, saveOperator, deactivatePartner } from './partner-commands';
describe('partners', () => {
  it('rejects duplicate tax codes and preserves identity when referenced', () => {
    const s = createDemoSnapshot();
    const c = s.customers[0];
    expect(saveCustomer(s, { ...c, id: undefined }).ok).toBe(false);
    expect(saveCustomer(s, { ...c, taxCode: '9999999999' }).ok).toBe(false);
    expect(saveCustomer(s, { ...c, name: 'Tên mới' }).ok).toBe(true);
    expect(s.orders[0].customerId).toBe(c.id);
  });
  it('blocks deactivation while inventory or active orders remain', () => {
    const s = createDemoSnapshot();
    expect(deactivatePartner(s, 'operator', s.operators[0].id, new Date()).ok).toBe(false);
    expect(deactivatePartner(s, 'customer', s.customers[0].id, new Date()).ok).toBe(false);
  });
});

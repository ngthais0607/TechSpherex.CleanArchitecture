import { describe, expect, it } from 'vitest';
import { can } from './permissions';
describe('role policies', () => {
  it('matches separate read and write permissions', () => {
    expect(can('readOrders', ['GateClerk'])).toBe(true);
    expect(can('manageOrders', ['GateClerk'])).toBe(false);
    expect(can('readYard', ['OrderClerk'])).toBe(false);
    expect(can('readExpiringOrders', ['YardPlanner'])).toBe(false);
    expect(can('manageGate', ['Admin'])).toBe(true);
    expect(can('readContainers', ['unknown'])).toBe(false);
  });
});

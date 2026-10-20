import { describe, it, expect } from 'vitest';
import { createDemoSnapshot } from './seed';
import { placeCheck } from './rules/yard-rules';
import { gateOut, cancelGate } from './commands/gate-commands';
import { assertSnapshot } from './storage';
import { dayKey } from './rules/container-rules';
import { expire, reserved, orderStatus } from './commands/order-commands';
import { report, trend } from './queries/report';
const now = new Date('2026-10-20T05:00:00Z');
describe('invalid placement and persistence boundaries', () => {
  it('rejects multi-letter rows and fractional tiers', () => {
    const s = createDemoSnapshot(now),
      n = s.containers[4].number;
    expect(
      placeCheck(s, n, { kind: 'physical', blockId: 'A', bay: 11, row: 'AA', tier: 1 }).ok,
    ).toBe(false);
    expect(
      placeCheck(s, n, { kind: 'physical', blockId: 'A', bay: 11, row: 'D', tier: 1.5 }).ok,
    ).toBe(false);
  });
  it('rejects corrupt booking references and unsupported snapshot versions', () => {
    const s = createDemoSnapshot(now);
    s.bookings[0].lifecycleId = 'missing';
    expect(assertSnapshot(s).ok).toBe(false);
    expect(assertSnapshot({ ...createDemoSnapshot(now), schemaVersion: 2 }).ok).toBe(false);
  });
});
describe('cancellation safety', () => {
  it('refuses to restore a shipped container into a now occupied slot', () => {
    const s = createDemoSnapshot(now),
      life = s.lifecycles.find((l) => l.number === s.containers[4].number && !l.outAt)!;
    const original = structuredClone(life.position),
      number = life.number;
    const r = gateOut(
      s,
      {
        number,
        orderId: s.orders[0].id,
        vehicleType: 'Truck',
        vehicleNo: '51C',
        driver: 'An',
        grade: 'A',
        condition: 'Bình thường',
      },
      now,
    );
    expect(r.ok).toBe(true);
    if (!r.ok) return;
    const other = s.lifecycles.find((l) => !l.outAt && l.position?.kind === 'virtual')!;
    other.position = original;
    const before = JSON.stringify(s);
    expect(cancelGate(s, r.value, now).ok).toBe(false);
    expect(JSON.stringify(s)).toBe(before);
  });
  it('expires reservations after the final depot day', () => {
    const s = createDemoSnapshot(now),
      b = s.bookings.find((b) => b.status === 'Reserved')!;
    s.orders.find((o) => o.id === b.orderId)!.expiry = '2026-10-20';
    expect(reserved(s, b.number, new Date('2026-10-20T16:59:59Z'))).toBeDefined();
    expire(s, new Date('2026-10-20T17:00:00Z'));
    expect(b.status).toBe('Expired');
  });
});
describe('report boundaries', () => {
  it('includes both depot dates and excludes cancelled transactions', () => {
    const s = createDemoSnapshot(now);
    s.gateTransactions = s.gateTransactions.slice(0, 2);
    s.gateTransactions[0].at = '2026-10-19T17:00:00Z';
    s.gateTransactions[1].at = '2026-10-20T16:59:59Z';
    s.gateTransactions[1].status = 'Cancelled';
    const r = report(s, { from: '2026-10-20', to: '2026-10-20' }, now);
    expect(r.in + r.out).toBe(1);
    expect(trend(2, 0)).toBe('Chưa có cơ sở so sánh');
    expect(trend(0, 0)).toBe('0%');
  });
});

import { describe, expect, it } from 'vitest';
import { createDemoSnapshot, containerNumber } from './seed';
import { dayKey, validContainerNumber, normalizeNumber } from './rules/container-rules';
import { assign, createOrder, release } from './commands/order-commands';
import { gateIn, gateOut, cancelGate, editGate } from './commands/gate-commands';
import { placeCheck, move, resize } from './rules/yard-rules';
import { inventory, dashboard } from './queries/analytics';
import { assertSnapshot } from './storage';

const now = new Date('2026-10-20T05:00:00Z');
describe('shared demo data', () => {
  it('has valid ISO numbers and consistent seed references', () => {
    const s = createDemoSnapshot(now);
    expect(s.containers.every((c) => validContainerNumber(c.number))).toBe(true);
    expect(new Set(s.containers.map((c) => c.type)).size).toBe(7);
    expect(assertSnapshot(s).ok).toBe(true);
    expect(
      s.containers.filter(
        (c) =>
          new Set(
            s.bookings
              .filter((b) => b.number === c.number)
              .map((b) => s.orders.find((o) => o.id === b.orderId)?.customerId),
          ).size >= 2,
      ).length,
    ).toBeGreaterThanOrEqual(3);
  });
  it('normalizes pasted codes and computes the depot day at UTC+7', () => {
    expect(normalizeNumber(' msku 482190 4 ')).toBe('MSKU4821904');
    expect(dayKey(new Date('2026-10-20T16:59:59Z'))).toBe('2026-10-20');
    expect(dayKey(new Date('2026-10-20T17:00:00Z'))).toBe('2026-10-21');
    expect(validContainerNumber('CSQU3054383')).toBe(true);
    expect(validContainerNumber('CSQU3054384')).toBe(false);
  });
  it('rejects structurally valid JSON with conflicting occupancy', () => {
    const s = createDemoSnapshot(now);
    const active = s.lifecycles.filter((l) => !l.outAt && l.position?.kind === 'physical');
    active[1].position = structuredClone(active[0].position);
    expect(assertSnapshot(s).ok).toBe(false);
  });
});
describe('yard rules', () => {
  it('requires both slots of a 40ft to be supported and refuses 20ft over 40ft', () => {
    const s = createDemoSnapshot(now);
    s.lifecycles = [];
    const [small, large] = [
      s.containers.find((c) => c.size === 20)!,
      s.containers.find((c) => c.size === 40)!,
    ];
    const p = { kind: 'physical' as const, blockId: 'A', bay: 2, row: 'A', tier: 2 };
    expect(placeCheck(s, large.number, p).ok).toBe(false);
    s.lifecycles.push({
      id: 'test',
      number: large.number,
      inAt: now.toISOString(),
      position: { ...p, tier: 1 },
    });
    expect(placeCheck(s, small.number, { ...p, bay: 1 }).ok).toBe(false);
  });
  it('blocks shrink and movement when occupied or covered', () => {
    const s = createDemoSnapshot(now);
    expect(resize(s, { blockId: 'A', oddBays: 1, rows: 1, tiers: 1 }).ok).toBe(false);
    const active = s.lifecycles.filter((l) => !l.outAt && l.position?.kind === 'physical');
    expect(move(s, { number: active[0].number, target: active[1].position! }, now).ok).toBe(false);
  });
});
describe('bookings and gate', () => {
  it('assigns batches atomically and keeps release history', () => {
    const s = createDemoSnapshot(now);
    const order = s.orders.find((o) => o.number === 'DO-2610-001')!;
    const eligible = s.containers.filter(
      (c) =>
        c.operatorId === order.operatorId &&
        s.lifecycles.some((l) => l.number === c.number && !l.outAt) &&
        !s.bookings.some((b) => b.number === c.number && b.status === 'Reserved'),
    );
    const wrong = s.containers.find((c) => c.operatorId !== order.operatorId)!;
    const before = JSON.stringify(s);
    expect(assign(s, order.id, [eligible[0].number, wrong.number], now).ok).toBe(false);
    expect(JSON.stringify(s)).toBe(before);
    expect(assign(s, order.id, [eligible[0].number], now).ok).toBe(true);
    const b = s.bookings.find((b) => b.number === eligible[0].number && b.status === 'Reserved')!;
    expect(release(s, b.id, now).ok).toBe(true);
    expect(s.bookings.find((x) => x.id === b.id)?.status).toBe('Cancelled');
  });
  it('rejects duplicate lines and invalid quantities in an order', () => {
    const s = createDemoSnapshot(now);
    const input = {
      number: 'DO-NEW',
      customerId: s.customers[0].id,
      operatorId: s.operators[0].id,
      expiry: '2026-10-30',
      voyage: 'A',
      lines: [{ type: 'Dry' as const, size: 20 as const, quantity: 0 }],
    };
    expect(createOrder(s, input, now).ok).toBe(false);
  });
  it('round trips entry, direct shipment and cancellation without losing lifecycle', () => {
    const s = createDemoSnapshot(now);
    const c = s.containers.find(
      (c) => !s.lifecycles.some((l) => l.number === c.number) && c.operatorId === 'maersk',
    )!;
    const order = s.orders.find((o) => o.number === 'DO-2610-001')!;
    const input = {
      number: c.number,
      vehicleType: 'Truck' as const,
      vehicleNo: '51C-12345',
      driver: 'An',
      grade: 'A' as const,
      condition: 'Bình thường',
      target: { kind: 'virtual' as const, blockId: 'VB-01' },
    };
    expect(gateIn(s, input, now).ok).toBe(true);
    expect(gateOut(s, { ...input, orderId: order.id }, now).ok).toBe(true);
    const last = s.gateTransactions.at(-1)!;
    expect(cancelGate(s, last.id, now).ok).toBe(true);
    expect(s.lifecycles.find((l) => l.number === c.number && !l.outAt)?.position).toEqual(
      input.target,
    );
    expect(s.bookings.find((b) => b.number === c.number)?.status).toBe('Reserved');
  });
});
describe('metrics', () => {
  it('excludes virtual blocks from physical capacity and cancelled movements from traffic', () => {
    const s = createDemoSnapshot(now);
    const d = dashboard(s, now);
    expect(d.inYard).toBe(s.lifecycles.filter((l) => !l.outAt).length);
    expect(d.usedTeu).toBe(
      s.lifecycles
        .filter((l) => !l.outAt && l.position?.kind === 'physical')
        .reduce(
          (v, l) => v + (s.containers.find((c) => c.number === l.number)!.size === 40 ? 2 : 1),
          0,
        ),
    );
  });
  it('uses elapsed 72 hours for long dwell and never includes exited visits', () => {
    const s = createDemoSnapshot(now);
    s.lifecycles = [
      {
        id: '1',
        number: s.containers[0].number,
        inAt: new Date(now.getTime() - 72 * 3600000).toISOString(),
        position: { kind: 'virtual', blockId: 'VB-01' },
      },
    ];
    expect(inventory(s, {}, now).longStay).toBe(1);
    expect(inventory(s, {}, new Date(now.getTime() - 1)).longStay).toBe(0);
  });
});

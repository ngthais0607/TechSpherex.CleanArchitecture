import { DepotSnapshot, Result } from '../core/models/entities';
import { fail, ok, validContainerNumber } from './rules/container-rules';
import { slots, placeCheck } from './rules/yard-rules';
export const STORAGE_KEY = 'snp-depot-demo:v1';
export function assertSnapshot(raw: unknown): Result<DepotSnapshot> {
  try {
    const s = raw as DepotSnapshot;
    if (
      s.schemaVersion !== 1 ||
      ![
        'containers',
        'customers',
        'operators',
        'orders',
        'bookings',
        'lifecycles',
        'blocks',
        'gateTransactions',
        'movements',
      ].every((k) => Array.isArray((s as unknown as Record<string, unknown>)[k]))
    )
      return fail('DEMO_SCHEMA', 'Bản lưu không tương thích. Hãy khôi phục dữ liệu mẫu.');
    if (
      s.containers.some(
        (c) => !validContainerNumber(c.number) || !s.operators.some((o) => o.id === c.operatorId),
      )
    )
      throw Error();
    if (new Set(s.containers.map((c) => c.number)).size !== s.containers.length) throw Error();
    const used = new Set<string>(),
      active = new Set<string>(),
      holds = new Set<string>();
    for (const l of s.lifecycles) {
      if (!s.containers.some((c) => c.number === l.number)) throw Error();
      if (l.outAt) continue;
      if (active.has(l.number)) throw Error();
      active.add(l.number);
      if (!l.position || !s.blocks.some((b) => b.id === l.position!.blockId)) throw Error();
      if (l.position.kind === 'physical')
        for (const slot of slots(
          l.position,
          s.containers.find((c) => c.number === l.number)!.size,
        )) {
          const key = `${l.position.blockId}/${l.position.row}/${l.position.tier}/${slot}`;
          if (used.has(key)) throw Error();
          used.add(key);
        }
    }
    for (const b of s.bookings) {
      if (
        !s.orders.some((o) => o.id === b.orderId && o.lines.some((l) => l.id === b.lineId)) ||
        !s.lifecycles.some((l) => l.id === b.lifecycleId && l.number === b.number)
      )
        throw Error();
      if (b.status === 'Reserved') {
        if (holds.has(b.number)) throw Error();
        holds.add(b.number);
      }
    }
    for (const l of s.lifecycles.filter((l) => !l.outAt)) {
      if (!placeCheck(s, l.number, l.position!).ok) throw Error();
    }
    return ok(s);
  } catch {
    return fail(
      'DEMO_CORRUPT',
      'Dữ liệu lưu bị hỏng hoặc có quan hệ không hợp lệ. Hãy khôi phục dữ liệu mẫu.',
    );
  }
}

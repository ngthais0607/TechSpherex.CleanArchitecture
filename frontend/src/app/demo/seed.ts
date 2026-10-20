import { DepotSnapshot, TYPES, ContainerType } from '../core/models/entities';
import { checksum, dayKey } from './rules/container-rules';
import { suggest } from './rules/yard-rules';
export function containerNumber(prefix: string, serial: number) {
  const raw = prefix + String(serial).padStart(6, '0');
  return raw + checksum(raw);
}
export function createDemoSnapshot(now = new Date()): DepotSnapshot {
  const at = (days: number, hours = 0) =>
    new Date(now.getTime() - days * 86400000 - hours * 3600000).toISOString();
  const future = (days: number) => dayKey(new Date(now.getTime() + days * 86400000));
  const s: DepotSnapshot = {
    schemaVersion: 1,
    revision: 0,
    seededAt: now.toISOString(),
    containers: [],
    customers: [],
    operators: [],
    orders: [],
    bookings: [],
    lifecycles: [],
    blocks: [],
    gateTransactions: [],
    movements: [],
  };
  s.operators = [
    {
      id: 'maersk',
      code: 'MSK',
      name: 'Maersk',
      contact: 'depot@maersk.com',
      isActive: true,
      color: '#008eab',
    },
    {
      id: 'evergreen',
      code: 'EGL',
      name: 'Evergreen',
      contact: 'depot@evergreen.com',
      isActive: true,
      color: '#059669',
    },
    {
      id: 'cma',
      code: 'CMA',
      name: 'CMA CGM',
      contact: 'depot@cma.com',
      isActive: true,
      color: '#6958db',
    },
    {
      id: 'oocl',
      code: 'OOC',
      name: 'OOCL',
      contact: 'depot@oocl.com',
      isActive: true,
      color: '#e58b32',
    },
  ];
  s.customers = ['An Phát', 'Hải Long', 'Việt Tín', 'Nam Sơn'].map((name, i) => ({
    id: 'customer-' + i,
    taxCode: '031234567' + i,
    name: 'Công ty ' + (i % 2 ? 'CP ' : 'TNHH ') + name,
    email: `contact${i}@example.vn`,
    phone: '090123456' + i,
    address: 'Thành phố Hồ Chí Minh',
    isActive: true,
  }));
  s.blocks = [
    { id: 'A', name: 'Block A', oddBays: 6, rows: 4, tiers: 5, virtual: false },
    { id: 'B', name: 'Block B', oddBays: 5, rows: 3, tiers: 4, virtual: false },
    { id: 'C', name: 'Block C', oddBays: 7, rows: 5, tiers: 3, virtual: false },
    { id: 'D', name: 'Block D', oddBays: 4, rows: 2, tiers: 2, virtual: false },
    { id: 'VB-01', name: 'Block ảo VB-01', oddBays: 0, rows: 0, tiers: 0, virtual: true },
  ];
  for (let i = 0; i < 32; i++) {
    const op = s.operators[i % 4],
      size = i % 3 === 0 ? 40 : 20;
    s.containers.push({
      number: containerNumber(['MSKU', 'EGHU', 'CMAU', 'OOLU'][i % 4], 603210 + i),
      type: i < 12 || i >= 24 ? 'Dry' : TYPES[(i - 12) % 7],
      size,
      iso: size === 40 ? '45G1' : '22G1',
      ownerCode: ['MSKU', 'EGHU', 'CMAU', 'OOLU'][i % 4],
      operatorId: op.id,
      grade: (['A', 'B', 'C'] as const)[i % 3],
      condition: i % 8 === 0 ? 'Móp' : 'Bình thường',
      maxWeight: 30480,
      tareWeight: size === 40 ? 3800 : 2200,
      manufactureMonth: '2021-06',
    });
    if (i < 24) {
      const b = s.blocks[Math.floor(i / 6)],
        slot = i % 6;
      const result = suggest(s, s.containers[i].number, slot === 5 ? 'VB-01' : b.id);
      if (!result.ok) throw Error(result.error.message);
      const p = result.value;
      const lifeId = 'life-' + i,
        inAt = at(i % 9, i % 3);
      s.lifecycles.push({ id: lifeId, number: s.containers[i].number, inAt, position: p });
      s.gateTransactions.push({
        id: 'in-' + i,
        number: s.containers[i].number,
        direction: 'In',
        at: inAt,
        status: 'Active',
        lifecycleId: lifeId,
        vehicleType: 'Truck',
        vehicleNo: '51C-123.' + String(i).padStart(2, '0'),
        driver: 'Nguyễn Văn An',
        grade: s.containers[i].grade,
        condition: s.containers[i].condition,
        target: p,
        createdBy: 'Nguyễn Minh',
        previousGrade: 'A',
        previousCondition: 'Bình thường',
        changes: [],
      });
    }
  }
  for (let i = 0; i < 5; i++)
    s.orders.push({
      id: 'order-' + i,
      number: 'DO-2610-00' + (i + 1),
      customerId: s.customers[i % 4].id,
      operatorId: s.operators[i % 4].id,
      expiry: future([10, 2, -1, 15, 0][i]),
      voyage: ['Tàu A 0123E', 'Tàu B 0456W', 'Tàu C 0789E', 'Tàu D 0234E', 'Tàu E 0312W'][i],
      lines: [
        { id: 'line-' + i + '-20', type: 'Dry', size: 20, quantity: 10 },
        { id: 'line-' + i + '-40', type: 'Dry', size: 40, quantity: 5 },
      ],
    });
  for (const i of [0, 1, 2, 3]) {
    const c = s.containers[i],
      order = s.orders[i];
    s.bookings.push({
      id: 'booking-' + i,
      number: c.number,
      orderId: order.id,
      lineId: `line-${i}-${c.size}`,
      lifecycleId: 'life-' + i,
      status: i === 2 ? 'Expired' : 'Reserved',
      createdAt: at(1),
    });
  }
  // Three containers each have two completed visits for different customers.
  for (let i = 0; i < 3; i++)
    for (let visit = 0; visit < 2; visit++) {
      const c = s.containers[24 + i],
        oid = `history-order-${i}-${visit}`,
        lid = `history-life-${i}-${visit}`,
        inAt = at(20 - visit * 7),
        outAt = at(18 - visit * 7),
        lineId = oid + '-line';
      s.orders.push({
        id: oid,
        number: `DO-H${i}${visit}`,
        customerId: s.customers[visit].id,
        operatorId: c.operatorId,
        expiry: future(-10 + visit * 7),
        voyage: 'Tàu lịch sử',
        lines: [{ id: lineId, type: c.type, size: c.size, quantity: 1 }],
      });
      s.lifecycles.push({ id: lid, number: c.number, inAt, outAt });
      s.bookings.push({
        id: oid + '-booking',
        number: c.number,
        orderId: oid,
        lineId,
        lifecycleId: lid,
        status: 'Shipped',
        createdAt: inAt,
        shippedAt: outAt,
      });
      for (const direction of ['In', 'Out'] as const)
        s.gateTransactions.push({
          id: lid + direction,
          number: c.number,
          direction,
          at: direction === 'In' ? inAt : outAt,
          status: 'Active',
          lifecycleId: lid,
          orderId: direction === 'Out' ? oid : undefined,
          vehicleType: 'Truck',
          vehicleNo: '51C-456.78',
          driver: 'Trần Văn Bình',
          grade: c.grade,
          condition: c.condition,
          createdBy: 'Nguyễn Minh',
          previousGrade: c.grade,
          previousCondition: c.condition,
          previousPosition: { kind: 'virtual', blockId: 'VB-01' },
          changes: [],
        });
    }
  s.gateTransactions.sort((a, b) => a.at.localeCompare(b.at));
  return s;
}

import { DepotSnapshot, GateInput, GateCheck, Result } from '../../core/models/entities';
import {
  activeLife,
  dayKey,
  fail,
  normalizeNumber,
  ok,
  uid,
  validContainerNumber,
} from '../rules/container-rules';
import { isBlocked, placeCheck } from '../rules/yard-rules';
import { eligible, reserved } from './order-commands';
export function evaluateGate(
  s: DepotSnapshot,
  input: GateInput,
  direction: 'In' | 'Out',
  now: Date,
): GateCheck[] {
  const n = normalizeNumber(input.number),
    c = s.containers.find((c) => c.number === n),
    l = activeLife(s, n);
  const check = (
    code: string,
    label: string,
    ready: boolean,
    valid: boolean,
    detail: string,
  ): GateCheck => ({
    code,
    label,
    state: !ready ? 'wait' : valid ? 'pass' : 'error',
    detail: !ready ? 'Chờ nhập thông tin' : valid ? 'Đã đối chiếu hợp lệ' : detail,
  });
  const checks = [
    check(
      'C1',
      'Định dạng và số kiểm tra',
      !!n,
      validContainerNumber(n),
      'Số container không hợp lệ.',
    ),
    check('G1', 'Container có trong danh mục', !!n, !!c, 'Chưa đăng ký container.'),
    check(
      'G2',
      'Trạng thái container',
      !!c,
      direction === 'In' ? !l : !!l,
      direction === 'In' ? 'Container đã ở trong bãi.' : 'Container chưa ở trong bãi.',
    ),
    check(
      'DEMO_VEHICLE',
      'Phương tiện & người điều khiển',
      !!input.vehicleNo || !!input.driver,
      !!input.vehicleNo.trim() && !!input.driver.trim(),
      'Nhập đủ biển số và tài xế.',
    ),
  ];
  if (direction === 'In') {
    const r = c && input.target ? placeCheck(s, n, input.target) : null;
    checks.push(
      check(
        'Y3',
        'Vị trí xếp',
        !!input.target,
        !!r?.ok,
        r && !r.ok ? r.error.message : 'Chọn vị trí xếp.',
      ),
    );
  } else {
    const b = reserved(s, n, now);
    const r = b
      ? b.orderId === input.orderId
        ? ok('')
        : fail('G4', 'Container đã giữ chỗ cho lệnh khác.')
      : eligible(s, input.orderId || '', n, now);
    checks.push(check('G3', 'Lệnh giao', !!input.orderId, r.ok, r.ok ? '' : r.error.message));
    const o = s.orders.find((o) => o.id === input.orderId);
    if (b && o && o.expiry < dayKey(now))
      checks[checks.length - 1] = check('G3', 'Lệnh giao', true, false, 'Lệnh đã hết hạn.');
    checks.push(
      check(
        'Y5',
        'Container không bị đè',
        !!l,
        !isBlocked(s, n),
        'Cần di chuyển container tầng trên trước.',
      ),
    );
  }
  return checks;
}
function baseCheck(
  s: DepotSnapshot,
  input: GateInput,
  direction: 'In' | 'Out',
  now: Date,
): Result<void> {
  const checks = evaluateGate(s, input, direction, now);
  const bad = checks.find((c) => c.state !== 'pass');
  if (bad) return fail(bad.code, bad.detail);
  if (
    !['A', 'B', 'C'].includes(input.grade) ||
    !['Bình thường', 'Móp', 'Méo', 'Cong', 'Hư hỏng khác'].includes(input.condition)
  )
    return fail('DEMO_FIELDS', 'Chọn đủ phân loại và tình trạng.');
  return ok(undefined);
}
export function gateIn(s: DepotSnapshot, input: GateInput, now: Date): Result<string> {
  input = { ...input, number: normalizeNumber(input.number) };
  const r = baseCheck(s, input, 'In', now);
  if (!r.ok) return r;
  const c = s.containers.find((c) => c.number === input.number)!,
    id = uid(),
    at = now.toISOString();
  s.lifecycles.push({ id, number: c.number, inAt: at, position: input.target });
  const tx = uid();
  s.gateTransactions.push({
    ...input,
    id: tx,
    direction: 'In',
    at,
    status: 'Active',
    lifecycleId: id,
    createdBy: 'Nguyễn Minh',
    previousGrade: c.grade,
    previousCondition: c.condition,
    changes: [],
  });
  c.grade = input.grade;
  c.condition = input.condition;
  return ok(tx);
}
export function gateOut(s: DepotSnapshot, input: GateInput, now: Date): Result<string> {
  input = { ...input, number: normalizeNumber(input.number) };
  const r = baseCheck(s, input, 'Out', now);
  if (!r.ok) return r;
  const life = activeLife(s, input.number)!,
    c = s.containers.find((c) => c.number === input.number)!,
    at = now.toISOString();
  let b = reserved(s, input.number, now);
  if (!b) {
    const line = eligible(s, input.orderId!, c.number, now);
    if (!line.ok) return line;
    b = {
      id: uid(),
      number: c.number,
      orderId: input.orderId!,
      lineId: line.value,
      lifecycleId: life.id,
      status: 'Reserved',
      createdAt: at,
    };
    s.bookings.push(b);
  }
  b.status = 'Shipped';
  b.shippedAt = at;
  life.outAt = at;
  const previousPosition = life.position;
  delete life.position;
  const id = uid();
  s.gateTransactions.push({
    ...input,
    id,
    direction: 'Out',
    at,
    status: 'Active',
    lifecycleId: life.id,
    createdBy: 'Nguyễn Minh',
    previousPosition,
    previousGrade: c.grade,
    previousCondition: c.condition,
    changes: [],
  });
  c.grade = input.grade;
  c.condition = input.condition;
  return ok(id);
}
export function cancelGate(s: DepotSnapshot, id: string, now: Date): Result<void> {
  const tx = s.gateTransactions.find((t) => t.id === id);
  if (!tx || tx.status === 'Cancelled') return fail('G9', 'Giao dịch không thể hủy.');
  const latest = s.gateTransactions
    .filter((t) => t.number === tx.number && t.status === 'Active')
    .at(-1);
  if (latest?.id !== id) return fail('G9', 'Chỉ hủy được giao dịch gần nhất.');
  const l = s.lifecycles.find((l) => l.id === tx.lifecycleId)!,
    c = s.containers.find((c) => c.number === tx.number)!;
  if (tx.direction === 'In') {
    if (
      reserved(s, c.number, now) ||
      s.movements.some((m) => m.number === c.number && m.at >= tx.at)
    )
      return fail('G9', 'Gỡ đặt chỗ hoặc xử lý di chuyển trước khi hủy nhập.');
    s.lifecycles = s.lifecycles.filter((x) => x.id !== l.id);
  } else {
    if (!tx.previousPosition) return fail('G9', 'Không tìm thấy vị trí trước khi xuất.');
    const r = placeCheck(s, c.number, tx.previousPosition);
    if (!r.ok) return fail('G9', r.error.message);
    l.position = tx.previousPosition;
    delete l.outAt;
    const b = s.bookings.find(
      (b) => b.lifecycleId === l.id && b.orderId === tx.orderId && b.status === 'Shipped',
    );
    if (b) {
      b.status =
        s.orders.find((o) => o.id === b.orderId)!.expiry < dayKey(now) ? 'Expired' : 'Reserved';
      delete b.shippedAt;
    }
  }
  tx.status = 'Cancelled';
  c.grade = tx.previousGrade;
  c.condition = tx.previousCondition;
  return ok(undefined);
}
export function editGate(
  s: DepotSnapshot,
  id: string,
  input: Pick<GateInput, 'vehicleNo' | 'vehicleType' | 'driver' | 'grade' | 'condition'>,
  now: Date,
): Result<void> {
  const tx = s.gateTransactions.find((t) => t.id === id);
  if (!tx || tx.status !== 'Active') return fail('G8', 'Giao dịch đã hủy không được sửa.');
  if (!input.vehicleNo.trim() || !input.driver.trim())
    return fail('G8', 'Nhập đủ phương tiện và tài xế.');
  for (const field of ['vehicleNo', 'vehicleType', 'driver', 'grade', 'condition'] as const) {
    if (tx[field] !== input[field])
      tx.changes.push({
        at: now.toISOString(),
        user: 'Nguyễn Minh',
        field,
        oldValue: tx[field],
        newValue: input[field],
      });
  }
  Object.assign(tx, input);
  const latest = s.gateTransactions
    .filter((t) => t.number === tx.number && t.status === 'Active')
    .at(-1);
  if (latest?.id === id) {
    const c = s.containers.find((c) => c.number === tx.number)!;
    c.grade = input.grade;
    c.condition = input.condition;
  }
  return ok(undefined);
}

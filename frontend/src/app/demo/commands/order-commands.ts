import { Booking, DepotSnapshot, OrderInput, Result } from '../../core/models/entities';
import { activeLife, dayKey, fail, normalizeNumber, ok, uid } from '../rules/container-rules';
export function orderStatus(s: DepotSnapshot, id: string, now: Date): string {
  const o = s.orders.find((o) => o.id === id);
  if (!o) return 'Không tìm thấy';
  const shipped = s.bookings.filter((b) => b.orderId === id && b.status === 'Shipped').length;
  if (shipped === o.lines.reduce((v, l) => v + l.quantity, 0)) return 'Đã giao đủ';
  const delta = (Date.parse(o.expiry) - Date.parse(dayKey(now))) / 86400000;
  return delta < 0 ? 'Hết hạn' : delta <= 3 ? 'Sắp hết hạn' : 'Còn hạn';
}
export function expire(s: DepotSnapshot, now: Date) {
  s.bookings.forEach((b) => {
    if (b.status === 'Reserved' && s.orders.find((o) => o.id === b.orderId)!.expiry < dayKey(now))
      b.status = 'Expired';
  });
}
export const reserved = (s: DepotSnapshot, number: string, now: Date) =>
  s.bookings.find(
    (b) =>
      b.number === number &&
      b.status === 'Reserved' &&
      s.orders.find((o) => o.id === b.orderId)!.expiry >= dayKey(now),
  );
export function lineCounts(s: DepotSnapshot, lineId: string) {
  const b = s.bookings.filter((b) => b.lineId === lineId);
  const held = b.filter((b) => b.status === 'Reserved').length,
    shipped = b.filter((b) => b.status === 'Shipped').length;
  const quantity = s.orders.flatMap((o) => o.lines).find((l) => l.id === lineId)!.quantity;
  return { held, shipped, remaining: quantity - held - shipped, quantity };
}
export function createOrder(s: DepotSnapshot, input: OrderInput, now: Date): Result<string> {
  const fields: Record<string, string> = {};
  const number = input.number.trim().toUpperCase();
  if (!number || s.orders.some((o) => o.number === number))
    fields['number'] = 'Số lệnh bắt buộc và không trùng.';
  if (!/^\d{4}-\d{2}-\d{2}$/.test(input.expiry) || input.expiry < dayKey(now))
    fields['expiry'] = 'Hạn lệnh không được ở quá khứ.';
  if (!s.customers.some((c) => c.id === input.customerId && c.isActive))
    fields['customerId'] = 'Chọn khách hàng đang hoạt động.';
  if (!s.operators.some((o) => o.id === input.operatorId && o.isActive))
    fields['operatorId'] = 'Chọn hãng đang hoạt động.';
  if (
    !input.lines.length ||
    input.lines.some((l) => !Number.isInteger(l.quantity) || l.quantity < 1) ||
    new Set(input.lines.map((l) => l.type + l.size)).size !== input.lines.length
  )
    fields['lines'] = 'Số lượng phải nguyên dương; không trùng loại/cỡ.';
  if (Object.keys(fields).length) return fail('D2', 'Kiểm tra thông tin lệnh giao.', fields);
  const id = uid();
  s.orders.push({ ...input, id, number, lines: input.lines.map((l) => ({ ...l, id: uid() })) });
  return ok(id);
}
export function eligible(
  s: DepotSnapshot,
  orderId: string,
  number: string,
  now: Date,
): Result<string> {
  const o = s.orders.find((o) => o.id === orderId),
    c = s.containers.find((c) => c.number === normalizeNumber(number));
  if (!o || !c || !activeLife(s, c.number)) return fail('D2', 'Container phải đang trong bãi.');
  if (['Hết hạn', 'Đã giao đủ'].includes(orderStatus(s, orderId, now)))
    return fail('D2', 'Lệnh hết hạn hoặc đã giao đủ.');
  if (o.operatorId !== c.operatorId)
    return fail('D2', 'Container và lệnh phải cùng hãng khai thác.');
  if (reserved(s, c.number, now)) return fail('D3', 'Container đã được giữ chỗ.');
  const line = o.lines.find(
    (l) => l.type === c.type && l.size === c.size && lineCounts(s, l.id).remaining > 0,
  );
  return line ? ok(line.id) : fail('D2', 'Không còn số lượng đúng loại/cỡ.');
}
export function assign(
  s: DepotSnapshot,
  orderId: string,
  numbers: string[],
  now: Date,
): Result<void> {
  const draft = structuredClone(s);
  expire(draft, now);
  if (!numbers.length || new Set(numbers.map(normalizeNumber)).size !== numbers.length)
    return fail('D3', 'Chọn container không trùng.');
  for (const raw of numbers) {
    const number = normalizeNumber(raw),
      r = eligible(draft, orderId, number, now);
    if (!r.ok) return r;
    draft.bookings.push({
      id: uid(),
      number,
      orderId,
      lineId: r.value,
      lifecycleId: activeLife(draft, number)!.id,
      status: 'Reserved',
      createdAt: now.toISOString(),
    });
  }
  s.bookings = draft.bookings;
  return ok(undefined);
}
export function release(s: DepotSnapshot, id: string, now: Date): Result<void> {
  const b = s.bookings.find((b) => b.id === id);
  if (!b || b.status !== 'Reserved') return fail('D4', 'Chỉ gỡ container đang giữ chỗ.');
  b.status = 'Cancelled';
  return ok(undefined);
}

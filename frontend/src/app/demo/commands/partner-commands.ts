import { Customer, Operator, DepotSnapshot, Result } from '../../core/models/entities';
import { fail, ok, uid } from '../rules/container-rules';
import { orderStatus } from './order-commands';
export function saveCustomer(
  s: DepotSnapshot,
  input: Omit<Customer, 'id'> & { id?: string },
): Result<void> {
  const existing = s.customers.find((c) => c.id === input.id),
    taxCode = input.taxCode.trim(),
    fields: Record<string, string> = {};
  if (
    !/^\d{10}$/.test(taxCode) ||
    s.customers.some((c) => c.id !== input.id && c.taxCode === taxCode)
  )
    fields['taxCode'] = 'MST gồm 10 chữ số và không trùng.';
  if (
    existing &&
    existing.taxCode !== taxCode &&
    s.orders.some((o) => o.customerId === existing.id)
  )
    fields['taxCode'] = 'Không đổi MST khi khách hàng đã có lệnh giao.';
  if (!input.name.trim()) fields['name'] = 'Tên khách hàng bắt buộc.';
  if (input.email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(input.email))
    fields['email'] = 'Email không hợp lệ.';
  if (Object.keys(fields).length)
    return fail('DEMO_PARTNER', 'Kiểm tra thông tin khách hàng.', fields);
  const value = {
    ...input,
    taxCode,
    name: input.name.trim(),
    id: existing?.id || uid(),
    isActive: existing?.isActive ?? true,
  };
  if (existing) Object.assign(existing, value);
  else s.customers.push(value);
  return ok(undefined);
}
export function saveOperator(
  s: DepotSnapshot,
  input: Omit<Operator, 'id' | 'color'> & { id?: string; color?: string },
): Result<void> {
  const existing = s.operators.find((o) => o.id === input.id),
    code = input.code.trim().toUpperCase(),
    fields: Record<string, string> = {};
  if (!/^[A-Z]{3}$/.test(code) || s.operators.some((o) => o.id !== input.id && o.code === code))
    fields['code'] = 'Mã hãng gồm 3 chữ cái và không trùng.';
  if (
    existing &&
    existing.code !== code &&
    (s.orders.some((o) => o.operatorId === existing.id) ||
      s.containers.some((c) => c.operatorId === existing.id))
  )
    fields['code'] = 'Không đổi mã hãng đã được tham chiếu.';
  if (!input.name.trim()) fields['name'] = 'Tên hãng bắt buộc.';
  if (input.contact && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(input.contact))
    fields['contact'] = 'Email không hợp lệ.';
  if (Object.keys(fields).length)
    return fail('DEMO_PARTNER', 'Kiểm tra thông tin hãng khai thác.', fields);
  const value = {
    ...input,
    code,
    name: input.name.trim(),
    id: existing?.id || uid(),
    isActive: existing?.isActive ?? true,
    color: existing?.color || '#427eae',
  };
  if (existing) Object.assign(existing, value);
  else s.operators.push(value);
  return ok(undefined);
}
export function deactivatePartner(
  s: DepotSnapshot,
  kind: 'customer' | 'operator',
  id: string,
  now: Date,
): Result<void> {
  const partner =
    kind === 'customer'
      ? s.customers.find((c) => c.id === id)
      : s.operators.find((o) => o.id === id);
  if (!partner) return fail('DEMO_PARTNER', 'Không tìm thấy đối tác.');
  if (
    s.orders.some(
      (o) =>
        (kind === 'customer' ? o.customerId : o.operatorId) === id &&
        ['Còn hạn', 'Sắp hết hạn'].includes(orderStatus(s, o.id, now)),
    )
  )
    return fail('DEMO_PARTNER', 'Không thể ngưng hoạt động: đối tác còn lệnh giao hiệu lực.');
  if (
    kind === 'operator' &&
    s.lifecycles.some(
      (l) =>
        !l.outAt &&
        !l.cancelled &&
        s.containers.find((c) => c.number === l.number)?.operatorId === id,
    )
  )
    return fail('DEMO_PARTNER', 'Hãng còn container đang trong bãi.');
  partner.isActive = false;
  return ok(undefined);
}

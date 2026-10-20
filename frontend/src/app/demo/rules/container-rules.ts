import { Container, DepotSnapshot, Result } from '../../core/models/entities';
export const ok = <T>(value: T): Result<T> => ({ ok: true, value });
export const fail = <T = never>(
  code: string,
  message: string,
  fields?: Record<string, string>,
): Result<T> => ({ ok: false, error: { code, message, fields } });
export const uid = () => crypto.randomUUID();
export const normalizeNumber = (v: string) => v.replace(/\s/g, '').toUpperCase();
export const dayKey = (d: Date) => new Date(d.getTime() + 7 * 3600000).toISOString().slice(0, 10);
export function checksum(prefix: string): number {
  const letters = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ';
  const values = [
    10, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 34, 35, 36,
    37, 38,
  ];
  return (
    ([...prefix].reduce(
      (s, c, i) => s + (letters.includes(c) ? values[letters.indexOf(c)] : Number(c)) * 2 ** i,
      0,
    ) %
      11) %
    10
  );
}
export const validContainerNumber = (v: string) =>
  /^[A-Z]{4}\d{7}$/.test(v) && checksum(v.slice(0, 10)) === Number(v[10]);
export function register(s: DepotSnapshot, input: Container, now: Date): Result<Container> {
  const c = {
    ...input,
    number: normalizeNumber(input.number),
    ownerCode: normalizeNumber(input.number).slice(0, 4),
  };
  const fields: Record<string, string> = {};
  if (!validContainerNumber(c.number))
    fields['number'] = 'Số container hoặc số kiểm tra không hợp lệ (C1).';
  if (s.containers.some((x) => x.number === c.number))
    fields['number'] = 'Số container đã tồn tại (C2).';
  if (
    !/^\d{2}[A-Z]\d$/.test(c.iso) ||
    (c.size === 20 ? !c.iso.startsWith('2') : !['4', 'L'].includes(c.iso[0]))
  )
    fields['iso'] = 'Mã ISO không khớp kích thước (C3).';
  if (!(c.tareWeight > 0 && c.maxWeight > c.tareWeight))
    fields['tareWeight'] = 'Trọng lượng vỏ phải dương và nhỏ hơn tối đa (C4).';
  if (
    !/^\d{4}-(0[1-9]|1[0-2])$/.test(c.manufactureMonth) ||
    c.manufactureMonth > dayKey(now).slice(0, 7)
  )
    fields['manufactureMonth'] = 'Tháng sản xuất không hợp lệ hoặc ở tương lai (C4).';
  if (!s.operators.some((x) => x.id === c.operatorId && x.isActive))
    fields['operatorId'] = 'Chọn hãng đang hoạt động.';
  if (Object.keys(fields).length)
    return fail('C1', 'Vui lòng kiểm tra thông tin container.', fields);
  s.containers.push(c);
  return ok(c);
}
export const activeLife = (s: DepotSnapshot, number: string) =>
  s.lifecycles.find((l) => l.number === normalizeNumber(number) && !l.outAt && !l.cancelled);
export const positionText = (p?: import('../../core/models/entities').Position) =>
  !p
    ? 'Ngoài bãi'
    : p.kind === 'virtual'
      ? p.blockId
      : `${p.blockId}-${String(p.bay).padStart(2, '0')}-${p.row}-${p.tier}`;

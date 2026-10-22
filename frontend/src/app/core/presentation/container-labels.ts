import { LookupsDto } from '../api/lookups.dto';
export function enumLabel(
  dictionary: Record<string, string> | undefined,
  code: string | number,
): string {
  return dictionary?.[String(code)] ?? String(code);
}
export function containerTypeLabel(lookups: LookupsDto | null, code: string): string {
  return enumLabel(lookups?.containerTypes, code);
}
export const containerStatusLabels: Record<string, string> = {
  InYard: 'Trong bãi',
  OutOfYard: 'Đã xuất',
  NeverIn: 'Chưa nhập bãi',
  Reserved: 'Đã giữ chỗ',
};
export const orderStatusLabels: Record<string, string> = {
  Active: 'Còn hiệu lực',
  ExpiringSoon: 'Sắp hết hạn',
  Expired: 'Hết hạn',
  FullyDelivered: 'Đã giao đủ',
};

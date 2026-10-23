import { CustomerDto, LineOperatorDto } from '../../core/api/partners.dto';
export type PartnerKind = 'customers' | 'line-operators';
export interface PartnerView {
  id: string;
  code: string;
  name: string;
  isActive: boolean;
  orderCount: number;
  activeOrderCount: number;
  containersInYard?: number;
}
export function mapPartner(row: CustomerDto | LineOperatorDto): PartnerView {
  return { ...row, code: 'taxCode' in row ? row.taxCode : row.code };
}
export function partnerColor(code: string): string {
  let hash = 0;
  for (const letter of code) hash = (hash * 31 + letter.charCodeAt(0)) >>> 0;
  return 'hsl(' + (hash % 360) + ' 55% 42%)';
}
export function partnerPayload(kind: PartnerKind, value: Record<string, unknown>) {
  return kind === 'customers'
    ? {
        name: String(value['name'] ?? '').trim(),
        taxCode: String(value['taxCode'] ?? value['code'] ?? '').trim(),
      }
    : {
        name: String(value['name'] ?? '').trim(),
        code: String(value['code'] ?? '')
          .trim()
          .toUpperCase(),
      };
}

import { ApiParams } from '../../core/api/api.types';
import { ContainerDto, RegisterContainerRequest } from '../../core/api/containers.dto';
import { normalizeContainerNumber } from '../../core/presentation/container-number';
export const containerTypes = [
  'Dry',
  'Reefer',
  'OpenTop',
  'FlatRack',
  'Bunker',
  'Ventilated',
  'Specialized',
] as const;
export const containerConditions = ['Normal', 'Damaged', 'Dented', 'Bent', 'Warped'] as const;
export function mapContainer(dto: ContainerDto) {
  const slot = dto.currentSlot;
  return {
    ...dto,
    number: dto.containerNumber,
    size: dto.size === 'Ft40' ? (40 as const) : (20 as const),
    location: slot
      ? {
          ...slot,
          kind:
            slot.bay === 0 && slot.row === 0 && slot.tier === 0
              ? ('virtual' as const)
              : ('physical' as const),
        }
      : null,
  };
}
export type ContainerView = ReturnType<typeof mapContainer>;
export function normalizeContainerQuery(
  raw: Record<string, unknown>,
): ApiParams & { pageIndex: number; pageSize: number } {
  const page = Number(raw['pageIndex'] ?? raw['page'] ?? 1);
  const member = (key: string, values: readonly string[]) =>
    values.includes(String(raw[key])) ? String(raw[key]) : undefined;
  return {
    search:
      String(raw['search'] ?? '')
        .trim()
        .slice(0, 100) || undefined,
    pageIndex: Number.isSafeInteger(page) && page > 0 && page <= 1_000_000 ? page : 1,
    pageSize: 8,
    type: member('type', containerTypes),
    condition: member('condition', containerConditions),
    booking: member('booking', ['assigned', 'unassigned']),
    displayStatus: member('displayStatus', ['InYard', 'Out', 'NeverIn', 'Reserved']),
    size: member('size', ['Ft20', 'Ft40']),
    ownerCode: String(raw['ownerCode'] ?? '').trim() || undefined,
    lineOperator: String(raw['lineOperator'] ?? '').trim() || undefined,
    sort: member('sort', ['number', 'size']) ?? 'number',
    descending: raw['descending'] === true || raw['descending'] === 'true',
  };
}
export function registrationRequest(value: Record<string, unknown>): RegisterContainerRequest {
  const month = String(value['manufactureMonth'] ?? '');
  return {
    containerNumber: normalizeContainerNumber(String(value['number'] ?? '')),
    isoCode: String(value['isoCode'] ?? '')
      .trim()
      .toUpperCase(),
    size: Number(value['size']) === 40 ? 'Ft40' : 'Ft20',
    type: String(value['type']) as RegisterContainerRequest['type'],
    tareWeight: Number(value['tareWeight']),
    maxGrossWeight: Number(value['maxGrossWeight']),
    grade: String(value['grade']),
    condition: String(value['condition']) as RegisterContainerRequest['condition'],
    dateOfManufacture: /^\d{4}-(0[1-9]|1[0-2])$/.test(month) ? month + '-01T00:00:00.000Z' : null,
  };
}

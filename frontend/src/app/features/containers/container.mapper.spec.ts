import { describe, expect, it } from 'vitest';
import { mapContainer, normalizeContainerQuery, registrationRequest } from './container.mapper';
import { ContainerDto } from '../../core/api/containers.dto';
import { containerStatusLabels } from '../../core/presentation/container-labels';
const dto = {
  id: '1',
  containerNumber: 'MSCU9000000',
  size: 'Ft20',
  type: 'Dry',
  isoCode: '22G1',
  classification: 'A',
  condition: 'Normal',
  ownerCode: 'MSC',
  tareWeight: 2000,
  maxGrossWeight: 30000,
  dateOfManufacture: null,
  displayStatus: 'InYard',
  currentBooking: null,
  isInYard: true,
  currentSlot: { blockCode: 'VB01', bay: 0, row: 0, tier: 0 },
} as ContainerDto;
describe('container wire mapping', () => {
  it('uses the departed-container status from the API for filtering and labels', () => {
    expect(normalizeContainerQuery({ displayStatus: 'OutOfYard' })['displayStatus']).toBe(
      'OutOfYard',
    );
    expect(normalizeContainerQuery({ displayStatus: 'Out' })['displayStatus']).toBeUndefined();
    expect(containerStatusLabels['OutOfYard']).toBe('Đã xuất');
  });
  it('keeps enum codes and distinguishes virtual location from absent location', () => {
    expect(mapContainer(dto).size).toBe(20);
    expect(mapContainer(dto).location?.kind).toBe('virtual');
    expect(mapContainer({ ...dto, currentSlot: null }).location).toBeNull();
    expect(mapContainer({ ...dto, size: 'Ft40' }).size).toBe(40);
  });
  it('invalidUrlQueryFallsBackSafely', () => {
    for (const pageIndex of [NaN, -1, Infinity, 'garbage']) {
      expect(
        normalizeContainerQuery({ pageIndex, type: 'evil', sort: 'evil', condition: 'evil' })
          .pageIndex,
      ).toBe(1);
      expect(normalizeContainerQuery({ pageIndex, type: 'evil' })['type']).toBeUndefined();
    }
  });
  it('sends numeric weights and explicit enum codes', () => {
    const request = registrationRequest({
      number: ' mscu 9000000 ',
      isoCode: '22G1',
      size: '20',
      type: 'Dry',
      grade: 'A',
      condition: 'Normal',
      tareWeight: '2200',
      maxGrossWeight: '30480',
      manufactureMonth: '2021-06',
    });
    expect(request).toMatchObject({
      containerNumber: 'MSCU9000000',
      size: 'Ft20',
      type: 'Dry',
      tareWeight: 2200,
      maxGrossWeight: 30480,
      grade: 'A',
      condition: 'Normal',
      dateOfManufacture: '2021-06-01T00:00:00.000Z',
    });
  });
});

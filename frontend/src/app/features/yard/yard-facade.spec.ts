import { TestBed } from '@angular/core/testing';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { errorInterceptor } from '../../core/api/error.interceptor';
import { YardFacade } from './yard-facade';
import { mapYardContainers, physicalCapacity } from './yard.mapper';
import { YardLayoutDto } from '../../core/api/yard.dto';
const layout = {
  blockId: 'block',
  code: 'A',
  name: 'A',
  isVirtual: false,
  maxBays: 5,
  oddBays: 3,
  maxRows: 1,
  maxTiers: 2,
  slots: [
    { id: 's1', bay: 1, row: 1, tier: 1, containerId: 'c1', isOccupied: true },
    { id: 's2', bay: 3, row: 1, tier: 1, containerId: 'c1', isOccupied: true },
  ],
  containers: [{ id: 'c1', number: 'MSCU1234567', size: 'Ft40' }],
} as YardLayoutDto;
describe('yard API contracts', () => {
  let http: HttpTestingController;
  let facade: YardFacade;
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
        YardFacade,
      ],
    });
    http = TestBed.inject(HttpTestingController);
    facade = TestBed.inject(YardFacade);
  });
  afterEach(() => {
    http.verify();
    TestBed.resetTestingModule();
  });
  it('fortyFootFootprintAndVirtualPositionMatchLayout', () => {
    const rows = mapYardContainers(layout);
    expect(rows).toHaveLength(1);
    expect(rows[0].footprint).toHaveLength(2);
    expect(rows[0].bay).toBe(2);
    expect(physicalCapacity({ ...layout, isVirtual: true })).toBe(0);
  });
  it('lateTierResponseDoesNotReplaceSelectedTier', () => {
    facade.select('block', 1);
    const old = http.expectOne('/api/yard/blocks/block/layout?tier=1');
    facade.select('block', 2);
    expect(old.cancelled).toBe(true);
    http.expectOne('/api/yard/blocks/block/layout?tier=2').flush({ ...layout, slots: [] });
    expect(facade.tier()).toBe(2);
    expect(facade.layout()?.slots).toHaveLength(0);
  });
  it('occupiedTarget409ReloadsLayout', () => {
    facade.select('block', 1);
    http.expectOne('/api/yard/blocks/block/layout?tier=1').flush(layout);
    facade.selectedContainer.set('c1');
    facade
      .relocate({
        blockId: 'block',
        sourceBay: 2,
        sourceRow: 1,
        sourceTier: 1,
        targetBay: 4,
        targetRow: 1,
        targetTier: 1,
      })
      .subscribe({ error: () => {} });
    http
      .expectOne('/api/yard/relocations')
      .flush(
        { code: 'Conflict.ConcurrentUpdate', detail: 'Occupied' },
        { status: 409, statusText: 'Conflict' },
      );
    http.expectOne('/api/yard/blocks/block/layout?tier=1').flush(layout);
    http.expectOne('/api/yard/blocks').flush([]);
    expect(facade.selectedContainer()).toBe('c1');
    expect(facade.error()?.status).toBe(409);
  });
  it.each(['expand', 'shrink'] as const)(
    '%s uses a dedicated endpoint and numeric dimensions',
    (mode) => {
      facade.select('block', 1);
      http.expectOne('/api/yard/blocks/block/layout?tier=1').flush(layout);
      facade.resize(mode, { newBays: 7, newRows: 2, newTiers: 3 }).subscribe();
      const request = http.expectOne('/api/yard/blocks/block/' + mode);
      expect(request.request.body).toEqual({ newBays: 7, newRows: 2, newTiers: 3 });
      request.flush(null);
      http.expectOne('/api/yard/blocks/block/layout?tier=1').flush(layout);
      http.expectOne('/api/yard/blocks').flush([]);
      expect(facade.pending()).toBe(false);
    },
  );
  it('placeSendsNumericCoordinates', () => {
    facade.select('block', 1);
    http.expectOne('/api/yard/blocks/block/layout?tier=1').flush(layout);
    facade.place({ blockId: 'block', containerId: 'c2', bay: 5, row: 3, tier: 1 }).subscribe();
    const request = http.expectOne('/api/yard/placements');
    expect(request.request.body.row).toBe(3);
    request.flush(null);
    http.expectOne('/api/yard/blocks/block/layout?tier=1').flush(layout);
    http.expectOne('/api/yard/blocks').flush([]);
  });
});

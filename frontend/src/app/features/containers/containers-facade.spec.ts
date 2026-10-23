import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { ContainersFacade } from './containers-facade';
import { ContainersApi } from '../../core/api/containers-api';
describe('server container queries', () => {
  let http: HttpTestingController;
  let facade: ContainersFacade;
  beforeEach(() => {
    vi.useFakeTimers();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), ContainersApi, ContainersFacade],
    });
    http = TestBed.inject(HttpTestingController);
    facade = TestBed.inject(ContainersFacade);
  });
  afterEach(() => {
    http.verify();
    TestBed.resetTestingModule();
    vi.useRealTimers();
  });
  it('lateSearchResponseCannotReplaceNewQuery', () => {
    facade.setQuery({ search: 'A' });
    vi.advanceTimersByTime(299);
    http.expectNone((request) => request.url === '/api/containers');
    vi.advanceTimersByTime(1);
    const old = http.expectOne(
      (request) => request.url === '/api/containers' && request.params.get('search') === 'A',
    );
    const oldSummary = http.expectOne('/api/containers/summary?search=A');
    facade.setQuery({ search: 'B' });
    expect(old.cancelled).toBe(true);
    expect(oldSummary.cancelled).toBe(true);
    expect(facade.query()['search']).toBe('B');
    vi.advanceTimersByTime(300);
    expect(old.cancelled).toBe(true);
    expect(oldSummary.cancelled).toBe(true);
    http
      .expectOne(
        (request) => request.url === '/api/containers' && request.params.get('search') === 'B',
      )
      .flush({ items: [], totalCount: 0, totalPages: 0 });
    http
      .expectOne('/api/containers/summary?search=B')
      .flush({ total: 0, byType: {}, bySize: {}, byCondition: {} });
    expect(facade.totalCount()).toBe(0);
    expect(facade.loading()).toBe(false);
  });
  it('pageBeyondLastMovesToLastPageOnce', () => {
    facade.setQuery({ pageIndex: 999 });
    vi.advanceTimersByTime(300);
    http
      .expectOne(
        (request) => request.url === '/api/containers' && request.params.get('pageIndex') === '999',
      )
      .flush({ items: [], totalCount: 17, totalPages: 3 });
    http
      .expectOne('/api/containers/summary')
      .flush({ total: 17, byType: { Dry: 17 }, bySize: {}, byCondition: {} });
    http
      .expectOne(
        (request) => request.url === '/api/containers' && request.params.get('pageIndex') === '3',
      )
      .flush({ items: [], totalCount: 17, totalPages: 3 });
    expect(facade.query().pageIndex).toBe(3);
    expect(facade.totalCount()).toBe(17);
    vi.advanceTimersByTime(1000);
    http.expectNone((request) => request.url === '/api/containers');
  });
  it('failedRegistrationDoesNotIncreaseTheServerCount', () => {
    facade.totalCount.set(7);
    facade
      .register({
        containerNumber: 'invalid',
        isoCode: '22G1',
        size: 'Ft20',
        type: 'Dry',
        tareWeight: 2000,
        maxGrossWeight: 30000,
        grade: 'A',
        condition: 'Normal',
        dateOfManufacture: null,
      })
      .subscribe({ error: () => {} });
    expect(facade.pending()).toBe(true);
    http.expectOne('/api/containers').flush({}, { status: 400, statusText: 'Invalid' });
    expect(facade.pending()).toBe(false);
    expect(facade.totalCount()).toBe(7);
  });
});

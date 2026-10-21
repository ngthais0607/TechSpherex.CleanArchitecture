import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { AuthService } from '../auth/auth-service';
import { QueryInvalidation } from './query-invalidation';
import { ReferenceData } from './reference-data';

describe('reference cache', () => {
  const user = signal({ userId: '1', tenantId: 'A' });
  let http: HttpTestingController;
  beforeEach(() => {
    user.set({ userId: '1', tenantId: 'A' });
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting(), { provide: AuthService, useValue: { user } }] });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => { http.verify(); vi.restoreAllMocks(); TestBed.resetTestingModule(); });
  it('cacheExpiresAndClearsOnIdentityChange', () => {
    let now = 1000;
    vi.spyOn(Date, 'now').mockImplementation(() => now);
    const cache = TestBed.inject(ReferenceData);
    TestBed.tick();
    let count = 0;
    cache.lookups().subscribe(() => count++);
    cache.lookups().subscribe(() => count++);
    http.expectOne('/api/lookups').flush({ containerTypes: { Dry: 'Khô' } });
    expect(count).toBe(2);
    cache.lookups().subscribe(() => count++);
    http.expectNone('/api/lookups');
    now += 3_600_001;
    cache.lookups().subscribe();
    http.expectOne('/api/lookups').flush({});
    TestBed.inject(QueryInvalidation).invalidate(['lookups']);
    cache.lookups().subscribe();
    const old = http.expectOne('/api/lookups');
    user.set({ userId: '2', tenantId: 'B' });
    TestBed.tick();
    expect(old.cancelled).toBe(true);
    let fresh = false;
    cache.lookups().subscribe(() => fresh = true);
    http.expectOne('/api/lookups').flush({ containerTypes: { Dry: 'Mới' } });
    expect(fresh).toBe(true);
  });
});

import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { AuthService } from '../../core/auth/auth-service';
import { errorInterceptor } from '../../core/api/error.interceptor';
import { PartnersFacade } from './partners-facade';
describe('partner contracts', () => {
  const user = signal({ roles: ['Admin'] });
  let http: HttpTestingController;
  let facade: PartnersFacade;
  beforeEach(() => {
    user.set({ roles: ['Admin'] });
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
        PartnersFacade,
        { provide: AuthService, useValue: { user } },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    facade = TestBed.inject(PartnersFacade);
  });
  afterEach(() => {
    http.verify();
    TestBed.resetTestingModule();
  });
  it('unsupportedContactFieldsAreNeverSubmitted', () => {
    facade
      .save('customers', null, {
        name: 'New',
        taxCode: '0102030405',
        email: 'secret',
        phone: '1',
        address: 'x',
        contact: 'x',
        color: 'red',
      })
      .subscribe();
    const request = http.expectOne('/api/customers');
    expect(request.request.body).toEqual({ name: 'New', taxCode: '0102030405' });
    request.flush({ id: 'id' });
  });
  it('readRoleCannotMutate', () => {
    user.set({ roles: ['OrderClerk'] });
    let status = 0;
    facade
      .save('customers', null, { name: 'New', taxCode: '0102030405' })
      .subscribe({ error: (error) => (status = error.status) });
    http.expectNone('/api/customers');
    expect(status).toBe(403);
    expect(facade.canManage()).toBe(false);
  });
  it('deactivation409PreservesRow', () => {
    facade.rows.set([
      {
        id: 'id',
        code: '0102030405',
        name: 'Customer',
        isActive: true,
        orderCount: 1,
        activeOrderCount: 1,
      },
    ]);
    facade.setActive('customers', 'id', false).subscribe({ error: () => {} });
    http
      .expectOne('/api/customers/id/deactivate')
      .flush(
        { code: 'Customer.HasActiveOrders', detail: 'Active orders' },
        { status: 409, statusText: 'Conflict' },
      );
    expect(facade.rows()[0].isActive).toBe(true);
    expect(facade.error()?.code).toBe('Customer.HasActiveOrders');
  });
});

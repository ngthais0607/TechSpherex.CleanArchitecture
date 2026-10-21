import { TestBed } from '@angular/core/testing';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { OrdersFacade } from './orders-facade';
import { errorInterceptor } from '../../core/api/error.interceptor';
const order = { id: 'order-id', orderNumber: 'DO001', lineOperator: 'MSC', customerTaxCode: '0101234567', customerName: 'Customer',
  expiryDate: '2099-01-01T00:00:00Z', outboundVessel: 'Vessel', isCompleted: false, status: 'Active',
  items: [{id:'item-id',size:'Ft20',type:'Dry',requiredQuantity:3,deliveredQuantity:0,remainingQuantity:3}] };
const bookings = { bookings: { items: [], totalCount: 0, totalPages: 0 }, itemCounts: [{itemId:'item-id',reservedQuantity:0,shippedQuantity:0,availableQuantity:3}] };
describe('order commands', () => {
  let http: HttpTestingController;
  let facade: OrdersFacade;
  function flushDetail() {
    http.expectOne('/api/delivery-orders/order-id').flush(order);
    http.expectOne(request => request.url === '/api/delivery-orders/DO001/bookings').flush(bookings);
  }
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(withInterceptors([errorInterceptor])), provideHttpClientTesting(), OrdersFacade] });
    http = TestBed.inject(HttpTestingController); facade = TestBed.inject(OrdersFacade);
    facade.select('order-id'); flushDetail();
  });
  afterEach(() => { http.verify(); TestBed.resetTestingModule(); });
  it('partialAssignShowsAssignedAndRejectedNumbers', () => {
    facade.selectedNumbers.set(['A', 'B', 'C']);
    facade.assign(['A', 'B', 'C']).subscribe();
    const request = http.expectOne('/api/delivery-orders/DO001/assign-containers');
    expect(request.request.body).toEqual({ containerNumbers: ['A', 'B', 'C'] });
    request.flush({ orderId:'order-id',requestedCount:3,assignedCount:2,bookingIds:['b1','b2'],failedContainerNumbers:['C'] });
    expect(facade.message()).toContain('2/3');
    expect(facade.message()).toContain('C');
    expect(facade.selectedNumbers()).toEqual(['C']);
    flushDetail();
    http.expectNone(request => request.url.includes('/yard'));
  });
  it.each([409,422])('conflictKeepsFormAndRefreshesOrder (%s)', status => {
    facade.selectedNumbers.set(['C']);
    facade.assign(['C']).subscribe({ error: () => {} });
    http.expectOne('/api/delivery-orders/DO001/assign-containers').flush({code:'Booking.QuotaExceeded',detail:'Quota'}, {status,statusText:'Conflict'});
    expect(facade.selectedNumbers()).toEqual(['C']);
    expect(facade.detail()?.items[0].remainingQuantity).toBe(3);
    flushDetail();
    expect(facade.pending()).toBe(false);
    http.expectNone(request => request.url.includes('/yard'));
  });
  it('releaseUsesTheRealBookingId', () => {
    facade.release('booking-guid').subscribe();
    const request = http.expectOne('/api/delivery-orders/bookings/booking-guid');
    expect(request.request.method).toBe('DELETE');request.flush(null);
    flushDetail();
  });
});

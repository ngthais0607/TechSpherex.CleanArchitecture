import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs';
import { API_BASE_URL } from './api-config';
import { ApiPage, ApiParams, queryParams } from './api.types';
import {
  AssignContainersResult,
  CreateOrderRequest,
  DeliveryOrderDto,
  OrderBookingsDto,
} from './orders.dto';
import { QueryInvalidation } from './query-invalidation';
@Injectable({ providedIn: 'root' })
export class OrdersApi {
  private readonly http = inject(HttpClient);
  private readonly invalidation = inject(QueryInvalidation);
  list(query: ApiParams) {
    return this.http.get<ApiPage<DeliveryOrderDto>>(API_BASE_URL + '/delivery-orders', {
      params: queryParams(query),
    });
  }
  byId(id: string) {
    return this.http.get<DeliveryOrderDto>(
      `${API_BASE_URL}/delivery-orders/${encodeURIComponent(id)}`,
    );
  }
  byNumber(number: string) {
    return this.http.get<DeliveryOrderDto>(
      `${API_BASE_URL}/delivery-orders/by-number/${encodeURIComponent(number)}`,
    );
  }
  create(input: CreateOrderRequest) {
    return this.http
      .post<{ id: string }>(API_BASE_URL + '/delivery-orders', input)
      .pipe(this.changed());
  }
  bookings(number: string, pageIndex = 1) {
    return this.http.get<OrderBookingsDto>(
      `${API_BASE_URL}/delivery-orders/${encodeURIComponent(number)}/bookings`,
      { params: { pageIndex, pageSize: 8 } },
    );
  }
  assign(number: string, containerNumbers: string[]) {
    return this.http
      .post<AssignContainersResult>(
        `${API_BASE_URL}/delivery-orders/${encodeURIComponent(number)}/assign-containers`,
        { containerNumbers },
      )
      .pipe(this.changed());
  }
  release(bookingId: string) {
    return this.http
      .delete<void>(`${API_BASE_URL}/delivery-orders/bookings/${encodeURIComponent(bookingId)}`)
      .pipe(this.changed());
  }
  private changed<T>() {
    return tap<T>(() => this.invalidation.invalidate(['orders', 'containers', 'reports']));
  }
}

import { Component, computed, inject, input, output, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DemoRepository } from '../../demo/demo-repository';
import { Modal, Badge } from '../../shared/ui';
import { assign, eligible } from '../../demo/commands/order-commands';
@Component({
  selector: 'app-container-detail',
  imports: [Modal, Badge, DatePipe, RouterLink, FormsModule],
  template: `@if (container(); as c) {
    <app-modal [title]="c.number" [width]="960" (closed)="closed.emit()"
      ><div class="tabs">
        <button [class.active]="tab() === 'info'" (click)="tab.set('info')">Thông tin</button
        ><button [class.active]="tab() === 'orders'" (click)="tab.set('orders')">Lệnh giao</button>
      </div>
      @if (tab() === 'info') {
        <div class="detail-grid">
          @for (item of details(); track item.label) {
            <div class="detail-item">
              <span>{{ item.label }}</span
              ><strong>{{ item.value }}</strong>
            </div>
          }
        </div>
      } @else {
        <div class="panel">
          <div class="panel-header">
            <h3>Lệnh giao hiện tại</h3>
            <app-badge [value]="store.status(c.number)" />
          </div>
          <div class="panel-body">
            @if (current(); as o) {
              <h3 class="mono">{{ o.number }}</h3>
              <p>
                {{ store.customer(o.customerId) }}<br />Hạn lệnh:
                {{ o.expiry | date: 'dd/MM/yyyy' }} · {{ o.voyage }}
              </p>
              <a class="button" routerLink="/delivery-orders" [queryParams]="{ order: o.id }"
                >Xem lệnh</a
              >
            } @else {
              <p class="muted">Container chưa được đặt chỗ.</p>
              <div class="toolbar-group">
                <select aria-label="Chọn lệnh giao để gán" [(ngModel)]="orderId">
                  <option value="">Chọn lệnh giao phù hợp...</option>
                  @for (o of eligibleOrders(); track o.id) {
                    <option [value]="o.id">
                      {{ o.number }} · {{ store.customer(o.customerId) }}
                    </option>
                  }</select
                ><button class="button primary" [disabled]="!orderId" (click)="assignOrder()">
                  Gán vào lệnh giao
                </button>
              </div>
              @if (!eligibleOrders().length) {
                <small class="muted"
                  >Không có lệnh còn hạn, cùng hãng và đúng loại/cỡ còn chỗ.</small
                >
              }
            }
            @if (error()) {
              <p class="field-error" role="alert">{{ error() }}</p>
            }
          </div>
        </div>
        <p class="muted section-gap">
          Đã qua {{ customerCount() }} khách hàng · Đã xuất {{ shippedCount() }} lần
        </p>
        <div class="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Lệnh giao</th>
                <th>Khách hàng</th>
                <th>Ngày giữ chỗ</th>
                <th>Trạng thái</th>
              </tr>
            </thead>
            <tbody>
              @for (b of bookings(); track b.id) {
                <tr>
                  <td>
                    <a routerLink="/delivery-orders" [queryParams]="{ order: b.orderId }">{{
                      order(b.orderId)?.number
                    }}</a>
                  </td>
                  <td>{{ store.customer(order(b.orderId)?.customerId || '') }}</td>
                  <td class="nowrap">{{ b.createdAt | date: 'dd/MM/yyyy' }}</td>
                  <td><app-badge [value]="bookingLabel(b.status)" /></td>
                </tr>
              } @empty {
                <tr>
                  <td colspan="4" class="empty">Chưa có lịch sử lệnh giao.</td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </app-modal>
  }`,
})
export class ContainerDetail {
  number = input.required<string>();
  closed = output();
  store = inject(DemoRepository);
  tab = signal('info');
  orderId = '';
  error = signal('');
  container = computed(() => this.store.state().containers.find((c) => c.number === this.number()));
  order = (id: string) => this.store.state().orders.find((o) => o.id === id);
  current = computed(() => {
    const b = this.store.booking(this.number());
    return b ? this.order(b.orderId) : null;
  });
  bookings = computed(() =>
    this.store
      .state()
      .bookings.filter((b) => b.number === this.number())
      .sort((a, b) => b.createdAt.localeCompare(a.createdAt)),
  );
  shippedCount = computed(() => this.bookings().filter((b) => b.status === 'Shipped').length);
  customerCount = computed(
    () => new Set(this.bookings().map((b) => this.order(b.orderId)?.customerId)).size,
  );
  eligibleOrders = computed(() =>
    this.store
      .state()
      .orders.filter((o) => eligible(this.store.state(), o.id, this.number(), this.store.now()).ok),
  );
  details = computed(() => {
    const c = this.container();
    return c
      ? [
          { label: 'Loại container', value: c.type },
          { label: 'Mã ISO', value: c.iso },
          { label: 'Kích thước', value: c.size + 'ft' },
          { label: 'Hãng khai thác', value: this.store.operator(c.operatorId) },
          { label: 'Chủ sở hữu', value: c.ownerCode },
          { label: 'Phân loại', value: c.grade },
          { label: 'Tình trạng', value: c.condition },
          { label: 'Trạng thái', value: this.store.status(c.number) },
          { label: 'Trọng lượng tối đa', value: c.maxWeight.toLocaleString('vi-VN') + ' kg' },
          { label: 'Trọng lượng vỏ', value: c.tareWeight.toLocaleString('vi-VN') + ' kg' },
          { label: 'Tháng sản xuất', value: c.manufactureMonth.split('-').reverse().join('/') },
          { label: 'Vị trí hiện tại', value: this.store.location(c.number) },
        ]
      : [];
  });
  bookingLabel(s: string) {
    return (
      (
        {
          Reserved: 'Đang giữ chỗ',
          Shipped: 'Đã xuất',
          Cancelled: 'Đã hủy',
          Expired: 'Hết hạn',
        } as Record<string, string>
      )[s] || s
    );
  }
  assignOrder() {
    const r = this.store.run(
      (s) => assign(s, this.orderId, [this.number()], this.store.now()),
      'Đã gán container vào lệnh giao.',
    );
    if (!r.ok) this.error.set(r.error.message);
    else this.orderId = '';
  }
}

import { Component, effect, inject, input, output, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Modal, Badge, Pagination } from '../../shared/ui';
import { ContainersFacade } from './containers-facade';
import { ContainersApi } from '../../core/api/containers-api';
import { ReportsApi } from '../../core/api/reports-api';
import { ContainerOrdersDto } from '../../core/api/containers.dto';
import { ContainerHistoryEventDto } from '../../core/api/reports.dto';
import { ApiPage } from '../../core/api/api.types';
import { ApiError } from '../../core/api/api-error';
import { containerStatusLabels } from '../../core/presentation/container-labels';
import { AuthService } from '../../core/auth/auth-service';
import { can } from '../../core/auth/permissions';
import { catchError, EMPTY, finalize } from 'rxjs';
@Component({
  selector: 'app-container-detail',
  imports: [Modal, Badge, DatePipe, RouterLink, Pagination],
  template: `<app-modal [title]="number()" [width]="960" (closed)="closed.emit()">
    <div class="tabs">
      <button [class.active]="tab() === 'info'" (click)="tab.set('info')">Thông tin</button>
      <button [class.active]="tab() === 'orders'" (click)="tab.set('orders')">Lệnh giao</button>
      <button [class.active]="tab() === 'history'" (click)="tab.set('history')">
        Lịch sử di chuyển
      </button>
    </div>
    @if (tab() === 'info') {
      @if (facade.detailLoading()) {
        <p role="status">Đang tải chi tiết…</p>
      }
      @if (facade.detailError(); as error) {
        <p class="banner error" role="alert">{{ error.detail }}</p>
      }
      @if (facade.detail(); as c) {
        <div class="detail-grid">
          <div class="detail-item">
            <span>Số container</span><strong>{{ c.number }}</strong>
          </div>
          <div class="detail-item">
            <span>Loại · ISO</span><strong>{{ c.type }} · {{ c.isoCode }}</strong>
          </div>
          <div class="detail-item">
            <span>Kích thước</span><strong>{{ c.size }}ft</strong>
          </div>
          <div class="detail-item">
            <span>Owner</span><strong>{{ c.ownerCode }}</strong>
          </div>
          <div class="detail-item">
            <span>Trạng thái</span
            ><app-badge [value]="statusLabels[c.displayStatus] || c.displayStatus" />
          </div>
          <div class="detail-item">
            <span>Vị trí</span
            ><strong>
              @if (c.location; as loc) {
                {{ loc.blockCode }}
                @if (loc.kind === 'physical') {
                  {{ loc.bay }}-{{ loc.row }}-{{ loc.tier }}
                } @else {
                  (ảo)
                }
              } @else {
                —
              }
            </strong>
          </div>
          <div class="detail-item">
            <span>Trọng lượng vỏ / tối đa</span
            ><strong>{{ c.tareWeight }} / {{ c.maxGrossWeight }} kg</strong>
          </div>
          <div class="detail-item">
            <span>Ngày sản xuất</span
            ><strong>{{ (c.dateOfManufacture | date: 'MM/yyyy') || '—' }}</strong>
          </div>
        </div>
      }
    } @else {
      @if (loading()) {
        <p role="status">Đang tải…</p>
      }
      @if (error(); as error) {
        <p class="banner error" role="alert">{{ error.detail }}</p>
      }
      @if (tab() === 'orders') {
        @if (orders(); as data) {
          <div class="panel-body">
            @if (data.currentOrder; as order) {
              <h3 class="mono">{{ order.orderNumber }}</h3>
              <p>{{ order.customerName }} · Hạn {{ order.expiryDate | date: 'dd/MM/yyyy' }}</p>
              @if (canReadOrders()) {
                <a
                  class="button"
                  routerLink="/delivery-orders"
                  [queryParams]="{ order: order.orderId }"
                  >Xem lệnh</a
                >
              }
            } @else {
              <p>Container chưa được giữ chỗ.</p>
              @if (canManageOrders()) {
                <a
                  class="button"
                  routerLink="/delivery-orders"
                  [queryParams]="{ container: number() }"
                  >Chọn lệnh giao</a
                >
              }
            }
            <p>
              Đã qua {{ data.summary.customerCount }} khách hàng · Đã xuất
              {{ data.summary.shipmentCount }} lần
            </p>
          </div>
          <div class="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Lệnh giao</th>
                  <th>Khách hàng</th>
                  <th>Ngày xuất</th>
                  <th>Trạng thái</th>
                </tr>
              </thead>
              <tbody>
                @for (row of data.history; track $index) {
                  <tr>
                    <td>{{ row.orderNumber }}</td>
                    <td>{{ row.customerName }}</td>
                    <td>{{ (row.shippedDate | date: 'dd/MM/yyyy HH:mm') || '—' }}</td>
                    <td>{{ row.status }}</td>
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
      } @else {
        <div class="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Thời điểm</th>
                <th>Sự kiện</th>
                <th>Từ</th>
                <th>Đến</th>
                <th>Phương tiện</th>
              </tr>
            </thead>
            <tbody>
              @for (row of history()?.items || []; track row.eventId) {
                <tr>
                  <td>{{ row.occurredAtUtc | date: 'dd/MM/yyyy HH:mm' }}</td>
                  <td>{{ row.eventType }}</td>
                  <td>{{ row.fromPosition?.blockCode || '—' }}</td>
                  <td>{{ row.toPosition?.blockCode || '—' }}</td>
                  <td>{{ row.vehicleNumber || '—' }}</td>
                </tr>
              } @empty {
                @if (!loading()) {
                  <tr>
                    <td colspan="5" class="empty">Chưa có lịch sử di chuyển.</td>
                  </tr>
                }
              }
            </tbody>
          </table>
        </div>
        <app-pagination
          [total]="history()?.totalCount || 0"
          [page]="historyPage()"
          (changed)="historyPage.set($event)"
        />
      }
    }
  </app-modal>`,
})
export class ContainerDetail {
  readonly number = input.required<string>();
  readonly closed = output<void>();
  readonly facade = inject(ContainersFacade);
  private readonly api = inject(ContainersApi);
  private readonly reports = inject(ReportsApi);
  private readonly auth = inject(AuthService);
  readonly tab = signal<'info' | 'orders' | 'history'>('info');
  readonly orders = signal<ContainerOrdersDto | null>(null);
  readonly history = signal<ApiPage<ContainerHistoryEventDto> | null>(null);
  readonly historyPage = signal(1);
  readonly loading = signal(false);
  readonly error = signal<ApiError | null>(null);
  readonly statusLabels = containerStatusLabels;
  canReadOrders(): boolean {
    return can('readOrders', this.auth.user()?.roles ?? []);
  }
  canManageOrders(): boolean {
    return can('manageOrders', this.auth.user()?.roles ?? []);
  }
  constructor() {
    effect((onCleanup) => {
      const number = this.number(),
        tab = this.tab(),
        page = this.historyPage();
      this.orders.set(null);
      this.history.set(null);
      this.error.set(null);
      this.loading.set(tab !== 'info');
      if (tab === 'info') return;
      const request =
        tab === 'orders'
          ? this.api.orders(number).subscribe({
              next: (value) => {
                this.orders.set(value);
                this.loading.set(false);
              },
              error: (error) => {
                this.error.set(error);
                this.loading.set(false);
              },
            })
          : this.reports.history(number, page).subscribe({
              next: (value) => {
                this.history.set(value);
                this.loading.set(false);
              },
              error: (error) => {
                this.error.set(error);
                this.loading.set(false);
              },
            });
      onCleanup(() => request.unsubscribe());
    });
  }
}

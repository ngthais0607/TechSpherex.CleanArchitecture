import { Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TYPES, OrderInput, ContainerType, ContainerSize } from '../../core/models/entities';
import { DemoRepository } from '../../demo/demo-repository';
import {
  assign,
  createOrder,
  eligible,
  lineCounts,
  release,
} from '../../demo/commands/order-commands';
import { dayKey } from '../../demo/rules/container-rules';
import { Badge, DynamicForm, Field, Icon, Modal, Pagination } from '../../shared/ui';
@Component({
  selector: 'app-orders',
  imports: [FormsModule, DatePipe, RouterLink, Badge, DynamicForm, Icon, Modal, Pagination],
  templateUrl: './orders-page.html',
})
export class OrdersPage {
  store = inject(DemoRepository);
  router = inject(Router);
  route = inject(ActivatedRoute);
  search = signal('');
  status = signal('');
  customer = signal('');
  page = signal(1);
  selected = signal('');
  showCreate = signal(false);
  showAssign = signal(false);
  checks = signal<string[]>([]);
  error = signal('');
  errors = signal<Record<string, string>>({});
  types = TYPES;
  lines: { type: ContainerType; size: ContainerSize; quantity: number }[] = [
    { type: 'Dry', size: 20, quantity: 1 },
  ];
  constructor() {
    this.route.queryParamMap.subscribe((q) => {
      this.selected.set(q.get('order') || '');
      this.status.set(q.get('status') || '');
      this.customer.set(q.get('customerId') || '');
    });
  }
  rows = computed(() =>
    this.store.state().orders.filter((o) => {
      const status = this.store.orderStatus(o.id);
      return (
        (!this.search() ||
          `${o.number} ${this.store.customer(o.customerId)}`
            .toLowerCase()
            .includes(this.search().toLowerCase())) &&
        (!this.customer() || o.customerId === this.customer()) &&
        (!this.status() ||
          (this.status() === 'active'
            ? ['Còn hạn', 'Sắp hết hạn'].includes(status)
            : status === this.status()))
      );
    }),
  );
  visible = computed(() => this.rows().slice((this.page() - 1) * 8, this.page() * 8));
  detail = computed(() => this.store.state().orders.find((o) => o.id === this.selected()));
  bookings = computed(() =>
    this.store.state().bookings.filter((b) => b.orderId === this.selected()),
  );
  eligibleContainers = computed(() =>
    this.store
      .containers()
      .filter((c) => eligible(this.store.state(), this.selected(), c.number, this.store.now()).ok),
  );
  count(status: string) {
    return this.store
      .state()
      .orders.filter((o) =>
        status === 'active'
          ? ['Còn hạn', 'Sắp hết hạn'].includes(this.store.orderStatus(o.id))
          : this.store.orderStatus(o.id) === status,
      ).length;
  }
  total(id: string) {
    return this.store
      .state()
      .orders.find((o) => o.id === id)!
      .lines.reduce((n, l) => n + l.quantity, 0);
  }
  shippedToday() {
    return this.store
      .state()
      .gateTransactions.filter(
        (t) =>
          t.direction === 'Out' &&
          t.status === 'Active' &&
          dayKey(new Date(t.at)) === dayKey(this.store.now()),
      ).length;
  }
  taxCode(id: string) {
    return this.store.state().customers.find((c) => c.id === id)?.taxCode;
  }
  container(number: string) {
    return this.store.containers().find((c) => c.number === number);
  }
  shipped(id: string) {
    return this.store.state().bookings.filter((b) => b.orderId === id && b.status === 'Shipped')
      .length;
  }
  counts(id: string) {
    return lineCounts(this.store.state(), id);
  }
  open(id: string) {
    this.error.set('');
    this.router.navigate([], { queryParams: { order: id }, queryParamsHandling: 'merge' });
  }
  close() {
    this.router.navigate([], { queryParams: { order: null }, queryParamsHandling: 'merge' });
  }
  filter(s: string) {
    this.status.set(this.status() === s ? '' : s);
    this.page.set(1);
    this.router.navigate([], {
      queryParams: { status: this.status() || null },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }
  fields(): Field[] {
    return [
      { key: 'number', label: 'Số lệnh', required: true },
      {
        key: 'customerId',
        label: 'Khách hàng',
        required: true,
        options: this.store
          .state()
          .customers.filter((c) => c.isActive)
          .map((c) => ({ value: c.id, label: c.taxCode + ' – ' + c.name })),
      },
      {
        key: 'operatorId',
        label: 'Hãng khai thác',
        required: true,
        options: this.store
          .state()
          .operators.filter((o) => o.isActive)
          .map((o) => ({ value: o.id, label: o.name })),
      },
      {
        key: 'expiry',
        label: 'Hạn lệnh',
        type: 'date',
        required: true,
        hint: 'Ngày cuối container được phép giao.',
      },
      { key: 'voyage', label: 'Chuyến tàu xuất' },
    ];
  }
  save(v: Record<string, unknown>) {
    const r = this.store.run(
      (s) =>
        createOrder(
          s,
          {
            ...v,
            lines: this.lines.map((l) => ({
              ...l,
              size: Number(l.size),
              quantity: Number(l.quantity),
            })),
          } as unknown as OrderInput,
          this.store.now(),
        ),
      'Đã tạo lệnh giao.',
    );
    if (!r.ok) {
      this.error.set(r.error.message);
      this.errors.set(r.error.fields || {});
    } else {
      this.showCreate.set(false);
      this.open(r.value);
    }
  }
  startCreate() {
    this.lines = [{ type: 'Dry', size: 20, quantity: 1 }];
    this.error.set('');
    this.errors.set({});
    this.showCreate.set(true);
  }
  toggle(number: string) {
    this.checks.update((a) =>
      a.includes(number) ? a.filter((n) => n !== number) : [...a, number],
    );
  }
  full(number: string) {
    const c = this.store.containers().find((c) => c.number === number)!,
      o = this.detail()!;
    const line = o.lines.find((l) => l.type === c.type && l.size === c.size)!;
    const chosen = this.checks().filter((n) => {
      const c = this.store.containers().find((c) => c.number === n)!;
      return c.type === line.type && c.size === line.size;
    }).length;
    return !this.checks().includes(number) && chosen >= this.counts(line.id).remaining;
  }
  assignSelected() {
    const r = this.store.run(
      (s) => assign(s, this.selected(), this.checks(), this.store.now()),
      'Đã gán container vào lệnh.',
    );
    if (!r.ok) this.error.set(r.error.message);
    else {
      this.showAssign.set(false);
      this.checks.set([]);
    }
  }
  unassign(id: string) {
    if (confirm('Gỡ container khỏi lệnh? Lịch sử đặt chỗ vẫn được giữ.')) {
      const r = this.store.run((s) => release(s, id, this.store.now()), 'Đã gỡ đặt chỗ.');
      if (!r.ok) this.error.set(r.error.message);
    }
  }
  bookingLabel(s: string) {
    return (
      {
        Reserved: 'Đang giữ chỗ',
        Shipped: 'Đã xuất',
        Cancelled: 'Đã hủy',
        Expired: 'Hết hạn',
      } as Record<string, string>
    )[s];
  }
  cancelCreate(dirty: boolean) {
    if (!dirty || confirm('Bỏ các thay đổi chưa lưu?')) this.showCreate.set(false);
  }
}

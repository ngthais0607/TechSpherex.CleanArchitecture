import { Injectable, computed, signal } from '@angular/core';
import { Container, DepotSnapshot, Result, Position } from '../core/models/entities';
import { createDemoSnapshot } from './seed';
import { assertSnapshot, STORAGE_KEY } from './storage';
import { activeLife, ok, fail, positionText } from './rules/container-rules';
import { expire, orderStatus, reserved } from './commands/order-commands';
@Injectable({ providedIn: 'root' })
export class DemoRepository {
  readonly state = signal<DepotSnapshot>(createDemoSnapshot());
  readonly now = signal(new Date());
  readonly warning = signal('');
  readonly toast = signal('');
  readonly readError = signal(false);
  readonly loading = signal(true);
  readonly containers = computed(() => this.state().containers);
  constructor() {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (raw) {
        const r = assertSnapshot(JSON.parse(raw));
        if (r.ok) this.state.set(r.value);
        else this.warning.set(r.error.message);
      }
    } catch {
      this.warning.set('Không đọc được dữ liệu đã lưu. Bạn có thể khôi phục dữ liệu mẫu.');
    }
    this.refresh();
    setInterval(() => this.refresh(), 60000);
    globalThis.addEventListener?.('focus', () => this.refresh());
    setTimeout(() => this.loading.set(false), 180);
  }
  refresh() {
    this.now.set(new Date());
    const next = structuredClone(this.state());
    expire(next, this.now());
    if (JSON.stringify(next.bookings) !== JSON.stringify(this.state().bookings)) {
      this.state.set(next);
      this.persist(next);
    }
  }
  run<T>(fn: (draft: DepotSnapshot) => Result<T>, message = 'Đã lưu thay đổi.'): Result<T> {
    const draft = structuredClone(this.state());
    expire(draft, this.now());
    const r = fn(draft);
    if (!r.ok) return r;
    draft.revision++;
    this.state.set(draft);
    this.persist(draft);
    this.notify(message);
    return r;
  }
  reset(empty = false) {
    const next = createDemoSnapshot();
    if (empty) {
      next.containers = [];
      next.orders = [];
      next.bookings = [];
      next.lifecycles = [];
      next.gateTransactions = [];
      next.movements = [];
    }
    this.state.set(next);
    this.warning.set('');
    this.persist(next);
    this.notify(empty ? 'Đã chuyển sang dữ liệu trống.' : 'Đã khôi phục dữ liệu mẫu.');
  }
  private persist(s: DepotSnapshot) {
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(s));
    } catch {
      this.warning.set('Không lưu được trong trình duyệt. Thay đổi hiện chỉ giữ trong bộ nhớ.');
    }
  }
  notify(message: string) {
    this.toast.set(message);
    setTimeout(() => this.toast.set(''), 3500);
  }
  operator(id: string) {
    return this.state().operators.find((o) => o.id === id)?.name ?? '—';
  }
  customer(id: string) {
    return this.state().customers.find((c) => c.id === id)?.name ?? '—';
  }
  position(number: string) {
    return activeLife(this.state(), number)?.position;
  }
  location(number: string) {
    return positionText(this.position(number));
  }
  status(number: string) {
    const s = this.state(),
      life = activeLife(s, number);
    if (!life) return s.lifecycles.some((l) => l.number === number) ? 'Đã xuất' : 'Chờ nhập';
    if (reserved(s, number, this.now())) return 'Chờ xuất';
    return life.position?.kind === 'virtual' ? 'Chờ phân vị trí' : 'Trong bãi';
  }
  booking(number: string) {
    return reserved(this.state(), number, this.now());
  }
  orderStatus(id: string) {
    return orderStatus(this.state(), id, this.now());
  }
  retry() {
    this.readError.set(false);
    this.loading.set(true);
    setTimeout(() => this.loading.set(false), 180);
  }
  failRead() {
    this.readError.set(true);
  }
}

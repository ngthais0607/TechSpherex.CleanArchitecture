import { Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Container, CONDITIONS, TYPES } from '../../core/models/entities';
import { DemoRepository } from '../../demo/demo-repository';
import { activeLife, normalizeNumber, register } from '../../demo/rules/container-rules';
import { Badge, DynamicForm, Field, Icon, Modal, Pagination } from '../../shared/ui';
import { ContainerDetail } from './container-detail';
@Component({
  selector: 'app-containers',
  imports: [FormsModule, DatePipe, Badge, DynamicForm, Icon, Modal, Pagination, ContainerDetail],
  templateUrl: './containers-page.html',
})
export class ContainersPage {
  store = inject(DemoRepository);
  router = inject(Router);
  route = inject(ActivatedRoute);
  types = TYPES;
  conditions = CONDITIONS;
  search = signal('');
  type = signal('');
  booking = signal('');
  operator = signal('');
  condition = signal('');
  page = signal(1);
  sort = signal('number');
  showCreate = signal(false);
  selected = signal('');
  errors = signal<Record<string, string>>({});
  error = signal('');
  constructor() {
    this.route.queryParamMap.subscribe((q) => {
      this.search.set(q.get('search') || '');
      this.type.set(TYPES.includes(q.get('type') as never) ? q.get('type')! : '');
      this.page.set(Math.max(1, Number(q.get('page')) || 1));
      this.selected.set(q.get('container') || '');
    });
  }
  filtered = computed(() =>
    this.store
      .containers()
      .filter(
        (c) =>
          (!this.search() ||
            normalizeNumber(c.number).includes(normalizeNumber(this.search())) ||
            c.iso.includes(this.search().toUpperCase())) &&
          (!this.operator() || c.operatorId === this.operator()) &&
          (!this.condition() || c.condition === this.condition()) &&
          (!this.booking() ||
            (this.booking() === 'assigned'
              ? !!this.store.booking(c.number)
              : !this.store.booking(c.number))),
      ),
  );
  rows = computed(() =>
    this.filtered()
      .filter((c) => !this.type() || c.type === this.type())
      .sort((a, b) =>
        this.sort() === 'size' ? a.size - b.size : a.number.localeCompare(b.number),
      ),
  );
  visible = computed(() => this.rows().slice((this.page() - 1) * 8, this.page() * 8));
  count(t: string) {
    return this.filtered().filter((c) => !t || c.type === t).length;
  }
  update() {
    this.page.set(1);
    this.router.navigate([], {
      queryParams: { search: this.search() || null, type: this.type() || null, page: null },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }
  open(number: string) {
    this.router.navigate([], { queryParams: { container: number }, queryParamsHandling: 'merge' });
  }
  close() {
    this.router.navigate([], { queryParams: { container: null }, queryParamsHandling: 'merge' });
  }
  next(p: number) {
    this.page.set(p);
    this.router.navigate([], {
      queryParams: { page: p },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }
  fields(): Field[] {
    return [
      {
        key: 'number',
        label: 'Số container',
        required: true,
        section: 'Nhận dạng',
        hint: '4 chữ cái + 7 chữ số, gồm số kiểm tra ISO 6346.',
      },
      {
        key: 'ownerCode',
        label: 'Chủ sở hữu',
        readonly: true,
        hint: 'Tự lấy từ 4 chữ đầu số container.',
      },
      {
        key: 'type',
        label: 'Loại container',
        required: true,
        options: TYPES.map((v) => ({ value: v, label: v })),
      },
      {
        key: 'size',
        label: 'Kích thước',
        required: true,
        options: [
          { value: 20, label: '20ft' },
          { value: 40, label: '40ft' },
        ],
      },
      { key: 'iso', label: 'Mã ISO', required: true, hint: 'Ví dụ: 22G1 (20ft), 45G1 (40ft).' },
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
        key: 'grade',
        label: 'Phân loại',
        required: true,
        section: 'Đặc tính',
        options: ['A', 'B', 'C'].map((v) => ({ value: v, label: v })),
      },
      {
        key: 'condition',
        label: 'Tình trạng',
        required: true,
        options: CONDITIONS.map((v) => ({ value: v, label: v })),
      },
      {
        key: 'maxWeight',
        label: 'Trọng lượng tối đa (kg)',
        type: 'number',
        required: true,
        min: 1,
        section: 'Thông số kỹ thuật',
      },
      { key: 'tareWeight', label: 'Trọng lượng vỏ (kg)', type: 'number', required: true, min: 1 },
      { key: 'manufactureMonth', label: 'Tháng sản xuất', type: 'month', required: true },
    ];
  }
  defaults = {
    type: 'Dry',
    size: 20,
    grade: 'A',
    condition: 'Bình thường',
    maxWeight: 30480,
    tareWeight: 2200,
    manufactureMonth: '2021-06',
  };
  closeDirty() {
    if (confirm('Bỏ các thay đổi chưa lưu?')) this.showCreate.set(false);
  }
  save(value: Record<string, unknown>) {
    const c = {
      ...value,
      size: Number(value['size']),
      maxWeight: Number(value['maxWeight']),
      tareWeight: Number(value['tareWeight']),
    } as unknown as Container;
    const r = this.store.run(
      (s) => register(s, c, this.store.now()),
      'Đã thêm container. Có thể ghi nhận nhập bãi.',
    );
    if (!r.ok) {
      this.errors.set(r.error.fields || {});
      this.error.set(r.error.message);
    } else {
      this.showCreate.set(false);
      this.search.set('');
      this.type.set('');
      this.update();
    }
  }
  entered(number: string) {
    return activeLife(this.store.state(), number)?.inAt;
  }
  currentOrder(number: string) {
    const b = this.store.booking(number);
    return b ? this.store.state().orders.find((o) => o.id === b.orderId) : null;
  }
}

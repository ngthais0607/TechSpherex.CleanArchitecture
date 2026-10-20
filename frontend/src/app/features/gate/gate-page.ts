import { Component, computed, inject, signal, DestroyRef } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormControl, FormGroup, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { debounceTime, switchMap, timer, tap } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CONDITIONS, GateInput, GateCheck, Grade } from '../../core/models/entities';
import { DemoRepository } from '../../demo/demo-repository';
import {
  evaluateGate,
  gateIn,
  gateOut,
  cancelGate,
  editGate,
} from '../../demo/commands/gate-commands';
import { normalizeNumber, positionText } from '../../demo/rules/container-rules';
import { suggest } from '../../demo/rules/yard-rules';
import { Badge, DynamicForm, Field, Icon, Modal, Pagination } from '../../shared/ui';
@Component({
  selector: 'app-gate',
  imports: [
    DatePipe,
    ReactiveFormsModule,
    FormsModule,
    RouterLink,
    Badge,
    DynamicForm,
    Icon,
    Modal,
    Pagination,
  ],
  templateUrl: './gate-page.html',
  styles: `
    .gate-layout {
      display: grid;
      grid-template-columns: minmax(0, 1.65fr) minmax(280px, 1fr);
      gap: 20px;
    }
    .gate-section {
      padding: 22px;
      border-bottom: 1px solid var(--border);
    }
    .gate-section h3 {
      font-size: 11px;
      color: var(--muted);
      letter-spacing: 0.7px;
      text-transform: uppercase;
      margin: 0 0 18px;
    }
    .gate-section .form-grid {
      gap: 16px 18px;
    }
    .position-grid {
      display: grid;
      grid-template-columns: repeat(3, minmax(0, 1fr));
      gap: 14px;
      margin-top: 18px;
    }
    .vehicle-toggle {
      display: flex;
      gap: 10px;
      margin-bottom: 18px;
    }
    .vehicle-toggle button {
      flex: 1;
    }
    .vehicle-toggle button.active {
      background: #e9fafc;
      border-color: #00a5c2;
      color: #00849f;
    }
    .gate-checks {
      position: sticky;
      top: 86px;
      align-self: start;
    }
    .check {
      display: flex;
      gap: 12px;
      align-items: flex-start;
      padding: 14px;
      border: 1px solid #eaf0f5;
      background: #f8fafc;
      border-radius: 10px;
      margin-bottom: 10px;
    }
    .check:last-child {
      margin-bottom: 0;
    }
    .check > span {
      width: 26px;
      height: 26px;
      border-radius: 50%;
      background: #e3eaf1;
      color: #8193a7;
      display: grid;
      place-items: center;
      flex-shrink: 0;
      font-size: 13px;
    }
    .check.pass > span {
      background: #e4f8ee;
      color: #029475;
    }
    .check.error > span {
      background: #ffe9ed;
      color: #c8304d;
    }
    .check strong {
      font-size: 12px;
      font-weight: 600;
    }
    .check small {
      display: block;
      font-size: 11px;
      color: var(--muted);
      line-height: 1.7;
      margin-top: 4px;
    }
    .gate-actions {
      padding: 18px 22px;
      display: flex;
      justify-content: flex-end;
      gap: 10px;
    }
    .rule-note {
      font-size: 11px;
      padding: 16px;
      line-height: 1.8;
      background: #fffbed;
      border: 1px solid #f1e1a9;
      color: #a06c13;
      border-radius: 10px;
      margin-top: 18px;
    }
    .full-width {
      grid-column: 1/-1;
    }
    @media (max-width: 1000px) {
      .gate-layout {
        grid-template-columns: 1fr;
      }
      .gate-checks {
        position: static;
      }
      .gate-checks .panel-body {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 10px;
      }
      .check {
        margin: 0;
      }
    }
    @media (max-width: 800px) {
      .gate-section .form-grid {
        grid-template-columns: 1fr 1fr;
      }
      .gate-section {
        padding: 18px;
      }
    }
  `,
})
export class GatePage {
  store = inject(DemoRepository);
  router = inject(Router);
  route = inject(ActivatedRoute);
  destroy = inject(DestroyRef);
  conditions = CONDITIONS;
  tab = signal('in');
  search = signal('');
  page = signal(1);
  selected = signal('');
  editing = signal(false);
  error = signal('');
  checking = signal(false);
  checks = signal<GateCheck[]>([]);
  form = new FormGroup({
    number: new FormControl('', { nonNullable: true }),
    orderId: new FormControl('', { nonNullable: true }),
    vehicleType: new FormControl<'Truck' | 'Barge'>('Truck', { nonNullable: true }),
    vehicleNo: new FormControl('', { nonNullable: true }),
    driver: new FormControl('', { nonNullable: true }),
    grade: new FormControl<Grade>('A', { nonNullable: true }),
    condition: new FormControl('Bình thường', { nonNullable: true }),
    blockId: new FormControl('A', { nonNullable: true }),
    bay: new FormControl(1, { nonNullable: true }),
    row: new FormControl('A', { nonNullable: true }),
    tier: new FormControl(1, { nonNullable: true }),
  });
  constructor() {
    this.route.queryParamMap.subscribe((q) => {
      this.tab.set(['in', 'out', 'history'].includes(q.get('tab') || '') ? q.get('tab')! : 'in');
      this.selected.set(q.get('transaction') || '');
      if (q.get('container')) this.form.controls.number.setValue(q.get('container')!);
      this.recheck();
    });
    this.form.valueChanges
      .pipe(
        tap(() => {
          this.checking.set(true);
          const n = normalizeNumber(this.form.controls.number.value),
            b = this.store.booking(n);
          if (this.tab() === 'out' && b && !this.form.controls.orderId.value)
            this.form.controls.orderId.setValue(b.orderId, { emitEvent: false });
        }),
        debounceTime(300),
        switchMap(() => timer(80)),
        takeUntilDestroyed(this.destroy),
      )
      .subscribe(() => this.recheck());
  }
  input(): GateInput {
    const v = this.form.getRawValue();
    const b = this.store.state().blocks.find((b) => b.id === v.blockId);
    return {
      ...v,
      number: normalizeNumber(v.number),
      target: b?.virtual
        ? { kind: 'virtual', blockId: b.id }
        : {
            kind: 'physical',
            blockId: v.blockId,
            bay: Number(v.bay),
            row: v.row,
            tier: Number(v.tier),
          },
    };
  }
  recheck() {
    this.checks.set(
      evaluateGate(
        this.store.state(),
        this.input(),
        this.tab() === 'out' ? 'Out' : 'In',
        this.store.now(),
      ),
    );
    this.checking.set(false);
  }
  valid = computed(
    () =>
      !this.checking() &&
      this.checks().length > 0 &&
      this.checks().every((c) => c.state === 'pass'),
  );
  order() {
    return this.store.state().orders.find((o) => o.id === this.form.controls.orderId.value);
  }
  operator() {
    const c = this.store
      .containers()
      .find((c) => c.number === normalizeNumber(this.form.controls.number.value));
    return c ? this.store.operator(c.operatorId) : 'Tự động theo danh mục';
  }
  block() {
    return this.store.state().blocks.find((b) => b.id === this.form.controls.blockId.value);
  }
  bays() {
    return Array.from(
      { length: this.block()?.oddBays || 0 },
      (_, i) =>
        i * 2 +
        (this.store
          .containers()
          .find((c) => c.number === normalizeNumber(this.form.controls.number.value))?.size === 40
          ? 2
          : 1),
    ).filter((b) => b <= (this.block()?.oddBays || 0) * 2 - 1);
  }
  rows() {
    return Array.from({ length: this.block()?.rows || 0 }, (_, i) => String.fromCharCode(65 + i));
  }
  tiers() {
    return Array.from({ length: this.block()?.tiers || 0 }, (_, i) => i + 1);
  }
  setTab(tab: string) {
    this.router.navigate([], {
      queryParams: { tab, transaction: null },
      queryParamsHandling: 'merge',
    });
    this.error.set('');
  }
  suggest() {
    const r = suggest(
      this.store.state(),
      normalizeNumber(this.form.controls.number.value),
      this.form.controls.blockId.value,
    );
    if (!r.ok) this.error.set(r.error.message);
    else {
      if (r.value.kind === 'physical') this.form.patchValue(r.value);
      this.error.set('');
      this.recheck();
    }
  }
  submit() {
    const r = this.store.run(
      (s) =>
        this.tab() === 'in'
          ? gateIn(s, this.input(), this.store.now())
          : gateOut(s, this.input(), this.store.now()),
      this.tab() === 'in' ? 'Đã ghi nhận nhập bãi.' : 'Đã ghi nhận xuất bãi.',
    );
    if (!r.ok) this.error.set(`[${r.error.code}] ${r.error.message}`);
    else {
      this.error.set('');
      this.form.patchValue({ number: '', orderId: '', vehicleNo: '', driver: '' });
      this.form.markAsPristine();
      this.recheck();
    }
  }
  clear() {
    if (!this.form.dirty || confirm('Xóa nội dung đang nhập?')) {
      this.form.reset({
        number: '',
        orderId: '',
        vehicleType: 'Truck',
        vehicleNo: '',
        driver: '',
        grade: 'A',
        condition: 'Bình thường',
        blockId: 'A',
        bay: 1,
        row: 'A',
        tier: 1,
      });
      this.error.set('');
      this.recheck();
    }
  }
  history = computed(() =>
    [...this.store.state().gateTransactions]
      .filter(
        (t) =>
          !this.search() ||
          `${t.number} ${t.vehicleNo} ${t.driver}`
            .toLowerCase()
            .includes(this.search().toLowerCase()),
      )
      .sort((a, b) => b.at.localeCompare(a.at)),
  );
  visible = computed(() => this.history().slice((this.page() - 1) * 8, this.page() * 8));
  detail = computed(() =>
    this.store.state().gateTransactions.find((t) => t.id === this.selected()),
  );
  open(id: string) {
    this.error.set('');
    this.router.navigate([], { queryParams: { transaction: id }, queryParamsHandling: 'merge' });
  }
  close() {
    this.router.navigate([], { queryParams: { transaction: null }, queryParamsHandling: 'merge' });
  }
  canCancel(id: string) {
    const tx = this.store.state().gateTransactions.find((t) => t.id === id);
    return (
      tx?.status === 'Active' &&
      this.store
        .state()
        .gateTransactions.filter((t) => t.number === tx.number && t.status === 'Active')
        .at(-1)?.id === id
    );
  }
  cancel(id: string) {
    if (
      confirm('Hủy giao dịch này? Dữ liệu container và đặt chỗ sẽ được khôi phục nếu còn hợp lệ.')
    ) {
      const r = this.store.run((s) => cancelGate(s, id, this.store.now()), 'Đã hủy giao dịch.');
      if (!r.ok) this.error.set(`[${r.error.code}] ${r.error.message}`);
    }
  }
  editFields: Field[] = [
    {
      key: 'vehicleType',
      label: 'Loại phương tiện',
      required: true,
      options: [
        { value: 'Truck', label: 'Xe đầu kéo' },
        { value: 'Barge', label: 'Sà lan' },
      ],
    },
    { key: 'vehicleNo', label: 'Biển số / số hiệu', required: true },
    { key: 'driver', label: 'Tài xế', required: true },
    {
      key: 'grade',
      label: 'Phân loại',
      required: true,
      options: ['A', 'B', 'C'].map((v) => ({ value: v, label: v })),
    },
    {
      key: 'condition',
      label: 'Tình trạng',
      required: true,
      options: CONDITIONS.map((v) => ({ value: v, label: v })),
    },
  ];
  editValue() {
    return { ...this.detail() } as Record<string, unknown>;
  }
  saveEdit(v: Record<string, unknown>) {
    const r = this.store.run(
      (s) => editGate(s, this.selected(), v as never, this.store.now()),
      'Đã cập nhật giao dịch và lưu lịch sử.',
    );
    if (!r.ok) this.error.set(r.error.message);
    else this.editing.set(false);
  }
  positionText = positionText;
  hasUnsavedChanges() {
    return this.form.dirty;
  }
  orderNumber(id: string) {
    return this.store.state().orders.find((o) => o.id === id)?.number || '—';
  }
}

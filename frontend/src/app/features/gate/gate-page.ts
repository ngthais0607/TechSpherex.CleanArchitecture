import { Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DynamicForm, Field, Modal, Pagination } from '../../shared/ui';
import { GateFacade } from './gate-facade';
import { mapGateChecks } from './gate.mapper';
import { YardApi } from '../../core/api/yard-api';
import { ReferenceData } from '../../core/api/reference-data';
import { LookupsDto } from '../../core/api/lookups.dto';
import { YardBlockDto, YardPositionDto } from '../../core/api/yard.dto';
import { GateTransactionDto, ValidateGateInput } from '../../core/api/gate.dto';
import { Notifications } from '../../core/notifications/notifications';
import { normalizeContainerNumber } from '../../core/presentation/container-number';
@Component({
  selector: 'app-gate',
  imports: [DatePipe, FormsModule, DynamicForm, Modal, Pagination],
  providers: [GateFacade],
  templateUrl: './gate-page.html',
})
export class GatePage {
  readonly facade = inject(GateFacade);
  readonly yard = inject(YardApi);
  readonly notify = inject(Notifications);
  readonly blocks = signal<YardBlockDto[]>([]);
  readonly lookups = signal<LookupsDto | null>(null);
  readonly dirty = signal(false);
  readonly editing = signal(false);
  readonly cancelReason = signal('');
  readonly suggestions = signal<YardPositionDto[]>([]);
  readonly freePage = signal(1);
  readonly freeTotal = signal(0);
  readonly checks = computed(() => mapGateChecks(this.facade.validation()?.checks ?? []));
  form = {
    type: 'GateIn' as 'GateIn' | 'GateOut',
    containerNumber: '',
    orderNumber: '',
    blockCode: '',
    bay: 1,
    row: 1,
    tier: 1,
    vehicleNumber: '',
    vehicleType: 'Truck',
    driverName: '',
    grade: 'A',
    condition: 'Normal',
  };
  history = { search: '', type: '', status: '', pageIndex: 1 };
  constructor() {
    inject(ReferenceData)
      .lookups()
      .pipe(takeUntilDestroyed())
      .subscribe({
        next: (value) => this.lookups.set(value),
        error: (error) => this.notify.error(error.detail),
      });
    this.yard
      .blocks()
      .pipe(takeUntilDestroyed())
      .subscribe({
        next: (blocks) => this.blocks.set(blocks),
        error: (error) => this.notify.error(error.detail),
      });
    const number = inject(ActivatedRoute).snapshot.queryParamMap.get('container');
    if (number) this.form.containerNumber = number;
    this.loadHistory();
    if (number) this.changed();
  }
  hasUnsavedChanges(): boolean {
    return this.dirty();
  }
  options(key: keyof LookupsDto) {
    return Object.entries(this.lookups()?.[key] ?? {}).map(([value, label]) => ({ value, label }));
  }
  changed(): void {
    this.dirty.set(true);
    const number = normalizeContainerNumber(this.form.containerNumber);
    if (number.length !== 11) {
      this.facade.setInput(null);
      return;
    }
    const block = this.blocks().find((block) => block.code === this.form.blockCode);
    const input: ValidateGateInput = { containerNumber: number, type: this.form.type };
    if (this.form.type === 'GateOut') input.orderNumber = this.form.orderNumber.trim();
    else if (block?.isVirtual) input.virtualBlockCode = block.code;
    else if (block) {
      input.blockCode = block.code;
      input.bay = Number(this.form.bay);
      input.row = Number(this.form.row);
      input.tier = Number(this.form.tier);
    }
    this.facade.setInput(input);
  }
  suggest(page = 1): void {
    const block = this.blocks().find((block) => block.code === this.form.blockCode),
      container = this.facade.validation()?.container;
    if (!block || block.isVirtual || !container) return;
    this.yard.freeSlots(block.id, container.size, page).subscribe({
      next: (result) => {
        this.suggestions.set(result.items);
        this.freePage.set(result.pageIndex);
        this.freeTotal.set(result.totalCount);
      },
      error: (error) => this.notify.error(error.detail),
    });
  }
  useSlot(slot: YardPositionDto): void {
    this.form.bay = slot.bay;
    this.form.row = slot.row;
    this.form.tier = slot.tier;
    this.changed();
  }
  submit(): void {
    if (this.facade.pending()) return;
    this.facade
      .submit({
        vehicleNumber: this.form.vehicleNumber.trim(),
        vehicleType: this.form.vehicleType,
        driverName: this.form.driverName.trim(),
        grade: this.form.grade,
        condition: this.form.condition,
      })
      .subscribe({
        next: () => {
          this.dirty.set(false);
          this.form.containerNumber = '';
          this.facade.setInput(null);
          this.notify.success('Đã ghi nhận giao dịch cổng.');
        },
        error: () => {},
      });
  }
  loadHistory(pageIndex = 1): void {
    this.history.pageIndex = pageIndex;
    this.facade.setQuery({ ...this.history });
  }
  open(row: GateTransactionDto): void {
    this.facade.selected.set(row);
    this.editing.set(false);
    this.cancelReason.set('');
  }
  editFields(): Field[] {
    return [
      { key: 'vehicleNumber', label: 'Biển số xe', required: true },
      { key: 'vehicleType', label: 'Loại phương tiện', options: this.options('vehicleTypes') },
      { key: 'driverName', label: 'Tài xế' },
      {
        key: 'grade',
        label: 'Phân loại (để trống nếu giữ nguyên)',
        options: this.options('classifications'),
      },
      {
        key: 'condition',
        label: 'Tình trạng (để trống nếu giữ nguyên)',
        options: this.options('containerConditions'),
      },
      { key: 'note', label: 'Ghi chú thay đổi' },
    ];
  }
  editValue() {
    const row = this.facade.selected();
    return {
      vehicleNumber: row?.vehicleNumber,
      vehicleType: row?.vehicleType,
      driverName: row?.driverName ?? '',
    };
  }
  update(value: Record<string, unknown>): void {
    const row = this.facade.selected();
    if (!row) return;
    const input = Object.fromEntries(
      Object.entries(value)
        .filter(([, value]) => value !== '' && value !== null)
        .map(([key, value]) => [key, String(value)]),
    );
    this.facade.update(row.id, input).subscribe({
      next: () => {
        this.editing.set(false);
        this.notify.success('Đã cập nhật giao dịch.');
      },
      error: () => {},
    });
  }
  cancel(): void {
    const row = this.facade.selected();
    if (!row || !confirm('Hủy giao dịch và hoàn tác trạng thái/vị trí liên quan?')) return;
    this.facade.cancel(row.id, this.cancelReason()).subscribe({
      next: () => {
        this.facade.selected.set(null);
        this.notify.success('Đã hủy giao dịch.');
      },
      error: () => {},
    });
  }
}

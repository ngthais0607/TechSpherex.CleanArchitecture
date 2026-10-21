import { Component, computed, effect, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Badge, DynamicForm, Field, Icon, Modal, Pagination } from '../../shared/ui';
import { AuthService } from '../../core/auth/auth-service';
import { can } from '../../core/auth/permissions';
import { ReferenceData } from '../../core/api/reference-data';
import { LookupsDto } from '../../core/api/lookups.dto';
import { ApiError } from '../../core/api/api-error';
import { Notifications } from '../../core/notifications/notifications';
import { containerStatusLabels } from '../../core/presentation/container-labels';
import { ContainerDetail } from './container-detail';
import { ContainersFacade } from './containers-facade';
import { containerConditions, containerTypes, registrationRequest } from './container.mapper';
@Component({
  selector: 'app-containers', imports: [FormsModule, DatePipe, Badge, DynamicForm, Icon, Modal, Pagination, ContainerDetail],
  providers: [ContainersFacade], templateUrl: './containers-page.html',
})
export class ContainersPage {
  readonly facade = inject(ContainersFacade);
  readonly router = inject(Router);
  readonly route = inject(ActivatedRoute);
  readonly auth = inject(AuthService);
  readonly notify = inject(Notifications);
  readonly reference = inject(ReferenceData);
  readonly lookups = signal<LookupsDto | null>(null);
  readonly canRegister = computed(() => can('registerContainer', this.auth.user()?.roles ?? []));
  readonly types = containerTypes;
  readonly conditions = containerConditions;
  readonly search = signal('');
  readonly type = signal('');
  readonly booking = signal('');
  readonly operator = signal('');
  readonly condition = signal('');
  readonly sort = signal('number');
  readonly selected = signal('');
  readonly showCreate = signal(false);
  readonly errors = signal<Record<string, string>>({});
  readonly error = signal('');
  readonly statusLabels = containerStatusLabels;
  readonly defaults = { type: 'Dry', size: 20, grade: 'A', condition: 'Normal', maxGrossWeight: 30480, tareWeight: 2200, manufactureMonth: '' };
  constructor() {
    this.reference.lookups().pipe(takeUntilDestroyed()).subscribe({ next: value => this.lookups.set(value), error: error => this.notify.error(error.detail) });
    this.route.queryParamMap.pipe(takeUntilDestroyed()).subscribe(params => {
      this.facade.setQuery(Object.fromEntries(params.keys.map(key => [key, params.get(key)])));
      const query = this.facade.query();
      this.search.set(String(query['search'] ?? '')); this.type.set(String(query['type'] ?? ''));
      this.booking.set(String(query['booking'] ?? '')); this.operator.set(String(query['lineOperator'] ?? ''));
      this.condition.set(String(query['condition'] ?? '')); this.sort.set(String(query['sort'] ?? 'number'));
      const number = params.get('container') ?? '';
      if (number !== this.selected()) { this.selected.set(number); this.facade.select(number || null); }
    });
    effect(() => {
      if (this.facade.detailError()?.status === 404) { this.notify.error('Container không còn tồn tại hoặc không thuộc depot hiện tại.'); this.close(); }
    });
  }
  count(type: string): number { const summary = this.facade.summary(); return type ? summary?.byType[type] ?? 0 : summary?.total ?? 0; }
  update(): void {
    this.router.navigate([], { queryParams: { search: this.search() || null, type: this.type() || null,
      booking: this.booking() || null, lineOperator: this.operator() || null, condition: this.condition() || null,
      sort: this.sort(), pageIndex: 1, page: null }, queryParamsHandling: 'merge', replaceUrl: true });
  }
  next(pageIndex: number): void { this.router.navigate([], { queryParams: { pageIndex, page: null }, queryParamsHandling: 'merge', replaceUrl: true }); }
  open(number: string): void { this.router.navigate([], { queryParams: { container: number }, queryParamsHandling: 'merge' }); }
  close(): void { this.router.navigate([], { queryParams: { container: null }, queryParamsHandling: 'merge' }); }
  fields(): Field[] {
    const dictionary = this.lookups();
    return [
      { key: 'number', label: 'Số container', required: true, section: 'Nhận dạng', hint: '4 chữ cái + 7 chữ số, gồm số kiểm tra ISO 6346.' },
      { key: 'type', label: 'Loại container', required: true, options: this.types.map(value => ({ value, label: dictionary?.containerTypes[value] ?? value })) },
      { key: 'size', label: 'Kích thước', required: true, options: [{value:20,label:'20ft'}, {value:40,label:'40ft'}] },
      { key: 'isoCode', label: 'Mã ISO', required: true, hint: 'Ví dụ: 22G1 (20ft), 42G1 (40ft).' },
      { key: 'grade', label: 'Phân loại', required: true, options: ['A','B','C'].map(value => ({value,label:dictionary?.classifications[value] ?? value})) },
      { key: 'condition', label: 'Tình trạng', required: true, options: this.conditions.map(value => ({value,label:dictionary?.containerConditions[value] ?? value})) },
      { key: 'maxGrossWeight', label: 'Trọng lượng tối đa (kg)', type:'number', required:true,min:1,section:'Thông số kỹ thuật' },
      { key: 'tareWeight', label:'Trọng lượng vỏ (kg)',type:'number',required:true,min:1 },
      { key: 'manufactureMonth',label:'Tháng sản xuất',type:'month' },
    ];
  }
  save(value: Record<string, unknown>): void {
    if (this.facade.pending() || !this.canRegister()) return;
    this.error.set(''); this.errors.set({});
    this.facade.register(registrationRequest(value)).subscribe({
      next: () => { this.showCreate.set(false); this.notify.success('Đã đăng ký container trên máy chủ.'); },
      error: (error: ApiError) => {
        this.error.set(error.detail);
        this.errors.set(Object.fromEntries(Object.entries(error.errors ?? {}).map(([key, messages]) => [key.charAt(0).toLowerCase()+key.slice(1),messages.join(' ')])));
      },
    });
  }
}

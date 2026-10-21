import { Component, effect, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  catchError,
  debounceTime,
  EMPTY,
  finalize,
  map,
  Observable,
  Subject,
  switchMap,
} from 'rxjs';
import { PartnersApi } from '../api/partners-api';
import { ApiError } from '../api/api-error';
import { Pagination } from '../../shared/ui';
export interface MasterChoice {
  id: string;
  code: string;
  name: string;
  isActive: boolean;
}
@Component({
  selector: 'app-master-picker',
  imports: [FormsModule, Pagination],
  template: `<div class="field">
    <label>{{ label() }}</label>
    <input
      [attr.aria-label]="'Tìm ' + label().toLowerCase()"
      placeholder="Tìm theo tên hoặc mã…"
      [ngModel]="search()"
      [ngModelOptions]="{ standalone: true }"
      (ngModelChange)="search.set($event); page.set(1)"
    />
    @if (selected(); as value) {
      <p class="banner">Đã chọn: {{ value.name }} · {{ value.code }}</p>
    }
    @if (loading()) {
      <p role="status">Đang tải danh mục…</p>
    }
    @if (error(); as error) {
      <p class="field-error" role="alert">{{ error.detail }}</p>
    }
    <div class="toolbar-group">
      @for (choice of choices(); track choice.id) {
        <button
          type="button"
          class="button small"
          [disabled]="!choice.isActive"
          (click)="select(choice)"
        >
          {{ choice.name }} · {{ choice.code }}{{ choice.isActive ? '' : ' (ngưng hoạt động)' }}
        </button>
      }
    </div>
    <app-pagination [total]="total()" [page]="page()" (changed)="page.set($event)" />
  </div>`,
})
export class MasterPicker {
  readonly kind = input.required<'customer' | 'operator'>();
  readonly label = input('Danh mục');
  readonly picked = output<MasterChoice>();
  readonly selected = signal<MasterChoice | null>(null);
  readonly search = signal('');
  readonly page = signal(1);
  readonly choices = signal<MasterChoice[]>([]);
  readonly total = signal(0);
  readonly loading = signal(false);
  readonly error = signal<ApiError | null>(null);
  private readonly api = inject(PartnersApi);
  private readonly requests = new Subject<{
    kind: 'customer' | 'operator';
    search: string;
    page: number;
  }>();
  constructor() {
    this.requests
      .pipe(
        debounceTime(300),
        switchMap((query) => {
          this.loading.set(true);
          this.error.set(null);
          const params = { search: query.search, pageIndex: query.page, pageSize: 8 };
          const source: Observable<{ items: MasterChoice[]; total: number }> =
            query.kind === 'customer'
              ? this.api
                  .customers(params)
                  .pipe(
                    map((page) => ({
                      items: page.items.map((row) => ({ ...row, code: row.taxCode })),
                      total: page.totalCount,
                    })),
                  )
              : this.api
                  .operators(params)
                  .pipe(map((page) => ({ items: page.items, total: page.totalCount })));
          return source.pipe(
            map((page) => {
              this.choices.set(page.items);
              this.total.set(page.total);
            }),
            catchError((error) => {
              this.error.set(error);
              return EMPTY;
            }),
            finalize(() => this.loading.set(false)),
          );
        }),
        takeUntilDestroyed(),
      )
      .subscribe();
    effect(() =>
      this.requests.next({ kind: this.kind(), search: this.search(), page: this.page() }),
    );
  }
  select(choice: MasterChoice): void {
    if (choice.isActive) {
      this.selected.set(choice);
      this.picked.emit(choice);
    }
  }
}

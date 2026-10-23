import { debouncedQuery } from '../../core/api/debounced-query';
import { computed, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  catchError,
  defer,
  EMPTY,
  finalize,
  map,
  Observable,
  Subject,
  switchMap,
  tap,
  throwError,
} from 'rxjs';
import { AuthService } from '../../core/auth/auth-service';
import { can } from '../../core/auth/permissions';
import { PartnersApi } from '../../core/api/partners-api';
import { ApiError } from '../../core/api/api-error';
import { QueryInvalidation } from '../../core/api/query-invalidation';
import { mapPartner, PartnerKind, partnerPayload, PartnerView } from './partner.mapper';
@Injectable()
export class PartnersFacade {
  private readonly api = inject(PartnersApi);
  private readonly auth = inject(AuthService);
  private readonly requests = new Subject<{
    kind: PartnerKind;
    search: string;
    pageIndex: number;
  }>();
  private activeQuery?: { kind: PartnerKind; search: string; pageIndex: number };
  readonly canManage = computed(() => can('admin', this.auth.user()?.roles ?? []));
  readonly rows = signal<PartnerView[]>([]);
  readonly totalCount = signal(0);
  readonly page = signal(1);
  readonly loading = signal(false);
  readonly pending = signal(false);
  readonly error = signal<ApiError | null>(null);
  constructor() {
    this.requests
      .pipe(
        debouncedQuery((query) => {
          this.loading.set(true);
          this.error.set(null);
          const params = { search: query.search, pageIndex: query.pageIndex, pageSize: 8 };
          const source: Observable<{ items: PartnerView[]; totalCount: number }> =
            query.kind === 'customers'
              ? this.api
                  .customers(params)
                  .pipe(map((page) => ({ ...page, items: page.items.map(mapPartner) })))
              : this.api
                  .operators(params)
                  .pipe(map((page) => ({ ...page, items: page.items.map(mapPartner) })));
          return source.pipe(
            tap((page) => {
              this.rows.set(page.items);
              this.totalCount.set(page.totalCount);
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
    inject(QueryInvalidation)
      .changes$.pipe(takeUntilDestroyed())
      .subscribe((keys) => {
        if (keys.includes('partners') && this.activeQuery) this.requests.next(this.activeQuery);
      });
  }
  setQuery(kind: PartnerKind, search = '', pageIndex = 1): void {
    this.page.set(pageIndex);
    this.activeQuery = { kind, search, pageIndex };
    this.requests.next(this.activeQuery);
  }
  save(kind: PartnerKind, id: string | null, value: Record<string, unknown>) {
    return this.command(() => {
      const payload = partnerPayload(kind, value);
      if (id) return this.api.update(kind, id, payload.name).pipe(map(() => ({ id })));
      return kind === 'customers'
        ? this.api.createCustomer(payload as { name: string; taxCode: string })
        : this.api.createOperator(payload as { name: string; code: string });
    });
  }
  setActive(kind: PartnerKind, id: string, active: boolean) {
    return this.command(() => this.api.setActive(kind, id, active));
  }
  addOwnerCode(id: string, code: string) {
    return this.command(() => this.api.addOwnerCode(id, code));
  }
  removeOwnerCode(id: string, code: string) {
    return this.command(() => this.api.removeOwnerCode(id, code));
  }
  private command<T>(run: () => Observable<T>) {
    return defer(() => {
      if (!this.canManage())
        return throwError(
          () => new ApiError(403, 'Auth.Forbidden', 'Bạn không có quyền thay đổi danh mục.'),
        );
      if (this.pending())
        return throwError(() => new ApiError(409, 'Request.Pending', 'Thao tác đang được xử lý.'));
      this.pending.set(true);
      this.error.set(null);
      return run().pipe(
        catchError((error) => {
          this.error.set(error);
          return throwError(() => error);
        }),
        finalize(() => this.pending.set(false)),
      );
    });
  }
}

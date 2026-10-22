import { debouncedQuery } from '../../core/api/debounced-query';
import { inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  BehaviorSubject,
  catchError,
  EMPTY,
  finalize,
  forkJoin,
  map,
  of,
  Subject,
  switchMap,
  tap,
} from 'rxjs';
import { ContainersApi } from '../../core/api/containers-api';
import { ContainerSummaryDto, RegisterContainerRequest } from '../../core/api/containers.dto';
import { ApiError } from '../../core/api/api-error';
import { QueryInvalidation } from '../../core/api/query-invalidation';
import { ContainerView, mapContainer, normalizeContainerQuery } from './container.mapper';
@Injectable()
export class ContainersFacade {
  private readonly api = inject(ContainersApi);
  private readonly changes = new BehaviorSubject(normalizeContainerQuery({}));
  private readonly selection = new Subject<string | null>();
  readonly query = signal(normalizeContainerQuery({}));
  readonly rows = signal<ContainerView[]>([]);
  readonly totalCount = signal(0);
  readonly summary = signal<ContainerSummaryDto | null>(null);
  readonly detail = signal<ContainerView | null>(null);
  readonly loading = signal(false);
  readonly detailLoading = signal(false);
  readonly error = signal<ApiError | null>(null);
  readonly detailError = signal<ApiError | null>(null);
  readonly pending = signal(false);
  constructor() {
    this.changes
      .pipe(
        debouncedQuery((query) => {
          this.loading.set(true);
          this.error.set(null);
          return forkJoin({ page: this.api.list(query), summary: this.api.summary(query) }).pipe(
            switchMap((result) => {
              const last = Math.max(1, result.page.totalPages);
              if (query.pageIndex <= last) return of(result);
              const corrected = { ...query, pageIndex: last };
              this.query.set(corrected);
              return this.api
                .list(corrected)
                .pipe(map((page) => ({ page, summary: result.summary })));
            }),
            tap((result) => {
              this.rows.set(result.page.items.map(mapContainer));
              this.totalCount.set(result.page.totalCount);
              this.summary.set(result.summary);
            }),
            catchError((error) => {
              this.rows.set([]);
              this.error.set(error);
              return EMPTY;
            }),
            finalize(() => this.loading.set(false)),
          );
        }),
        takeUntilDestroyed(),
      )
      .subscribe();
    this.selection
      .pipe(
        switchMap((number) => {
          this.detail.set(null);
          this.detailError.set(null);
          this.detailLoading.set(!!number);
          return number
            ? this.api.byNumber(number).pipe(
                tap((dto) => this.detail.set(mapContainer(dto))),
                catchError((error) => {
                  this.detailError.set(error);
                  return EMPTY;
                }),
                finalize(() => this.detailLoading.set(false)),
              )
            : EMPTY;
        }),
        takeUntilDestroyed(),
      )
      .subscribe();
    inject(QueryInvalidation)
      .changes$.pipe(takeUntilDestroyed())
      .subscribe((keys) => {
        if (keys.includes('containers')) this.reload();
      });
  }
  setQuery(raw: Record<string, unknown>): void {
    const query = normalizeContainerQuery(raw);
    if (JSON.stringify(query) === JSON.stringify(this.query())) return;
    this.query.set(query);
    this.changes.next(query);
  }
  select(number: string | null): void {
    this.selection.next(number);
  }
  reload(): void {
    // A mutation must refetch even when filter values have not changed.
    this.changes.next(this.query());
  }
  register(input: RegisterContainerRequest) {
    this.pending.set(true);
    return this.api.register(input).pipe(
      map(() => undefined),
      finalize(() => this.pending.set(false)),
    );
  }
}

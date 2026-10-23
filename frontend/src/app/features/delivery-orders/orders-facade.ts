import { debouncedQuery } from '../../core/api/debounced-query';
import { inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  catchError,
  defer,
  EMPTY,
  finalize,
  forkJoin,
  map,
  Observable,
  Subject,
  switchMap,
  tap,
  throwError,
} from 'rxjs';
import { OrdersApi } from '../../core/api/orders-api';
import { ContainersApi } from '../../core/api/containers-api';
import { ApiPage, ApiParams } from '../../core/api/api.types';
import { ApiError } from '../../core/api/api-error';
import {
  AssignContainersResult,
  CreateOrderRequest,
  DeliveryOrderDto,
  OrderBookingsDto,
} from '../../core/api/orders.dto';
import { ContainerDto } from '../../core/api/containers.dto';
import { QueryInvalidation } from '../../core/api/query-invalidation';
import { assignmentMessage } from './order.mapper';
@Injectable()
export class OrdersFacade {
  private readonly api = inject(OrdersApi);
  private readonly containers = inject(ContainersApi);
  private readonly invalidation = inject(QueryInvalidation);
  private readonly queries = new Subject<ApiParams>();
  private readonly selection = new Subject<{ id: string; page: number }>();
  private readonly candidateQueries = new Subject<ApiParams>();
  private hasQuery = false;
  private candidateQuery?: ApiParams;
  readonly query = signal<ApiParams>({
    pageIndex: 1,
    pageSize: 8,
    sort: 'createdAt',
    descending: true,
  });
  readonly rows = signal<DeliveryOrderDto[]>([]);
  readonly totalCount = signal(0);
  readonly counts = signal<Record<string, number>>({});
  readonly detail = signal<DeliveryOrderDto | null>(null);
  readonly bookings = signal<OrderBookingsDto | null>(null);
  readonly candidates = signal<ApiPage<ContainerDto> | null>(null);
  readonly selectedId = signal('');
  readonly bookingPage = signal(1);
  readonly selectedNumbers = signal<string[]>([]);
  readonly loading = signal(false);
  readonly detailLoading = signal(false);
  readonly candidatesLoading = signal(false);
  readonly pending = signal(false);
  readonly error = signal<ApiError | null>(null);
  readonly candidateError = signal<ApiError | null>(null);
  readonly message = signal('');
  constructor() {
    this.queries
      .pipe(
        debouncedQuery((query) => {
          this.loading.set(true);
          this.error.set(null);
          const statuses = ['Active', 'ExpiringSoon', 'Expired', 'FullyDelivered'];
          return forkJoin({
            page: this.api.list(query),
            counts: forkJoin(
              statuses.map((status) =>
                this.api.list({ ...query, status, pageIndex: 1, pageSize: 1 }),
              ),
            ),
          }).pipe(
            tap((result) => {
              this.rows.set(result.page.items);
              this.totalCount.set(result.page.totalCount);
              this.counts.set(
                Object.fromEntries(
                  statuses.map((status, i) => [status, result.counts[i].totalCount]),
                ),
              );
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
    this.selection
      .pipe(
        switchMap(({ id, page }) => {
          if (!id) {
            this.detailLoading.set(false);
            return EMPTY;
          }
          this.detailLoading.set(true);
          this.error.set(null);
          return this.api.byId(id).pipe(
            switchMap((order) =>
              this.api
                .bookings(order.orderNumber, page)
                .pipe(map((bookings) => ({ order, bookings }))),
            ),
            tap((result) => {
              this.detail.set(result.order);
              this.bookings.set(result.bookings);
            }),
            catchError((error) => {
              this.error.set(error);
              if (error.status === 404) {
                this.detail.set(null);
                this.bookings.set(null);
              }
              return EMPTY;
            }),
            finalize(() => this.detailLoading.set(false)),
          );
        }),
        takeUntilDestroyed(),
      )
      .subscribe();
    this.candidateQueries
      .pipe(
        debouncedQuery((query) => {
          this.candidatesLoading.set(true);
          this.candidateError.set(null);
          return this.containers.list(query).pipe(
            tap((page) => this.candidates.set(page)),
            catchError((error) => {
              this.candidateError.set(error);
              return EMPTY;
            }),
            finalize(() => this.candidatesLoading.set(false)),
          );
        }),
        takeUntilDestroyed(),
      )
      .subscribe();
    this.invalidation.changes$.pipe(takeUntilDestroyed()).subscribe((keys) => {
      if (keys.includes('orders')) {
        if (this.hasQuery) this.queries.next(this.query());
        if (this.selectedId())
          this.selection.next({ id: this.selectedId(), page: this.bookingPage() });
      }
      if (keys.includes('containers') && this.candidateQuery)
        this.candidateQueries.next(this.candidateQuery);
    });
  }
  setQuery(raw: ApiParams): void {
    const page = Number(raw['pageIndex'] ?? 1);
    const status = String(raw['status'] ?? '');
    const query = {
      ...raw,
      pageIndex: Number.isSafeInteger(page) && page > 0 ? page : 1,
      pageSize: 8,
      status: ['Active', 'ExpiringSoon', 'Expired', 'FullyDelivered'].includes(status)
        ? status
        : undefined,
      sort: ['createdAt', 'orderNumber', 'expiryDate'].includes(String(raw['sort']))
        ? raw['sort']
        : 'createdAt',
      descending: raw['descending'] ?? true,
    };
    this.query.set(query);
    this.hasQuery = true;
    this.queries.next(query);
  }
  select(id: string, page = 1): void {
    if (id !== this.selectedId()) {
      this.detail.set(null);
      this.bookings.set(null);
      this.selectedNumbers.set([]);
      this.message.set('');
      this.candidates.set(null);
      this.candidateQuery = undefined;
    }
    this.selectedId.set(id);
    this.bookingPage.set(page);
    this.selection.next({ id, page });
  }
  loadCandidates(itemId: string, search = '', pageIndex = 1): void {
    const order = this.detail(),
      item = order?.items.find((item) => item.id === itemId);
    if (!order || !item) return;
    this.candidateQuery = {
      lineOperator: order.lineOperator,
      size: item.size,
      type: item.type,
      displayStatus: 'InYard',
      search,
      pageIndex,
      pageSize: 8,
    };
    this.candidateQueries.next(this.candidateQuery);
  }
  assign(numbers: string[]): Observable<AssignContainersResult> {
    return this.command(() =>
      this.api.assign(this.detail()!.orderNumber, numbers).pipe(
        tap((result) => {
          this.message.set(assignmentMessage(result));
          this.selectedNumbers.set(result.failedContainerNumbers);
        }),
      ),
    );
  }
  release(id: string): Observable<void> {
    return this.command(() => this.api.release(id));
  }
  create(input: CreateOrderRequest) {
    return this.command(() => this.api.create(input));
  }
  private command<T>(run: () => Observable<T>): Observable<T> {
    return defer(() => {
      if (this.pending())
        return throwError(() => new ApiError(409, 'Request.Pending', 'Thao tác đang được xử lý.'));
      this.pending.set(true);
      this.error.set(null);
      return run().pipe(
        catchError((error) => {
          this.error.set(error);
          if (error.status === 409 || error.status === 422)
            this.invalidation.invalidate(['orders', 'containers', 'reports']);
          return throwError(() => error);
        }),
        finalize(() => this.pending.set(false)),
      );
    });
  }
}

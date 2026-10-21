import { computed, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  catchError,
  debounceTime,
  defer,
  EMPTY,
  finalize,
  Observable,
  Subject,
  switchMap,
  tap,
  throwError,
  map,
} from 'rxjs';
import { GateApi } from '../../core/api/gate-api';
import { ApiError } from '../../core/api/api-error';
import { ApiParams } from '../../core/api/api.types';
import {
  GateTransactionDto,
  GateVehicleInput,
  UpdateGateRequest,
  ValidateGateInput,
  ValidateGateResponseDto,
} from '../../core/api/gate.dto';
import { QueryInvalidation } from '../../core/api/query-invalidation';
@Injectable()
export class GateFacade {
  private readonly api = inject(GateApi);
  private readonly validations = new Subject<ValidateGateInput | null>();
  private readonly queries = new Subject<ApiParams>();
  readonly input = signal<ValidateGateInput | null>(null);
  readonly validation = signal<ValidateGateResponseDto | null>(null);
  readonly validating = signal(false);
  readonly pending = signal(false);
  readonly error = signal<ApiError | null>(null);
  readonly loading = signal(false);
  readonly selected = signal<GateTransactionDto | null>(null);
  readonly rows = signal<GateTransactionDto[]>([]);
  readonly totalCount = signal(0);
  readonly query = signal<ApiParams>({ pageIndex: 1, pageSize: 8 });
  readonly canSubmit = computed(
    () =>
      !!this.input() &&
      !this.validating() &&
      !!this.validation() &&
      !this.validation()!.checks.some((check) => check.state === 'Fail'),
  );
  private hasQuery = false;
  constructor() {
    this.validations
      .pipe(
        switchMap((input) => {
          this.validation.set(null);
          this.validating.set(!!input);
          return input
            ? this.api.validate(input).pipe(
                tap((value) => this.validation.set(value)),
                catchError((error) => {
                  this.error.set(error);
                  return EMPTY;
                }),
                finalize(() => this.validating.set(false)),
              )
            : EMPTY;
        }),
        takeUntilDestroyed(),
      )
      .subscribe();
    this.queries
      .pipe(
        debounceTime(300),
        switchMap((query) => {
          this.loading.set(true);
          return this.api.transactions(query).pipe(
            tap((page) => {
              this.rows.set(page.items);
              this.totalCount.set(page.totalCount);
              const selected = this.selected();
              if (selected) {
                const fresh = page.items.find((row) => row.id === selected.id);
                if (fresh) this.selected.set(fresh);
              }
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
        if (keys.includes('gate') && this.hasQuery) this.queries.next(this.query());
      });
  }
  setInput(input: ValidateGateInput | null): void {
    this.input.set(input);
    this.error.set(null);
    this.validations.next(input);
  }
  setQuery(query: ApiParams): void {
    this.query.set({ ...query, pageSize: 8 });
    this.hasQuery = true;
    this.queries.next(this.query());
  }
  submit(vehicle: GateVehicleInput): Observable<void> {
    return this.command(() => {
      const input = this.input();
      if (!input || !this.canSubmit())
        return throwError(
          () =>
            new ApiError(400, 'Gate.ValidationPending', 'Vui lòng chờ kiểm tra điều kiện cổng.'),
        );
      const payload = { ...input, ...vehicle };
      return input.type === 'GateIn'
        ? this.api.gateIn(payload).pipe(map(() => undefined))
        : this.api.gateOut(payload);
    });
  }
  update(id: string, input: UpdateGateRequest) {
    return this.command(() => this.api.update(id, input));
  }
  cancel(id: string, reason: string) {
    return this.command(() => this.api.cancel(id, reason));
  }
  private command<T>(run: () => Observable<T>) {
    return defer(() => {
      if (this.pending())
        return throwError(() => new ApiError(409, 'Request.Pending', 'Thao tác đang được xử lý.'));
      this.pending.set(true);
      this.error.set(null);
      return run().pipe(
        catchError((error) => {
          this.error.set(error);
          if (this.hasQuery && (error.status === 409 || error.status === 422))
            this.queries.next(this.query());
          return throwError(() => error);
        }),
        finalize(() => this.pending.set(false)),
      );
    });
  }
}

import { inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  catchError,
  defer,
  EMPTY,
  finalize,
  forkJoin,
  Observable,
  of,
  Subject,
  switchMap,
  tap,
  throwError,
} from 'rxjs';
import { YardApi } from '../../core/api/yard-api';
import { ApiError } from '../../core/api/api-error';
import { QueryInvalidation } from '../../core/api/query-invalidation';
import {
  PlaceRequest,
  RelocateRequest,
  VirtualBlockContainerDto,
  YardBlockDto,
  YardBlockSizeRequest,
  YardLayoutDto,
} from '../../core/api/yard.dto';
@Injectable()
export class YardFacade {
  private readonly api = inject(YardApi);
  private readonly requests = new Subject<{ block: string; tier: number }>();
  readonly blocks = signal<YardBlockDto[]>([]);
  readonly layout = signal<YardLayoutDto | null>(null);
  readonly virtual = signal<VirtualBlockContainerDto[]>([]);
  readonly blockId = signal('');
  readonly tier = signal(1);
  readonly selectedContainer = signal('');
  readonly loading = signal(false);
  readonly pending = signal(false);
  readonly error = signal<ApiError | null>(null);
  constructor() {
    this.requests
      .pipe(
        switchMap((query) => {
          this.loading.set(true);
          const block = this.blocks().find(
            (block) => block.id === query.block || block.code === query.block,
          );
          return forkJoin({
            layout: this.api.layout(query.block, query.tier),
            virtual: block?.isVirtual ? this.api.virtualContainers(block.code) : of([]),
          }).pipe(
            tap((result) => {
              this.layout.set(result.layout);
              this.virtual.set(result.virtual);
            }),
            catchError((error) => {
              this.error.set(error);
              this.layout.set(null);
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
        if (keys.includes('yard')) this.reload();
      });
  }
  loadBlocks(): void {
    this.api
      .blocks()
      .subscribe({
        next: (blocks) => this.blocks.set(blocks),
        error: (error) => this.error.set(error),
      });
  }
  select(block: string, tier: number): void {
    this.blockId.set(block);
    this.tier.set(Number.isInteger(tier) && tier > 0 ? tier : 1);
    this.error.set(null);
    this.requests.next({ block, tier: this.tier() });
  }
  reload(): void {
    this.loadBlocks();
    if (this.blockId()) this.requests.next({ block: this.blockId(), tier: this.tier() });
  }
  resize(mode: 'expand' | 'shrink', input: YardBlockSizeRequest) {
    return this.command(() =>
      mode === 'expand'
        ? this.api.expand(this.blockId(), input)
        : this.api.shrink(this.blockId(), input),
    );
  }
  relocate(input: RelocateRequest) {
    return this.command(() => this.api.relocate(input));
  }
  place(input: PlaceRequest) {
    return this.command(() => this.api.place(input));
  }
  fromVirtual(
    code: string,
    id: string,
    target: { targetBlockCode: string; bay: number; row: number; tier: number },
  ) {
    return this.command(() => this.api.placeFromVirtual(code, id, target));
  }
  remove(input: { blockId: string; bay: number; row: number; tier: number }) {
    return this.command(() => this.api.remove(input));
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
          if (error.status === 409 || error.status === 422) this.reload();
          return throwError(() => error);
        }),
        finalize(() => this.pending.set(false)),
      );
    });
  }
}

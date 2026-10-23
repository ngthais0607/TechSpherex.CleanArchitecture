import { DestroyRef, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, debounceTime, EMPTY, finalize, map, Subject, switchMap, tap } from 'rxjs';
import { ReportsApi } from '../../core/api/reports-api';
import { ApiParams } from '../../core/api/api.types';
import { InventoryReportDto } from '../../core/api/reports.dto';
import { QueryInvalidation } from '../../core/api/query-invalidation';
import { collectPages } from '../../core/export/paged-export';
import { downloadWorkbook } from '../../core/export/workbook-export';
@Injectable()
export class InventoryFacade {
  private readonly api = inject(ReportsApi);
  private readonly queries = new Subject<void>();
  private abort?: AbortController;
  readonly query = signal<ApiParams>({ pageIndex: 1, pageSize: 8 });
  readonly data = signal<InventoryReportDto | null>(null);
  readonly loading = signal(false);
  readonly error = signal('');
  readonly exporting = signal(false);
  readonly progress = signal('');
  constructor() {
    this.queries
      .pipe(
        debounceTime(300),
        switchMap(() => {
          this.loading.set(true);
          this.error.set('');
          return this.api.inventory(this.query()).pipe(
            tap((value) => this.data.set(value)),
            catchError((error) => {
              this.error.set(error.detail ?? error.message);
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
        if (keys.includes('reports')) this.queries.next();
      });
    inject(DestroyRef).onDestroy(() => this.abort?.abort());
  }
  load(query: ApiParams): void {
    this.query.set({ ...query, pageSize: 8 });
    this.queries.next();
  }
  async export(): Promise<void> {
    if (this.exporting()) return;
    this.abort = new AbortController();
    const abort = this.abort,
      query = { ...this.query() };
    let total: number | undefined;
    this.exporting.set(true);
    this.error.set('');
    try {
      const rows = await collectPages(
        (pageIndex, pageSize) =>
          this.api.inventory({ ...query, pageIndex, pageSize }).pipe(
            map((result) => {
              if (total !== undefined && total !== result.totalContainers)
                throw new Error('Dữ liệu đã thay đổi trong lúc xuất. Vui lòng thử lại.');
              total = result.totalContainers;
              return result.groups;
            }),
          ),
        abort.signal,
        (done, total) => this.progress.set(done + '/' + total),
      );
      await downloadWorkbook(
        'Tồn bãi',
        ['Hãng', 'Cỡ', 'Loại', 'Tình trạng', 'Block', 'Số container'],
        rows.map((row) => [
          row.lineOperator,
          row.size,
          row.type,
          row.condition,
          row.blockCode ?? 'Chưa xếp vị trí',
          row.containerCount,
        ]),
        JSON.stringify(query) + '; không bảo đảm snapshot đồng thời',
        abort.signal,
      );
    } catch (error) {
      if (!abort.signal.aborted)
        this.error.set(error instanceof Error ? error.message : 'Không thể xuất báo cáo.');
    } finally {
      this.exporting.set(false);
    }
  }
  cancelExport(): void {
    this.abort?.abort();
  }
}

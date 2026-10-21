import { firstValueFrom, fromEvent, Observable, takeUntil } from 'rxjs';
import { ApiPage } from '../api/api.types';
export async function collectPages<T>(
  load: (pageIndex: number, pageSize: number) => Observable<ApiPage<T>>,
  signal: AbortSignal,
  progress: (done: number, total: number) => void,
): Promise<T[]> {
  const rows: T[] = [];
  let total: number | undefined;
  let pages: number | undefined;
  for (let index = 1; ; index++) {
    if (signal.aborted) throw new DOMException('Đã hủy xuất báo cáo.', 'AbortError');
    let page: ApiPage<T>;
    try {
      page = await firstValueFrom(load(index, 100).pipe(takeUntil(fromEvent(signal, 'abort'))));
    } catch (error) {
      if (signal.aborted) throw new DOMException('Đã hủy xuất báo cáo.', 'AbortError');
      throw error;
    }
    if (
      page.pageIndex !== index ||
      page.pageSize !== 100 ||
      !Number.isSafeInteger(page.totalCount) ||
      page.totalCount < 0 ||
      page.totalPages !== Math.ceil(page.totalCount / 100)
    )
      throw new Error('Dữ liệu trang không hợp lệ. Vui lòng thử xuất lại.');
    if (total !== undefined && (page.totalCount !== total || page.totalPages !== pages))
      throw new Error('Dữ liệu đã thay đổi trong lúc xuất. Vui lòng thử lại.');
    total = page.totalCount;
    pages = page.totalPages;
    if (page.items.length !== Math.min(100, Math.max(0, total - (index - 1) * 100)))
      throw new Error('Thiếu dữ liệu trang. Vui lòng thử xuất lại.');
    rows.push(...page.items);
    progress(rows.length, total);
    if (index >= pages) break;
  }
  if (signal.aborted) throw new DOMException('Đã hủy xuất báo cáo.', 'AbortError');
  return rows;
}

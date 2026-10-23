import { HttpErrorResponse } from '@angular/common/http';

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    public readonly code: string,
    public readonly detail: string,
    public readonly ruleCode?: string,
    public readonly errors?: Record<string, string[]>,
    public readonly traceId?: string,
    public readonly retryAfterSeconds?: number,
  ) {
    super(detail);
    this.name = 'ApiError';
  }
}
export function normalizeApiError(error: HttpErrorResponse): ApiError {
  const body: Record<string, unknown> =
    error.error && typeof error.error === 'object' ? error.error : {};
  const text = (key: string) => (typeof body[key] === 'string' ? (body[key] as string) : undefined);
  const rawErrors = body['errors'];
  const errors =
    rawErrors && typeof rawErrors === 'object' && !Array.isArray(rawErrors)
      ? (Object.fromEntries(
          Object.entries(rawErrors).filter(
            ([, values]) =>
              Array.isArray(values) && values.every((value) => typeof value === 'string'),
          ),
        ) as Record<string, string[]>)
      : undefined;
  const retry = error.headers.get('Retry-After');
  const seconds =
    retry === null
      ? undefined
      : /^\d+$/.test(retry)
        ? Number(retry)
        : Math.max(0, Math.ceil((Date.parse(retry) - Date.now()) / 1000));
  return new ApiError(
    error.status,
    text('code') ?? (error.status === 0 ? 'Network.Unavailable' : 'Request.Failed'),
    text('detail') ??
      (error.status === 0
        ? 'Không thể kết nối máy chủ. Vui lòng thử lại.'
        : error.status >= 500
          ? 'Máy chủ tạm thời gặp lỗi. Vui lòng thử lại.'
          : 'Yêu cầu không thành công.'),
    text('ruleCode'),
    errors,
    text('traceId'),
    seconds !== undefined && Number.isFinite(seconds) ? seconds : undefined,
  );
}

import { HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { describe, expect, it } from 'vitest';
import { normalizeApiError } from './api-error';
import { isApiUrl } from './api-config';

describe('API transport contract', () => {
  it('normalizesProblemAndNonJsonNetworkError', () => {
    const problem = normalizeApiError(new HttpErrorResponse({ status: 429,
      headers: new HttpHeaders({ 'Retry-After': '12' }), error: {
        code: 'RateLimit.TooManyRequests', detail: 'Chờ', traceId: 'trace-1',
        ruleCode: 'G1', errors: { number: ['Sai định dạng'] },
      } }));
    expect(problem.traceId).toBe('trace-1');
    expect(problem.status).toBe(429);
    expect(problem.errors?.['number']).toEqual(['Sai định dạng']);
    expect(problem.retryAfterSeconds).toBe(12);
    expect(problem.ruleCode).toBe('G1');
    expect(normalizeApiError(new HttpErrorResponse({ status: 0 })).detail).toContain('kết nối');
    const html = normalizeApiError(new HttpErrorResponse({ status: 502, error: '<html>proxy error</html>' }));
    expect(html.status).toBe(502);
    expect(html.detail).not.toContain('<html>');
  });
  it('attaches credentials only within the same-origin API boundary', () => {
    expect(isApiUrl('/api/containers')).toBe(true);
    expect(isApiUrl(new URL('/api/auth/me', location.origin).href)).toBe(true);
    expect(isApiUrl('/api-evil')).toBe(false);
    expect(isApiUrl('https://evil.example/api/containers')).toBe(false);
    expect(isApiUrl('//evil.example/api')).toBe(false);
  });
});

import { describe, expect, it, vi } from 'vitest';
import { TokenStorage } from './token-storage';
import { safeReturnUrl } from './safe-return-url';

describe('session boundaries', () => {
  it('brokenOrDeniedSessionStorageDoesNotCrash', () => {
    sessionStorage.setItem('snp-api-session', '{broken');
    const broken = new TokenStorage();
    expect(broken.read()).toBeNull();
    expect(broken.warning()).toContain('hỏng');
    const read = vi.spyOn(Storage.prototype, 'getItem').mockImplementation(() => {
      throw new Error('denied');
    });
    const write = vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new Error('denied');
    });
    const denied = new TokenStorage();
    denied.write({
      userName: 'gate',
      accessToken: 'a',
      refreshToken: 'r',
      accessTokenExpiresAt: '2099-01-01T00:00:00Z',
    });
    expect(denied.read()?.userName).toBe('gate');
    expect(denied.persistent()).toBe(false);
    expect(denied.warning()).toContain('phiên hiện tại');
    read.mockRestore();
    write.mockRestore();
    sessionStorage.clear();
  });
  it('unsafeReturnUrlIsRejected', () => {
    for (const url of ['//evil.example', 'https://evil.example', '/\\evil', '/login'])
      expect(safeReturnUrl(url)).toBe('/dashboard');
    expect(safeReturnUrl('/containers?search=MSC')).toBe('/containers?search=MSC');
  });
});

import { beforeEach, describe, expect, it } from 'vitest';
import { DemoSession, safeReturnUrl } from './demo-session';

describe('demo session', () => {
  beforeEach(() => sessionStorage.clear());
  it('rejects wrong credentials without creating a session', () => {
    const session = new DemoSession();
    expect(session.login('demo', 'wrong').ok).toBe(false);
    expect(new DemoSession().currentUser()).toBeNull();
  });
  it('persists login for reload and clears it on logout', () => {
    const session = new DemoSession();
    expect(session.login('demo', 'Demo@123').ok).toBe(true);
    expect(new DemoSession().currentUser()?.displayName).toBe('Nguyễn Minh');
    session.logout();
    expect(new DemoSession().currentUser()).toBeNull();
  });
  it('accepts only known internal destinations', () => {
    expect(safeReturnUrl('https://example.com')).toBe('/dashboard');
    expect(safeReturnUrl('//example.com')).toBe('/dashboard');
    expect(safeReturnUrl('/inventory?type=Dry')).toBe('/inventory?type=Dry');
  });
});

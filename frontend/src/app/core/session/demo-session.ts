import { Injectable, signal } from '@angular/core';
import { fail, ok } from '../../demo/rules/container-rules';
export function safeReturnUrl(raw: string | null): string {
  return raw &&
    /^\/(dashboard|containers|delivery-orders|gate|yard|partners|reports|inventory)([/?#]|$)/.test(
      raw,
    )
    ? raw
    : '/dashboard';
}
@Injectable({ providedIn: 'root' })
export class DemoSession {
  currentUser = signal<{ displayName: string; role: string } | null>(null);
  constructor() {
    try {
      if (sessionStorage.getItem('snp-demo-session') === 'active')
        this.currentUser.set({ displayName: 'Nguyễn Minh', role: 'Điều phối viên' });
    } catch {}
  }
  login(username: string, password: string) {
    if (username.trim() !== 'demo' || password !== 'Demo@123')
      return fail('DEMO_LOGIN', 'Tên đăng nhập hoặc mật khẩu không đúng.');
    this.currentUser.set({ displayName: 'Nguyễn Minh', role: 'Điều phối viên' });
    try {
      sessionStorage.setItem('snp-demo-session', 'active');
    } catch {}
    return ok(undefined);
  }
  logout() {
    this.currentUser.set(null);
    try {
      sessionStorage.removeItem('snp-demo-session');
    } catch {}
  }
}

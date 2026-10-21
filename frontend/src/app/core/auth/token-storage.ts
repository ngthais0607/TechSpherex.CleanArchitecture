import { Injectable, signal } from '@angular/core';
import { StoredSession } from './auth.dto';

@Injectable({ providedIn: 'root' })
export class TokenStorage {
  private readonly key = 'snp-api-session';
  private session: StoredSession | null = null;
  readonly warning = signal<string | null>(null);
  readonly persistent = signal(true);
  constructor() {
    try {
      const raw = sessionStorage.getItem(this.key);
      if (raw) {
        const value = JSON.parse(raw);
        if (!value || !['userName', 'accessToken', 'refreshToken', 'accessTokenExpiresAt']
          .every(key => typeof value[key] === 'string' && value[key].length > 0) ||
          !Number.isFinite(Date.parse(value.accessTokenExpiresAt))) throw new Error('invalid session');
        this.session = value;
      }
    } catch {
      this.warning.set('Phiên đăng nhập bị hỏng hoặc không đọc được. Vui lòng đăng nhập lại.');
      try { sessionStorage.removeItem(this.key); } catch { this.denied(); }
    }
  }
  read(): StoredSession | null { return this.session; }
  write(value: StoredSession): void {
    this.session = { userName: value.userName, accessToken: value.accessToken,
      refreshToken: value.refreshToken, accessTokenExpiresAt: value.accessTokenExpiresAt };
    try { sessionStorage.setItem(this.key, JSON.stringify(this.session)); } catch { this.denied(); }
  }
  clear(): void {
    this.session = null;
    try { sessionStorage.removeItem(this.key); } catch { this.denied(); }
  }
  private denied(): void {
    this.persistent.set(false);
    this.warning.set('Trình duyệt không cho lưu phiên; đăng nhập chỉ có hiệu lực trong phiên hiện tại.');
  }
}

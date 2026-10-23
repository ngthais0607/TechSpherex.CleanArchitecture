import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class Notifications {
  readonly toast = signal<{ message: string; kind: 'success' | 'error' } | null>(null);
  private timer?: ReturnType<typeof setTimeout>;
  success(message: string): void {
    this.show(message, 'success');
  }
  error(message: string): void {
    this.show(message, 'error');
  }
  private show(message: string, kind: 'success' | 'error'): void {
    clearTimeout(this.timer);
    this.toast.set({ message, kind });
    this.timer = setTimeout(() => this.toast.set(null), 6000);
  }
}

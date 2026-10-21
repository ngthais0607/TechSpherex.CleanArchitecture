import { effect, inject, Injectable } from '@angular/core';
import { Observable, shareReplay, Subject, takeUntil, filter } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AuthService } from '../auth/auth-service';
import { LookupsApi } from './lookups-api';
import { LookupsDto } from './lookups.dto';
import { QueryInvalidation } from './query-invalidation';
@Injectable({ providedIn: 'root' })
export class ReferenceData {
  private readonly auth = inject(AuthService);
  private readonly api = inject(LookupsApi);
  private readonly canceled = new Subject<void>();
  private cache?: { key: string; expires: number; stream: Observable<LookupsDto> };
  constructor() {
    let identity = this.key();
    effect(() => {
      const key = this.key();
      if (key !== identity) {
        identity = key;
        this.clear();
      }
    });
    inject(QueryInvalidation)
      .changes$.pipe(takeUntilDestroyed())
      .subscribe((keys) => {
        if (keys.includes('lookups')) this.clear();
      });
  }
  lookups(): Observable<LookupsDto> {
    const key = this.key();
    if (this.cache?.key === key && this.cache.expires > Date.now()) return this.cache.stream;
    const stream = this.api.get().pipe(
      takeUntil(this.canceled),
      filter(() => this.key() === key),
      shareReplay({ bufferSize: 1, refCount: false }),
    );
    this.cache = { key, expires: Date.now() + 3_600_000, stream };
    return stream;
  }
  clear(): void {
    this.canceled.next();
    this.cache = undefined;
  }
  private key(): string {
    const user = this.auth.user();
    return `${user?.userId ?? ''}:${user?.tenantId ?? ''}`;
  }
}

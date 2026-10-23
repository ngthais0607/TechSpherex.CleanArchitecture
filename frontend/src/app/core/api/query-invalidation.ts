import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';

export type QueryKey =
  'containers' | 'orders' | 'gate' | 'yard' | 'partners' | 'reports' | 'lookups';
@Injectable({ providedIn: 'root' })
export class QueryInvalidation {
  private readonly changes = new Subject<readonly QueryKey[]>();
  readonly changes$ = this.changes.asObservable();
  invalidate(keys: readonly QueryKey[]): void {
    if (keys.length) this.changes.next([...new Set(keys)]);
  }
}

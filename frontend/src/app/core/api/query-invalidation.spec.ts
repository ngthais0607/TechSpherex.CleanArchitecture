import { describe, expect, it } from 'vitest';
import { QueryInvalidation, QueryKey } from './query-invalidation';

describe('query invalidation', () => {
  it('emits only requested keys without business data', () => {
    const bus = new QueryInvalidation();
    const received: (readonly QueryKey[])[] = [];
    const sub = bus.changes$.subscribe((keys) => received.push(keys));
    bus.invalidate(['containers', 'orders', 'containers']);
    bus.invalidate([]);
    expect(received).toEqual([['containers', 'orders']]);
    sub.unsubscribe();
  });
});

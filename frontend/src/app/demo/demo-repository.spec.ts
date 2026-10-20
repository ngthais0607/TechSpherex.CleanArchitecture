import { beforeEach, afterEach, describe, it, expect, vi } from 'vitest';
import { DemoRepository } from './demo-repository';
import { STORAGE_KEY } from './storage';
import { fail, ok } from './rules/container-rules';
describe('persistent demo transactions', () => {
  beforeEach(() => {
    localStorage.clear();
    vi.useFakeTimers();
  });
  afterEach(() => {
    vi.restoreAllMocks();
    vi.clearAllTimers();
    vi.useRealTimers();
  });
  it('does not publish or persist a failed transaction', () => {
    const store = new DemoRepository(),
      before = JSON.stringify(store.state());
    const r = store.run((s) => {
      s.containers = [];
      return fail('TEST', 'Rejected');
    });
    expect(r.ok).toBe(false);
    expect(JSON.stringify(store.state())).toBe(before);
    expect(localStorage.getItem(STORAGE_KEY)).toBeNull();
  });
  it('persists successful writes and restores them on reload', () => {
    const store = new DemoRepository();
    store.run((s) => {
      s.containers[0].condition = 'Móp';
      return ok(undefined);
    });
    expect(new DemoRepository().state().containers[0].condition).toBe('Móp');
  });
  it('keeps current work in memory and warns on storage quota failures', () => {
    const store = new DemoRepository();
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new DOMException('quota', 'QuotaExceededError');
    });
    expect(
      store.run((s) => {
        s.containers[0].condition = 'Móp';
        return ok(undefined);
      }).ok,
    ).toBe(true);
    expect(store.state().containers[0].condition).toBe('Móp');
    expect(store.warning()).toContain('bộ nhớ');
  });
  it('offers recovery without replacing an unreadable snapshot automatically', () => {
    localStorage.setItem(STORAGE_KEY, '{bad-json');
    const store = new DemoRepository();
    expect(store.warning()).not.toBe('');
    expect(localStorage.getItem(STORAGE_KEY)).toBe('{bad-json');
  });
  it('does not overwrite a corrupt snapshot through a later write before recovery', () => {
    localStorage.setItem(STORAGE_KEY, '{bad-json');
    const store = new DemoRepository();
    expect(
      store.run((s) => {
        s.containers[0].condition = 'Móp';
        return ok(undefined);
      }).ok,
    ).toBe(false);
    expect(localStorage.getItem(STORAGE_KEY)).toBe('{bad-json');
    store.reset();
    expect(store.run(() => ok(undefined)).ok).toBe(true);
  });
});

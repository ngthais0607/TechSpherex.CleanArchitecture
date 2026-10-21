import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of, Subject } from 'rxjs';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { App } from '../../app';
import { AuthService } from './auth-service';
describe('logout with unsaved gate form', () => {
  afterEach(() => TestBed.resetTestingModule());
  it('canceledNavigationKeepsTheSession', async () => {
    const logout = vi.fn(() => of(undefined));
    TestBed.configureTestingModule({ providers: [
      { provide: Router, useValue: { events: new Subject(), navigateByUrl: vi.fn(async () => false) } },
      { provide: AuthService, useValue: { logout } },
    ] });
    const app = TestBed.runInInjectionContext(() => new App());
    await app.logout();
    expect(logout).not.toHaveBeenCalled();
  });
});

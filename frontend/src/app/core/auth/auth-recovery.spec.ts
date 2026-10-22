import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { CanDeactivateFn, Router, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { App } from '../../app';
import { routes } from '../../app.routes';
import { LoginPage } from '../../features/login/login-page';
import { errorInterceptor } from '../api/error.interceptor';
import { AuthService } from './auth-service';
import { authInterceptor } from './auth.interceptor';
import { authGuard } from './auth.guard';

@Component({ template: 'protected page' })
class ProtectedPage {}
const tokens = {
  userName: 'gate',
  accessToken: 'access',
  refreshToken: 'refresh',
  accessTokenExpiresAt: '2099-01-01T00:00:00Z',
};
const user = {
  userId: '1',
  userName: 'gate',
  roles: ['GateClerk'],
  tenantId: 'DEPOT-A',
  depotName: 'Depot',
};
describe('session recovery on an active route', () => {
  let http: HttpTestingController;
  let auth: AuthService;
  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideRouter([
          { path: 'containers', component: ProtectedPage, canActivate: [authGuard] },
          { path: 'login', component: LoginPage },
        ]),
        provideHttpClient(withInterceptors([errorInterceptor, authInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpTestingController);
    auth = TestBed.inject(AuthService);
    auth.storage.write(tokens);
  });
  afterEach(() => {
    http.verify();
    TestBed.resetTestingModule();
    vi.restoreAllMocks();
  });
  it('refresh rejection opens login with the protected return URL', async () => {
    auth.user.set(user);
    auth.status.set('authenticated');
    const harness = await RouterTestingHarness.create('/containers?search=MSC');
    TestBed.runInInjectionContext(() => new App());
    auth.storage.write({ ...tokens, accessTokenExpiresAt: '2020-01-01T00:00:00Z' });
    TestBed.inject(HttpClient)
      .get('/api/resource')
      .subscribe({ error: () => {} });
    http.expectOne('/api/auth/refresh').flush({}, { status: 401, statusText: 'Expired' });
    await vi.waitFor(() =>
      expect(TestBed.inject(Router).url).toBe('/login?returnUrl=%2Fcontainers%3Fsearch%3DMSC'),
    );
    expect(auth.storage.read()).toBeNull();
    expect(harness.routeNativeElement?.textContent).toContain('Đăng nhập');
  });
  it('temporary me failure offers retry and restores the retained session without credentials', async () => {
    const harness = await RouterTestingHarness.create();
    const navigation = harness.navigateByUrl('/containers?search=MSC');
    const me = await vi.waitFor(() => http.expectOne('/api/auth/me'));
    me.flush({}, { status: 503, statusText: 'Unavailable' });
    await navigation;
    harness.detectChanges();
    const button = harness.routeNativeElement?.querySelector<HTMLButtonElement>(
      'button[aria-label="Thử khôi phục phiên"]',
    );
    expect(button).toBeTruthy();
    button!.click();
    const retry = await vi.waitFor(() => http.expectOne('/api/auth/me'));
    retry.flush(user);
    await vi.waitFor(() => expect(TestBed.inject(Router).url).toBe('/containers?search=MSC'));
    expect(auth.storage.read()?.refreshToken).toBe('refresh');
    http.expectNone('/api/auth/login');
  });
  it('an expired session can leave a dirty gate page without an impossible cancel loop', () => {
    auth.status.set('anonymous');
    const confirm = vi.spyOn(window, 'confirm').mockReturnValue(false);
    const guard = routes.find((route) => route.path === 'gate')!
      .canDeactivate![0] as CanDeactivateFn<{ hasUnsavedChanges: () => boolean }>;
    const result = TestBed.runInInjectionContext(() =>
      guard({ hasUnsavedChanges: () => true }, {} as never, {} as never, {} as never),
    );
    expect(result).toBe(true);
    expect(confirm).not.toHaveBeenCalled();
  });
});

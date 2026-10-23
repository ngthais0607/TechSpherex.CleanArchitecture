import { provideHttpClient, withInterceptors, HttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { authInterceptor } from './auth.interceptor';
import { AuthService } from './auth-service';
import { TokenStorage } from './token-storage';
import { errorInterceptor } from '../api/error.interceptor';

const tokens = {
  userName: 'gate',
  accessToken: 'access-new',
  refreshToken: 'refresh-new',
  role: 'GateClerk',
  expiresAt: '2099-01-01T00:00:00Z',
  accessTokenExpiresAt: '2099-01-01T00:00:00Z',
};
describe('auth transport', () => {
  let http: HttpTestingController;
  let client: HttpClient;
  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([errorInterceptor, authInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpTestingController);
    client = TestBed.inject(HttpClient);
    TestBed.inject(TokenStorage).write({
      ...tokens,
      accessToken: 'access-old',
      refreshToken: 'refresh-old',
      accessTokenExpiresAt: '2020-01-01T00:00:00Z',
    });
  });
  afterEach(() => {
    http.verify();
    TestBed.resetTestingModule();
  });
  it('threeExpiredRequestsUseOneRotatingRefresh', () => {
    let completed = 0;
    for (let n = 0; n < 3; n++) client.get('/api/resource/' + n).subscribe(() => completed++);
    const refresh = http.expectOne('/api/auth/refresh');
    expect(refresh.request.body).toEqual({ userName: 'gate', refreshToken: 'refresh-old' });
    refresh.flush(tokens);
    for (let n = 0; n < 3; n++) {
      const request = http.expectOne('/api/resource/' + n);
      expect(request.request.headers.get('Authorization')).toBe('Bearer access-new');
      request.flush({});
    }
    expect(completed).toBe(3);
    expect(TestBed.inject(TokenStorage).read()?.refreshToken).toBe('refresh-new');
  });
  it('resource401RetriesOnlyOnceAndExternalUrlsHaveNoBearer', () => {
    TestBed.inject(TokenStorage).write(tokens);
    let status = 0;
    client.get('/api/resource').subscribe({ error: (error) => (status = error.status) });
    http.expectOne('/api/resource').flush({}, { status: 401, statusText: 'Unauthorized' });
    http.expectOne('/api/auth/refresh').flush(tokens);
    http.expectOne('/api/resource').flush({}, { status: 401, statusText: 'Unauthorized' });
    expect(status).toBe(401);
    http.expectNone('/api/auth/refresh');
    client.get('https://example.test/api/resource').subscribe();
    const external = http.expectOne('https://example.test/api/resource');
    expect(external.request.headers.has('Authorization')).toBe(false);
    external.flush({});
  });
  it('login401DoesNotRefreshRecursively', () => {
    TestBed.inject(AuthService)
      .login({ userName: 'gate', password: 'wrong' })
      .subscribe({ error: () => {} });
    http.expectOne('/api/auth/login').flush({}, { status: 401, statusText: 'Unauthorized' });
    http.expectNone('/api/auth/refresh');
  });
  it('restoreWaitsForMeAndSharesTheRequest', () => {
    TestBed.inject(TokenStorage).write(tokens);
    const auth = TestBed.inject(AuthService);
    let completed = 0;
    auth.restore().subscribe(() => completed++);
    auth.restore().subscribe(() => completed++);
    expect(auth.user()).toBeNull();
    expect(auth.status()).toBe('initializing');
    const me = http.expectOne('/api/auth/me');
    expect(me.request.headers.get('Authorization')).toBe('Bearer access-new');
    me.flush({
      userId: '1',
      userName: 'gate',
      roles: ['GateClerk'],
      tenantId: 'DEPOT-A',
      depotName: 'Depot A',
    });
    expect(completed).toBe(2);
    expect(auth.user()?.tenantId).toBe('DEPOT-A');
    expect(auth.status()).toBe('authenticated');
  });
  it('restoreServerFailureKeepsTokensForRetry', () => {
    TestBed.inject(TokenStorage).write(tokens);
    const auth = TestBed.inject(AuthService);
    auth.restore().subscribe();
    http.expectOne('/api/auth/me').flush({}, { status: 503, statusText: 'Unavailable' });
    expect(auth.status()).toBe('error');
    expect(auth.storage.read()?.refreshToken).toBe('refresh-new');
    auth.restore().subscribe();
    http.expectOne('/api/auth/me').flush({
      userId: '1',
      userName: 'gate',
      roles: ['GateClerk'],
      tenantId: 'DEPOT-A',
      depotName: 'Depot A',
    });
    expect(auth.status()).toBe('authenticated');
  });
  it('logoutDuringRefreshCannotRestoreUserOrReplayCommands', () => {
    const auth = TestBed.inject(AuthService);
    let commandSent = false;
    client.post('/api/command', {}).subscribe(() => (commandSent = true));
    const refresh = http.expectOne('/api/auth/refresh');
    auth.logout().subscribe();
    http.expectOne('/api/auth/logout').flush({}, { status: 500, statusText: 'Error' });
    expect(refresh.cancelled).toBe(true);
    expect(auth.user()).toBeNull();
    expect(auth.status()).toBe('anonymous');
    expect(TestBed.inject(TokenStorage).read()).toBeNull();
    http.expectNone('/api/command');
    expect(commandSent).toBe(false);
  });
  it('identity change cancels an issued command instead of retrying it as the next user', () => {
    const auth = TestBed.inject(AuthService);
    auth.storage.write(tokens);
    client.post('/api/command-from-A', { container: 'A' }).subscribe({ error: () => {} });
    const old = http.expectOne('/api/command-from-A');
    auth.logout().subscribe();
    http.expectOne('/api/auth/logout').flush(null);
    auth.storage.write({ ...tokens, userName: 'other-user', accessToken: 'other-access' });
    expect(old.cancelled).toBe(true);
    http.expectNone('/api/command-from-A');
  });
  it.each([401, 403, 500, 0])('refresh %s clears only rejected credentials', (status) => {
    const auth = TestBed.inject(AuthService);
    auth.refreshOnce().subscribe({ error: () => {} });
    const request = http.expectOne('/api/auth/refresh');
    if (status === 0) request.error(new ProgressEvent('network'));
    else request.flush({}, { status, statusText: 'Error' });
    expect(TestBed.inject(TokenStorage).read() === null).toBe(status === 401 || status === 403);
  });
});

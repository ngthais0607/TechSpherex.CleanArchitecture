import { inject, Injectable, signal } from '@angular/core';
import { catchError, defer, finalize, map, Observable, of, shareReplay, Subject, switchMap, takeUntil, tap, throwError } from 'rxjs';
import { ApiError } from '../api/api-error';
import { QueryInvalidation } from '../api/query-invalidation';
import { AuthApi } from './auth-api';
import { CurrentUserDto, LoginInput } from './auth.dto';
import { TokenStorage } from './token-storage';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly api = inject(AuthApi);
  readonly storage = inject(TokenStorage);
  private readonly invalidation = inject(QueryInvalidation);
  readonly user = signal<CurrentUserDto | null>(null);
  readonly status = signal<'initializing' | 'authenticated' | 'anonymous' | 'error'>('initializing');
  private generation = 0;
  private readonly canceled = new Subject<void>();
  private refreshFlight?: Observable<string>;
  private restoreFlight?: Observable<void>;

  restore(): Observable<void> {
    if (this.restoreFlight) return this.restoreFlight;
    if (this.status() === 'authenticated' || this.status() === 'anonymous') return of(undefined);
    if (!this.storage.read()) { this.status.set('anonymous'); return of(undefined); }
    const generation = this.generation;
    this.status.set('initializing');
    this.restoreFlight = this.validAccessToken().pipe(switchMap(() => this.api.me()),
      tap(user => { if (generation === this.generation) { this.user.set(user); this.status.set('authenticated'); } }),
      map(() => undefined), takeUntil(this.canceled), catchError(error => {
        if (generation === this.generation) {
          if (this.rejected(error)) this.clear(); else this.status.set('error');
        }
        return of(undefined);
      }), finalize(() => { this.restoreFlight = undefined; }), shareReplay({ bufferSize: 1, refCount: false }));
    return this.restoreFlight;
  }
  login(input: LoginInput): Observable<void> {
    return defer(() => {
      this.clear();
      const generation = this.generation;
      return this.api.login(input).pipe(takeUntil(this.canceled), tap(session => this.storage.write(session)),
        switchMap(() => this.api.me()), takeUntil(this.canceled), tap(user => {
          if (generation === this.generation) { this.user.set(user); this.status.set('authenticated'); this.invalidateIdentity(); }
        }), map(() => undefined), catchError(error => {
          if (generation === this.generation) {
            if (this.rejected(error)) this.clear(); else this.status.set('error');
          }
          return throwError(() => error);
        }));
    });
  }
  validAccessToken(): Observable<string> {
    return defer(() => {
      const session = this.storage.read();
      if (!session) return throwError(() => new ApiError(401, 'Auth.Unauthorized', 'Vui lòng đăng nhập.'));
      return Date.parse(session.accessTokenExpiresAt) - Date.now() > 30_000 ? of(session.accessToken) : this.refreshOnce();
    });
  }
  refreshOnce(): Observable<string> {
    if (this.refreshFlight) return this.refreshFlight;
    const session = this.storage.read();
    if (!session) return throwError(() => new ApiError(401, 'Auth.Unauthorized', 'Vui lòng đăng nhập.'));
    const generation = this.generation;
    const flight = this.api.refresh(session.userName, session.refreshToken).pipe(takeUntil(this.canceled),
      tap(value => { if (generation === this.generation) this.storage.write(value); }),
      map(value => value.accessToken), catchError(error => {
        if (generation === this.generation && this.rejected(error)) this.clear();
        return throwError(() => error);
      }), finalize(() => { if (this.refreshFlight === flight) this.refreshFlight = undefined; }),
      shareReplay({ bufferSize: 1, refCount: false }));
    this.refreshFlight = flight;
    return flight;
  }
  logout(): Observable<void> {
    const token = this.storage.read()?.accessToken;
    this.clear();
    return token ? this.api.logout(token).pipe(catchError(() => of(undefined)), map(() => undefined)) : of(undefined);
  }
  private rejected(error: unknown): boolean { return error instanceof ApiError && (error.status === 401 || error.status === 403); }
  private clear(): void {
    this.generation++;
    this.canceled.next();
    this.refreshFlight = undefined;
    this.restoreFlight = undefined;
    this.storage.clear(); this.user.set(null); this.status.set('anonymous');
    this.invalidateIdentity();
  }
  private invalidateIdentity(): void { this.invalidation.invalidate(['containers', 'orders', 'gate', 'yard', 'partners', 'reports', 'lookups']); }
}

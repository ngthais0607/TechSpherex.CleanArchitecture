import { inject } from '@angular/core';
import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, switchMap, throwError } from 'rxjs';
import { isApiUrl } from '../api/api-config';
import { AuthService } from './auth-service';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  if (!isApiUrl(request.url) || /\/auth\/(login|refresh)(?:\?|$)/.test(new URL(request.url, location.origin).pathname)) return next(request);
  const auth = inject(AuthService);
  const withToken = (token: string) => request.clone({ setHeaders: { Authorization: 'Bearer ' + token } });
  return auth.validAccessToken().pipe(switchMap(token => next(withToken(token)).pipe(catchError(error => {
    if (!(error instanceof HttpErrorResponse) || error.status !== 401) return throwError(() => error);
    // Another request may already have rotated the token while this one was in flight.
    const current = auth.storage.read()?.accessToken;
    const retry = current && current !== token ? auth.validAccessToken() : auth.refreshOnce();
    return retry.pipe(switchMap(fresh => next(withToken(fresh))));
  }))));
};

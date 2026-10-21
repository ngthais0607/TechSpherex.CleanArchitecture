import { inject, Injectable } from '@angular/core';
import { HttpBackend, HttpClient, HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { catchError, Observable, throwError } from 'rxjs';
import { API_BASE_URL } from '../api/api-config';
import { normalizeApiError } from '../api/api-error';
import { AuthResponseDto, CurrentUserDto, LoginInput } from './auth.dto';

@Injectable({ providedIn: 'root' })
export class AuthApi {
  private readonly http = inject(HttpClient);
  private readonly raw = new HttpClient(inject(HttpBackend));
  login(input: LoginInput) { return this.raw.post<AuthResponseDto>(API_BASE_URL + '/auth/login', input).pipe(this.errors()); }
  refresh(userName: string, refreshToken: string) {
    return this.raw.post<AuthResponseDto>(API_BASE_URL + '/auth/refresh', { userName, refreshToken }).pipe(this.errors());
  }
  me() { return this.http.get<CurrentUserDto>(API_BASE_URL + '/auth/me'); }
  logout(accessToken?: string) {
    // Capture the old bearer so local logout can clear state immediately without refreshing it.
    return this.raw.post<void>(API_BASE_URL + '/auth/logout', {}, {
      headers: accessToken ? new HttpHeaders({ Authorization: 'Bearer ' + accessToken }) : undefined,
    }).pipe(this.errors());
  }
  private errors<T>() { return catchError<T, Observable<never>>(error => throwError(() => error instanceof HttpErrorResponse ? normalizeApiError(error) : error)); }
}

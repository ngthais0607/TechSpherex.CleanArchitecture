import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';
import { normalizeApiError } from './api-error';
import { isApiUrl } from './api-config';

export const errorInterceptor: HttpInterceptorFn = (request, next) =>
  next(request).pipe(
    catchError((error) =>
      throwError(() =>
        isApiUrl(request.url) && error instanceof HttpErrorResponse
          ? normalizeApiError(error)
          : error,
      ),
    ),
  );

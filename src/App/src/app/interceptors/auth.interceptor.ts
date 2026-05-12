import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { from, switchMap, throwError, catchError } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);

  if (!req.url.startsWith('/api')) {
    return next(req);
  }

  const token = authService.accessToken();
  const apiReq = buildApiRequest(req, token);

  return next(apiReq).pipe(
    catchError((error: HttpErrorResponse) => {
      const isRefreshEndpoint = req.url.includes('/players/refresh');
      if (error.status === 401 && token && !isRefreshEndpoint) {
        return from(authService.refreshToken()).pipe(
          switchMap(newToken => {
            if (!newToken) return throwError(() => error);
            return next(buildApiRequest(req, newToken));
          })
        );
      }
      return throwError(() => error);
    })
  );
};

function buildApiRequest(req: HttpRequest<unknown>, token: string | null): HttpRequest<unknown> {
  return req.clone({
    withCredentials: true,
    ...(token ? { setHeaders: { Authorization: `Bearer ${token}` } } : {}),
  });
}

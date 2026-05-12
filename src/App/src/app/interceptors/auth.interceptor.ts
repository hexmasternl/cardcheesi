import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { from, switchMap, throwError, catchError } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);

  if (!req.url.startsWith('/api')) {
    return next(req);
  }

  // Refresh endpoint authenticates via HttpOnly cookie only — never add a Bearer token
  if (req.url.includes('/players/refresh')) {
    return next(req.clone({ withCredentials: true }));
  }

  const token = authService.accessToken();
  const apiReq = buildApiRequest(req, token);

  return next(apiReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) {
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

import { HttpErrorResponse, HttpHandlerFn, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { from, switchMap, throwError, catchError } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);

  const token = authService.accessToken();
  const authReq = token ? addBearerToken(req, token) : req;

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401 && token) {
        return from(authService.refreshToken()).pipe(
          switchMap(newToken => {
            if (!newToken) return throwError(() => error);
            return next(addBearerToken(req, newToken));
          })
        );
      }
      return throwError(() => error);
    })
  );
};

function addBearerToken(req: HttpRequest<unknown>, token: string): HttpRequest<unknown> {
  return req.clone({
    setHeaders: { Authorization: `Bearer ${token}` },
  });
}

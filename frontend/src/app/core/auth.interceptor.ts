import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';
import { ToastService } from './toast.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const toast = inject(ToastService);

  return next(req.clone({ withCredentials: true })).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse) {
        const path = req.url.split('?')[0];
        if (error.status === 401 && path !== '/api/me' && !path.startsWith('/api/auth/')) {
          auth.user.set(null);
          void auth.requestSignIn();
        } else if (error.status === 429) {
          toast.show('Too many requests. Wait a moment and try again.', 'error');
        } else if (error.status === 503) {
          toast.show(error.error?.title ?? 'Solana is unreachable right now. Try again.', 'error');
        }
      }
      return throwError(() => error);
    }),
  );
};

import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';
import { NO_SOLANA, SIGNED_OUT, TOO_MANY } from './error-text';
import { ToastService } from './toast.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const toast = inject(ToastService);

  return next(req.clone({ withCredentials: true })).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse) {
        const path = req.url.split('?')[0];
        if (error.status === 401 && path !== '/api/me' && !path.startsWith('/api/auth/')) {
          const wasSignedIn = !!auth.user();
          auth.user.set(null);
          if (wasSignedIn) toast.show(SIGNED_OUT);
          void auth.requestSignIn();
        } else if (error.status === 429) {
          toast.show(TOO_MANY, 'error');
        } else if (error.status === 503) {
          toast.show(NO_SOLANA, 'error');
        }
      }
      return throwError(() => error);
    }),
  );
};

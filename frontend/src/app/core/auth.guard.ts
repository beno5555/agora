import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

/** Protected pages: ask the user to sign in, and go home if they decline. */
export const authGuard: CanActivateFn = async () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (await auth.requestSignIn()) return true;
  return router.createUrlTree(['/']);
};

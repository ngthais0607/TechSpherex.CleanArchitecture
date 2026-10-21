import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs';
import { AuthService } from './auth-service';
import { can, Capability } from './permissions';

export const authGuard: CanActivateFn = (route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return auth.restore().pipe(
    map(() => {
      if (auth.status() !== 'authenticated')
        return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
      const capability = route.data['capability'] as Capability | undefined;
      return !capability || can(capability, auth.user()?.roles ?? [])
        ? true
        : router.createUrlTree(['/dashboard']);
    }),
  );
};

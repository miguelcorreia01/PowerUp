import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from '../auth.service';
import { map, take } from 'rxjs/operators';

export const roleGuard = (allowedRoles: string[]): CanActivateFn => {
  return (route, state) => {
    const authService = inject(AuthService);
    const router = inject(Router);

    return authService.currentUser$.pipe(
      take(1),
      map(user => {
        if (!user || !authService.isLoggedIn()) {
          router.navigate(['/login'], { queryParams: { returnUrl: state.url } });
          return false;
        }

        const userRole = user.role?.toLowerCase() || '';
        const normalizedAllowedRoles = allowedRoles.map(role => role.toLowerCase());
        const hasAccess = normalizedAllowedRoles.some(role => role === userRole);

        if (hasAccess) {
          return true;
        }

        const role = user.role?.toLowerCase();
        if (role === 'admin') {
          router.navigate(['/admin-dashboard']);
        } else if (role === 'instructor') {
          router.navigate(['/dashboard']);
        } else {
          router.navigate(['/dashboard']);
        }
        return false;
      })
    );
  };
};

export const adminGuard: CanActivateFn = roleGuard(['Admin']);

export const instructorGuard: CanActivateFn = roleGuard(['Instructor']);

export const memberGuard: CanActivateFn = roleGuard(['Member']);

export const adminOrInstructorGuard: CanActivateFn = roleGuard(['Admin', 'Instructor']);

export const instructorOrMemberGuard: CanActivateFn = roleGuard(['Instructor', 'Member']);

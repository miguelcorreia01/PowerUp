import { Routes } from '@angular/router';
import { Login } from './core/auth/pages/login/login';
import { Register } from './core/auth/pages/register/register';
import { Dashboard } from './features/dashboard/pages/dashboard/dashboard';
import { PersonalTraining } from './features/personal-training/pages/personal-training/personal-training';
import { GroupClasses } from './features/group-classes/pages/group-classes/group-classes';
import { Membership } from './features/membership/pages/membership/membership';
import { Profile } from './features/profile/pages/profile/profile';
import { AdminDashboard } from './features/admin-dashboard/pages/admin-dashboard/admin-dashboard';
import { Users } from './features/users/pages/users/users';
import { AdminMembership } from './features/admin-membership/pages/admin-membership/admin-membership';
import { authGuard } from './core/auth/guards/auth.guard';
import { adminGuard, instructorOrMemberGuard, memberGuard } from './core/auth/guards/role.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  { path: 'login', component: Login },
  { path: 'register', component: Register },
  
  { 
    path: 'dashboard', 
    component: Dashboard,
    canActivate: [authGuard]
  },
  { 
    path: 'personal-training', 
    component: PersonalTraining,
    canActivate: [authGuard, instructorOrMemberGuard] 
  },
  { 
    path: 'group-classes', 
    component: GroupClasses,
    canActivate: [authGuard, instructorOrMemberGuard]
  },
  { 
    path: 'membership', 
    component: Membership,
    canActivate: [authGuard, memberGuard]
  },
  { 
    path: 'profile', 
    component: Profile,
    canActivate: [authGuard]
  },
  { 
    path: 'profile/:id', 
    component: Profile,
    canActivate: [authGuard]
  },
  
  // Admin-only routes
  {
    path: 'admin-dashboard',
    component: AdminDashboard,
    canActivate: [authGuard, adminGuard]
  },
  {
    path: 'users',
    component: Users,
    canActivate: [authGuard, adminGuard]
  },
  {
    path: 'admin-membership',
    component: AdminMembership,
    canActivate: [authGuard, adminGuard]
  }
];
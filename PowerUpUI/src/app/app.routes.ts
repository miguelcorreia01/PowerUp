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

export const routes: Routes = [
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  { path: 'login', component: Login },
  { path: 'register', component: Register },
  { path: 'dashboard', component: Dashboard },
  { path: 'personal-training', component: PersonalTraining },
  { path: 'group-classes', component: GroupClasses },
  { path: 'membership', component: Membership },
  { path: 'profile', component: Profile },
  { path: 'profile/:id', component: Profile },
  {path: 'admin-dashboard', component: AdminDashboard },
  {path: 'users', component: Users }
];
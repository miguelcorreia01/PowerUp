import { Routes } from '@angular/router';
import { Login } from './core/auth/pages/login/login';
import { Register } from './core/auth/pages/register/register';
import { Dashboard } from './features/dashboard/pages/dashboard/dashboard';
import { PersonalTraining } from './features/personal-training/pages/personal-training/personal-training';

export const routes: Routes = [
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  { path: 'login', component: Login },
  { path: 'register', component: Register },
  { path: 'dashboard', component: Dashboard },
  { path: 'personal-training', component: PersonalTraining },
  { path: 'group-classes', redirectTo: 'dashboard' },
  { path: 'membership', redirectTo: 'dashboard' }
];
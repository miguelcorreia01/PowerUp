import { Routes } from '@angular/router';
import { Login } from './core/auth/pages/login/login';
import { Register } from './core/auth/pages/register/register';

export const routes: Routes = [
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  { path: 'login', component: Login },
  { path: 'register', component: Register },
  { path: 'dashboard', redirectTo: 'login' },
];
import { Routes } from '@angular/router';
import { authGuard } from './core/auth/guards';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'sales' },
  {
    path: '',
    canActivate: [authGuard],
    canActivateChild: [authGuard],
    loadComponent: () => import('./layout/shell/shell').then(m => m.Shell),
    children: [],
  },
  { path: '**', title: 'Page not found', loadComponent: () => import('./features/not-found/not-found').then(m => m.NotFound) },
];

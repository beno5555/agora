import { Routes } from '@angular/router';
import { authGuard } from './core/auth.guard';

export const routes: Routes = [
  { path: '', loadComponent: () => import('./pages/home/home').then(m => m.Home) },
  { path: 'g/:slug', loadComponent: () => import('./pages/game/game').then(m => m.Game) },
  { path: 'l/:id', loadComponent: () => import('./pages/listing/listing').then(m => m.Listing) },
  { path: 'd/:id', canActivate: [authGuard], loadComponent: () => import('./pages/deal/deal').then(m => m.Deal) },
  { path: 'sell', canActivate: [authGuard], loadComponent: () => import('./pages/sell/sell').then(m => m.Sell) },
  { path: 'sell/:id', canActivate: [authGuard], loadComponent: () => import('./pages/sell/sell').then(m => m.Sell) },
  { path: 'me', canActivate: [authGuard], loadComponent: () => import('./pages/me/me').then(m => m.Me) },
  { path: '**', redirectTo: '' },
];

import { Routes } from '@angular/router';
import { authGuard } from './core/auth.guard';

export const routes: Routes = [
  { path: '', loadComponent: () => import('./pages/home/home').then((m) => m.Home) },
  // A page with a name of its own replaces its route's title once loaded (core/page-title.ts).
  {
    path: 'g/:slug',
    title: 'Game',
    loadComponent: () => import('./pages/game/game').then((m) => m.Game),
  },
  {
    path: 'l/:id',
    title: 'Listing',
    loadComponent: () => import('./pages/listing/listing').then((m) => m.Listing),
  },
  {
    path: 'd/:id',
    title: 'Deal',
    canActivate: [authGuard],
    loadComponent: () => import('./pages/deal/deal').then((m) => m.Deal),
  },
  {
    path: 'sell',
    title: 'Sell something',
    canActivate: [authGuard],
    loadComponent: () => import('./pages/sell/sell').then((m) => m.Sell),
  },
  {
    path: 'sell/:id',
    title: 'Edit listing',
    canActivate: [authGuard],
    loadComponent: () => import('./pages/sell/sell').then((m) => m.Sell),
  },
  {
    path: 'me',
    title: 'My deals',
    canActivate: [authGuard],
    loadComponent: () => import('./pages/me/me').then((m) => m.Me),
  },
  {
    path: 'u/:wallet',
    title: 'Profile',
    loadComponent: () => import('./pages/profile/profile').then((m) => m.Profile),
  },
  { path: '**', redirectTo: '' },
];

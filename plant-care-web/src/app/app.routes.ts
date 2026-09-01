import { Routes } from '@angular/router';

import { authGuard } from './core/auth/guards/auth.guard';
import { guestGuard } from './core/auth/guards/guest.guard';

export const routes: Routes = [
  {
    path: 'plants',
    loadComponent: () =>
      import(
        './features/plant-catalog/pages/plant-catalog-page/plant-catalog-page'
      ).then(
        (component) => component.PlantCatalogPage,
      ),
  },
  {
    path: 'plants/:id',
    loadComponent: () =>
      import(
        './features/plant-catalog/pages/plant-detail-page/plant-detail-page'
      ).then(
        (component) => component.PlantDetailPage,
      ),
  },
  {
    path: 'my-plants/:id/care-history',
    canActivate: [
      authGuard,
    ],
    loadComponent: () =>
      import(
        './features/my-plants/pages/care-history-page/care-history-page'
      ).then(
        (component) => component.CareHistoryPage,
      ),
  },
  {
    path: 'my-plants',
    canActivate: [
      authGuard,
    ],
    loadComponent: () =>
      import(
        './features/my-plants/pages/my-plants-page/my-plants-page'
      ).then(
        (component) => component.MyPlantsPage,
      ),
  },
  {
    path: 'login',
    canActivate: [
      guestGuard,
    ],
    loadComponent: () =>
      import(
        './features/auth/pages/login-page/login-page'
      ).then(
        (component) => component.LoginPage,
      ),
  },
  {
    path: 'register',
    canActivate: [
      guestGuard,
    ],
    loadComponent: () =>
      import(
        './features/auth/pages/register-page/register-page'
      ).then(
        (component) => component.RegisterPage,
      ),
  },
  {
    path: 'dashboard',
    canActivate: [
      authGuard,
    ],
    loadComponent: () =>
      import(
        './features/dashboard/pages/dashboard-page/dashboard-page'
      ).then(
        component =>
          component.DashboardPage,
      ),
  },
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'plants',
  },
  {
    path: '**',
    redirectTo: 'plants',
  },
];

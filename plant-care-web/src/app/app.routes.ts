import { Routes } from '@angular/router';

import { authGuard } from './core/auth/guards/auth.guard';
import { guestGuard } from './core/auth/guards/guest.guard';

export const routes: Routes = [
  {
    path: 'account', canActivate: [authGuard],
    loadComponent: () => import('./features/account/account-page').then(c => c.AccountPage),
  },
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
    path: 'plants/compare',
    loadComponent: () => import('./features/plant-catalog/pages/plant-comparison-page/plant-comparison-page')
      .then(component => component.PlantComparisonPage),
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
    path: 'my-plants/:id',
    canActivate: [authGuard],
    loadComponent: () => import('./features/my-plants/pages/my-plant-detail-page/my-plant-detail-page')
      .then(component => component.MyPlantDetailPage),
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
    path: 'forgot-password',
    canActivate: [guestGuard],
    loadComponent: () =>
      import(
        './features/auth/pages/forgot-password-page/forgot-password-page'
      ).then(
        component => component.ForgotPasswordPage,
      ),
  },
  {
    path: 'reset-password',
    canActivate: [guestGuard],
    loadComponent: () =>
      import(
        './features/auth/pages/reset-password-page/reset-password-page'
      ).then(
        component => component.ResetPasswordPage,
      ),
  },
  {
    path: 'resend-confirmation',
    canActivate: [guestGuard],
    loadComponent: () =>
      import(
        './features/auth/pages/resend-confirmation-page/resend-confirmation-page'
      ).then(
        component => component.ResendConfirmationPage,
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
    path: 'notifications',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/notifications/pages/notifications-page/notifications-page')
        .then(component => component.NotificationsPage),
  },
  {
    path: 'calendar',
    canActivate: [authGuard],
    loadComponent: () => import('./features/calendar/pages/calendar-page/calendar-page')
      .then(component => component.CalendarPage),
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

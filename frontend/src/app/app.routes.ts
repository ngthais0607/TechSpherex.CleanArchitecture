import { inject } from '@angular/core';
import { Routes, Router, CanActivateFn, CanDeactivateFn } from '@angular/router';
import { DemoSession } from './core/session/demo-session';
const guard: CanActivateFn = (_, state) =>
  inject(DemoSession).currentUser()
    ? true
    : inject(Router).createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
const pendingGateGuard: CanDeactivateFn<{ hasUnsavedChanges(): boolean }> = (component) =>
  !component.hasUnsavedChanges() || confirm('Bỏ nội dung phiếu cổng chưa ghi nhận?');

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/login/login-page').then((m) => m.LoginPage),
  },
  {
    path: 'dashboard',
    canActivate: [guard],
    loadComponent: () => import('./features/dashboard/dashboard-page').then((m) => m.DashboardPage),
  },
  {
    path: 'containers',
    canActivate: [guard],
    loadComponent: () =>
      import('./features/containers/containers-page').then((m) => m.ContainersPage),
  },
  {
    path: 'delivery-orders',
    canActivate: [guard],
    loadComponent: () => import('./features/delivery-orders/orders-page').then((m) => m.OrdersPage),
  },
  {
    path: 'gate',
    canActivate: [guard],
    canDeactivate: [pendingGateGuard],
    loadComponent: () => import('./features/gate/gate-page').then((m) => m.GatePage),
  },
  {
    path: 'yard',
    canActivate: [guard],
    loadComponent: () => import('./features/yard/yard-page').then((m) => m.YardPage),
  },
  {
    path: 'partners',
    canActivate: [guard],
    loadComponent: () => import('./features/partners/partners-page').then((m) => m.PartnersPage),
  },
  {
    path: 'reports',
    canActivate: [guard],
    loadComponent: () => import('./features/reports/reports-page').then((m) => m.ReportsPage),
  },
  {
    path: 'inventory',
    canActivate: [guard],
    loadComponent: () => import('./features/inventory/inventory-page').then((m) => m.InventoryPage),
  },
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  { path: '**', redirectTo: 'dashboard' },
];

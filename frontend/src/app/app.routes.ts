import { Routes, CanDeactivateFn } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';
import { Capability } from './core/auth/permissions';
const pendingGateGuard: CanDeactivateFn<{ hasUnsavedChanges?: () => boolean }> = (component) =>
  !component.hasUnsavedChanges?.() || confirm('Bỏ nội dung phiếu cổng chưa ghi nhận?');
const dashboard = () => import('./features/dashboard/dashboard-page').then((m) => m.DashboardPage);
const reports = () => import('./features/reports/reports-page').then((m) => m.ReportsPage);
const inventory = () => import('./features/inventory/inventory-page').then((m) => m.InventoryPage);
const containers = () =>
  import('./features/containers/containers-page').then((m) => m.ContainersPage);
const orders = () => import('./features/delivery-orders/orders-page').then((m) => m.OrdersPage);
const yard = () => import('./features/yard/yard-page').then((m) => m.YardPage);
const gate = () => import('./features/gate/gate-page').then((m) => m.GatePage);
const partners = () => import('./features/partners/partners-page').then((m) => m.PartnersPage);
export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/login/login-page').then((m) => m.LoginPage),
  },
  ...(
    [
      ['dashboard', 'readReports'],
      ['containers', 'readContainers'],
      ['delivery-orders', 'readOrders'],
      ['gate', 'manageGate'],
      ['yard', 'readYard'],
      ['partners', 'readMasterData'],
      ['reports', 'readReports'],
      ['inventory', 'readReports'],
    ] as [string, Capability][]
  ).map(([path, capability]) => ({
    path,
    canActivate: [authGuard],
    ...(path === 'gate' ? { canDeactivate: [pendingGateGuard] } : {}),
    data: { capability },
    loadComponent:
      path === 'containers'
        ? containers
        : path === 'delivery-orders'
          ? orders
          : path === 'yard'
            ? yard
            : path === 'gate'
              ? gate
              : path === 'partners'
                ? partners
                : path === 'reports'
                  ? reports
                  : path === 'inventory'
                    ? inventory
                    : dashboard,
  })),
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  { path: '**', redirectTo: 'dashboard' },
];

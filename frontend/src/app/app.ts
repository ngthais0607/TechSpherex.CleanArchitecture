import { Component, computed, signal, inject } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive, Router, NavigationEnd } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AuthService } from './core/auth/auth-service';
import { can, Capability } from './core/auth/permissions';
import { Notifications } from './core/notifications/notifications';
import { Icon } from './shared/ui';
@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, Icon],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  readonly session = inject(AuthService);
  readonly notifications = inject(Notifications);
  readonly router = inject(Router);
  readonly navOpen = signal(false);
  readonly pageTitle = signal('Tổng quan');
  private readonly items: { path: string; label: string; icon: string; capability: Capability }[] =
    [
      { path: '/dashboard', label: 'Tổng quan', icon: 'grid', capability: 'readReports' },
      { path: '/containers', label: 'Container', icon: 'box', capability: 'readContainers' },
      { path: '/gate', label: 'Cổng nhập / xuất', icon: 'gate', capability: 'manageGate' },
      { path: '/delivery-orders', label: 'Lệnh giao', icon: 'orders', capability: 'readOrders' },
      { path: '/yard', label: 'Sơ đồ bãi', icon: 'map', capability: 'readYard' },
      { path: '/partners', label: 'Khách hàng', icon: 'users', capability: 'readMasterData' },
      { path: '/reports', label: 'Báo cáo', icon: 'chart', capability: 'readReports' },
      { path: '/inventory', label: 'Tồn bãi', icon: 'box', capability: 'readReports' },
    ];
  readonly nav = computed(() =>
    this.items.filter((item) => can(item.capability, this.session.user()?.roles ?? [])),
  );
  constructor() {
    this.router.events.pipe(takeUntilDestroyed()).subscribe((event) => {
      if (event instanceof NavigationEnd)
        this.pageTitle.set(
          this.items.find((item) => event.urlAfterRedirects.startsWith(item.path))?.label ??
            'Tổng quan',
        );
    });
  }
  async logout(): Promise<void> {
    if (await this.router.navigateByUrl('/login')) this.session.logout().subscribe();
  }
}

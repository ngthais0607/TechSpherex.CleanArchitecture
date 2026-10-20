import { Component, signal, inject } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive, Router, NavigationEnd } from '@angular/router';
import { DemoSession } from './core/session/demo-session';
import { DemoRepository } from './demo/demo-repository';
import { Icon } from './shared/ui';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, Icon],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  session = inject(DemoSession);
  store = inject(DemoRepository);
  router = inject(Router);
  navOpen = signal(false);
  tools = signal(false);
  pageTitle = signal('Tổng quan');
  nav = [
    { path: '/dashboard', label: 'Tổng quan', icon: 'grid' },
    { path: '/containers', label: 'Container', icon: 'box' },
    { path: '/gate', label: 'Cổng nhập / xuất', icon: 'gate' },
    { path: '/delivery-orders', label: 'Lệnh giao', icon: 'orders' },
    { path: '/yard', label: 'Sơ đồ bãi', icon: 'map' },
    { path: '/partners', label: 'Khách hàng', icon: 'users' },
    { path: '/reports', label: 'Báo cáo', icon: 'chart' },
    { path: '/inventory', label: 'Tồn bãi', icon: 'box' },
  ];
  constructor() {
    this.router.events.subscribe((e) => {
      if (e instanceof NavigationEnd)
        this.pageTitle.set(
          this.nav.find((n) => e.urlAfterRedirects.startsWith(n.path))?.label || 'Tổng quan',
        );
    });
  }
  logout() {
    this.session.logout();
    this.router.navigateByUrl('/login');
  }
  reset(empty: boolean) {
    if (
      confirm(
        empty
          ? 'Xóa dữ liệu nghiệp vụ demo? Danh mục và cấu hình bãi vẫn được giữ.'
          : 'Khôi phục dữ liệu mẫu? Thao tác hiện tại sẽ bị thay thế.',
      )
    )
      this.store.reset(empty);
  }
}

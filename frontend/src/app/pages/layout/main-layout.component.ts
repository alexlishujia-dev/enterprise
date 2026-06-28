import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import { NzAvatarModule } from 'ng-zorro-antd/avatar';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzDropDownModule } from 'ng-zorro-antd/dropdown';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzLayoutModule } from 'ng-zorro-antd/layout';
import { NzMenuModule } from 'ng-zorro-antd/menu';
import { NzTabsModule } from 'ng-zorro-antd/tabs';
import { APP_CONSTANTS } from '../../core/config/app.constants';
import { SysMenuNavDto } from '../../core/models/sys-menu.model';
import { AuthService } from '../../service/auth.service';
import { LayoutTabService } from '../../service/layout-tab.service';
import { resolveTabRoot } from '../../shared/utils/layout-tab.util';

interface MenuItem {
  title: string;
  icon: string;
  path: string;
}

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [
    RouterOutlet,
    RouterLink,
    NzLayoutModule,
    NzMenuModule,
    NzIconModule,
    NzButtonModule,
    NzAvatarModule,
    NzDropDownModule,
    NzTabsModule
  ],
  templateUrl: './main-layout.component.html',
  styleUrl: './main-layout.component.scss'
})
export class MainLayoutComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly tabService = inject(LayoutTabService);

  readonly appTitle = APP_CONSTANTS.appTitle;
  readonly user = this.auth.user;
  readonly tabs = this.tabService.tabList;

  collapsed = false;
  currentPath = signal(normalizePath(this.router.url));

  readonly menus = computed(() => this.flattenMenus(this.auth.getMenus()));

  readonly displayName = computed(() =>
    this.user()?.displayName || this.user()?.userName || '用户'
  );

  readonly selectedTabIndex = computed(() => {
    const tabRoot = resolveTabRoot(this.currentPath());
    const index = this.tabs().findIndex(t => t.path === tabRoot);
    return index >= 0 ? index : 0;
  });

  ngOnInit(): void {
    if (this.auth.isAuthenticated()) {
      this.auth.refreshCurrentUser().subscribe({ error: () => undefined });
    }

    this.syncTab(this.currentPath());

    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd)
    ).subscribe(e => {
      const path = normalizePath(e.urlAfterRedirects);
      this.currentPath.set(path);
      this.syncTab(path);
    });
  }

  toggleCollapsed(): void {
    this.collapsed = !this.collapsed;
  }

  logout(): void {
    this.tabService.reset();
    this.auth.logout();
    void this.router.navigate(['/login']);
  }

  onTabSelect(index: number): void {
    const tab = this.tabs()[index];
    if (tab) {
      this.tabService.activate(tab.path);
    }
  }

  onTabClose(index: number): void {
    this.tabService.close(index);
  }

  isMenuActive(menuPath: string): boolean {
    return resolveTabRoot(this.currentPath()) === menuPath;
  }

  private syncTab(path: string): void {
    this.tabService.open(path);
  }

  private flattenMenus(menus: SysMenuNavDto[]): MenuItem[] {
    const result: MenuItem[] = [];
    for (const menu of menus) {
      if (menu.path) {
        result.push({
          title: menu.menuName,
          icon: menu.icon || 'file',
          path: menu.path
        });
      }
      if (menu.children?.length) {
        result.push(...this.flattenMenus(menu.children));
      }
    }
    return result;
  }
}

function normalizePath(url: string): string {
  const path = url.split('?')[0].split('#')[0];
  if (!path || path === '/') {
    return '/dashboard';
  }
  return path.startsWith('/') ? path : `/${path}`;
}

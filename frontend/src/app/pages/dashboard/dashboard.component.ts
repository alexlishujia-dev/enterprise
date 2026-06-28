import { Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzColDirective, NzRowDirective } from 'ng-zorro-antd/grid';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzStatisticModule } from 'ng-zorro-antd/statistic';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { SysMenuNavDto } from '../../core/models/sys-menu.model';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { AuthService } from '../../service/auth.service';

interface QuickLink {
  title: string;
  icon: string;
  path: string;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    RouterLink,
    PageHeaderComponent,
    NzCardModule,
    NzRowDirective,
    NzColDirective,
    NzStatisticModule,
    NzTagModule,
    NzIconModule
  ],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent {
  readonly auth = inject(AuthService);

  /** 快捷入口仅展示当前用户有权限访问的菜单（排除工作台自身） */
  readonly quickLinks = computed(() =>
    this.flattenMenus(this.auth.getMenus())
      .filter(m => m.path && m.path !== '/dashboard')
  );

  private flattenMenus(menus: SysMenuNavDto[]): QuickLink[] {
    const result: QuickLink[] = [];
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

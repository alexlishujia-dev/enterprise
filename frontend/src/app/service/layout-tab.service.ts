import { Injectable, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { isPathInTabRoot, resolveTabRoot, resolveTabTitle } from '../shared/utils/layout-tab.util';

export interface LayoutTab {
  path: string;
  title: string;
  closable: boolean;
}

const DASHBOARD_PATH = '/dashboard';

@Injectable({ providedIn: 'root' })
export class LayoutTabService {
  private readonly router = inject(Router);
  private readonly tabs = signal<LayoutTab[]>([
    { path: DASHBOARD_PATH, title: '工作台', closable: false }
  ]);

  readonly tabList = this.tabs.asReadonly();

  /** 打开或激活页签（同菜单子路由共用页签根路径，不新建页签） */
  open(path: string): void {
    const tabRoot = resolveTabRoot(path);
    if (!tabRoot || tabRoot === '/login') {
      return;
    }

    const list = this.tabs();
    if (list.some(t => t.path === tabRoot)) {
      return;
    }

    this.tabs.set([
      ...list,
      {
        path: tabRoot,
        title: resolveTabTitle(tabRoot),
        closable: tabRoot !== DASHBOARD_PATH
      }
    ]);
  }

  activate(tabRoot: string): void {
    const normalized = resolveTabRoot(tabRoot);
    const currentRoot = resolveTabRoot(this.router.url);
    if (currentRoot !== normalized) {
      void this.router.navigateByUrl(normalized);
    }
  }

  close(index: number): void {
    const list = this.tabs();
    const target = list[index];
    if (!target?.closable) {
      return;
    }

    const remaining = list.filter((_, i) => i !== index);
    const nextList = remaining.length > 0
      ? remaining
      : [{ path: DASHBOARD_PATH, title: '工作台', closable: false }];

    this.tabs.set(nextList);

    if (isPathInTabRoot(this.router.url, target.path)) {
      const fallback = nextList[Math.min(index, nextList.length - 1)] ?? nextList[0];
      void this.router.navigateByUrl(fallback.path);
    }
  }

  closeOthers(activePath: string): void {
    const tabRoot = resolveTabRoot(activePath);
    this.tabs.set(
      this.tabs().filter(t => !t.closable || t.path === tabRoot)
    );
  }

  reset(): void {
    this.tabs.set([{ path: DASHBOARD_PATH, title: '工作台', closable: false }]);
  }
}

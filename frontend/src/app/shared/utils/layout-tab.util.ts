const DASHBOARD_PATH = '/dashboard';

/** 菜单页签根路径：同一菜单下的子页面共享一个页签 */
const TAB_ROOT_RULES: { root: string; prefix: string }[] = [
  { root: '/system/users', prefix: '/system/users' },
  { root: '/system/roles', prefix: '/system/roles' },
  { root: '/system/role-permissions', prefix: '/system/role-permissions' },
  { root: '/system/logs', prefix: '/system/logs' }
];

const TAB_TITLES: Record<string, string> = {
  [DASHBOARD_PATH]: '工作台',
  '/system/users': '用户管理',
  '/system/roles': '角色管理',
  '/system/role-permissions': '角色权限',
  '/system/logs': '操作日志'
};

export function normalizeLayoutPath(url: string): string {
  const path = url.split('?')[0].split('#')[0];
  if (!path || path === '/') {
    return DASHBOARD_PATH;
  }
  return path.startsWith('/') ? path : `/${path}`;
}

/** 将当前路由映射到页签根路径（同菜单共用一个页签） */
export function resolveTabRoot(path: string): string {
  const normalized = normalizeLayoutPath(path);
  for (const rule of TAB_ROOT_RULES) {
    if (normalized === rule.root || normalized.startsWith(`${rule.prefix}/`)) {
      return rule.root;
    }
  }
  return normalized;
}

export function resolveTabTitle(tabRoot: string): string {
  return TAB_TITLES[tabRoot] ?? '页面';
}

export function isPathInTabRoot(path: string, tabRoot: string): boolean {
  return resolveTabRoot(path) === tabRoot;
}

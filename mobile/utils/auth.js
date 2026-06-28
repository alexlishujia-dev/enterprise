/** 权限判断 — 与 Angular HasPermissionDirective 对齐 */
export function hasPermission(user, permissionCode) {
  return user?.permissions?.includes(permissionCode) ?? false;
}

export function hasRole(user, roleCode) {
  return user?.roles?.includes(roleCode) ?? false;
}

export function hasAnyPermission(user, codes) {
  return codes.some(code => hasPermission(user, code));
}

export function filterMenusByPermission(menus, user) {
  if (!menus?.length) return [];
  return menus
    .filter(menu => canAccessMenu(menu, user))
    .map(menu => ({
      ...menu,
      children: filterMenusByPermission(menu.children || [], user)
    }));
}

function canAccessMenu(menu, user) {
  if (!menu.children?.length) {
    return true;
  }
  return filterMenusByPermission(menu.children, user).length > 0;
}

export function requireLogin() {
  const pages = getCurrentPages();
  const current = pages[pages.length - 1];
  const route = current?.route ? `/${current.route}` : '';
  if (route.includes('login')) return;
  uni.reLaunch({ url: '/pages/login/login' });
}

export function requirePermission(user, permissionCode, message = '无权访问') {
  if (!hasPermission(user, permissionCode)) {
    uni.showToast({ title: message, icon: 'none' });
    return false;
  }
  return true;
}

import { NzTreeNodeOptions } from 'ng-zorro-antd/tree';
import { SysMenuTreeDto } from '../../core/models/sys-menu.model';

const PERM_KEY_PREFIX = 'p-';
const MENU_KEY_PREFIX = 'm-';

export function permNodeKey(permissionId: number): string {
  return `${PERM_KEY_PREFIX}${permissionId}`;
}

export function menuNodeKey(menuId: number): string {
  return `${MENU_KEY_PREFIX}${menuId}`;
}

export function parsePermissionIdFromKey(key: string): number | null {
  if (!key.startsWith(PERM_KEY_PREFIX)) {
    return null;
  }
  const id = Number(key.slice(PERM_KEY_PREFIX.length));
  return Number.isFinite(id) ? id : null;
}

export function buildMenuPermissionTree(menus: SysMenuTreeDto[]): NzTreeNodeOptions[] {
  return menus.map(menu => toTreeNode(menu));
}

export function collectPermissionKeys(nodes: NzTreeNodeOptions[]): string[] {
  const keys: string[] = [];
  const walk = (list: NzTreeNodeOptions[]) => {
    for (const node of list) {
      const permId = parsePermissionIdFromKey(node.key);
      if (permId != null) {
        keys.push(node.key);
      }
      if (node.children?.length) {
        walk(node.children);
      }
    }
  };
  walk(nodes);
  return keys;
}

export function collectMenuKeys(nodes: NzTreeNodeOptions[]): string[] {
  const keys: string[] = [];
  const walk = (list: NzTreeNodeOptions[]) => {
    for (const node of list) {
      if (node.key.startsWith(MENU_KEY_PREFIX)) {
        keys.push(node.key);
      }
      if (node.children?.length) {
        walk(node.children);
      }
    }
  };
  walk(nodes);
  return keys;
}

export function setTreeExpanded(nodes: NzTreeNodeOptions[], expanded: boolean): NzTreeNodeOptions[] {
  return nodes.map(node => ({
    ...node,
    expanded,
    children: node.children?.length ? setTreeExpanded(node.children, expanded) : node.children
  }));
}

/** 搜索过滤：菜单命中时保留全部子权限；权限命中时保留对应菜单路径 */
export function filterPermissionTree(nodes: NzTreeNodeOptions[], keyword: string): NzTreeNodeOptions[] {
  const kw = keyword.trim().toLowerCase();
  if (!kw) {
    return nodes;
  }

  const result: NzTreeNodeOptions[] = [];
  for (const node of nodes) {
    const filtered = filterNode(node, kw);
    if (filtered) {
      result.push(filtered);
    }
  }
  return result;
}

function filterNode(node: NzTreeNodeOptions, kw: string): NzTreeNodeOptions | null {
  const isMenu = node.key.startsWith(MENU_KEY_PREFIX);
  if (!isMenu) {
    return nodeMatches(node, kw) ? node : null;
  }

  const children = node.children ?? [];
  const childMenus = children.filter(c => c.key.startsWith(MENU_KEY_PREFIX));
  const permNodes = children.filter(c => c.key.startsWith(PERM_KEY_PREFIX));
  const menuMatched = nodeMatches(node, kw);

  const filteredChildMenus = childMenus
    .map(c => filterNode(c, kw))
    .filter((c): c is NzTreeNodeOptions => c != null);

  const matchedPerms = permNodes.filter(p => nodeMatches(p, kw));

  if (menuMatched) {
    return {
      ...node,
      expanded: true,
      children: [...childMenus, ...permNodes]
    };
  }

  if (filteredChildMenus.length || matchedPerms.length) {
    return {
      ...node,
      expanded: true,
      children: [...filteredChildMenus, ...matchedPerms]
    };
  }

  return null;
}

function nodeMatches(node: NzTreeNodeOptions, kw: string): boolean {
  const title = String(node.title ?? '').toLowerCase();
  const code = String(node['permissionCode'] ?? '').toLowerCase();
  const desc = String(node['description'] ?? '').toLowerCase();
  return title.includes(kw) || code.includes(kw) || desc.includes(kw);
}

function toTreeNode(menu: SysMenuTreeDto): NzTreeNodeOptions {
  const childMenus = menu.children.map(toTreeNode);
  const permNodes: NzTreeNodeOptions[] = menu.permissions.map(p => ({
    title: `${p.permissionName}`,
    key: permNodeKey(p.id),
    isLeaf: true,
    icon: 'key',
    permissionCode: p.permissionCode,
    description: p.description
  }));

  return {
    title: menu.menuName,
    key: menuNodeKey(menu.id),
    icon: menu.icon || 'folder',
    children: [...childMenus, ...permNodes]
  };
}

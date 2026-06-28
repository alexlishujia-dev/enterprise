-- EnterprisePlatform 菜单与权限种子数据
-- 用法：psql -U platform -d enterprise -f 03_menu_permission_seed.sql
-- 注意：应用启动时 DataSeeder 也会自动初始化，本脚本供手动部署使用。

-- 工作台
INSERT INTO sys_menu (ParentId, MenuCode, MenuName, Path, Icon, SortOrder, IsActive)
SELECT NULL, 'dashboard', '工作台', '/dashboard', 'dashboard', 1, TRUE
WHERE NOT EXISTS (SELECT 1 FROM sys_menu WHERE MenuCode = 'dashboard');

INSERT INTO sys_permission (MenuId, PermissionCode, PermissionName, Description)
SELECT m.Id, 'dashboard:view', '查看', '访问工作台'
FROM sys_menu m
WHERE m.MenuCode = 'dashboard'
  AND NOT EXISTS (SELECT 1 FROM sys_permission WHERE PermissionCode = 'dashboard:view');

-- 用户管理
INSERT INTO sys_menu (ParentId, MenuCode, MenuName, Path, Icon, SortOrder, IsActive)
SELECT NULL, 'system.users', '用户管理', '/system/users', 'user', 2, TRUE
WHERE NOT EXISTS (SELECT 1 FROM sys_menu WHERE MenuCode = 'system.users');

INSERT INTO sys_permission (MenuId, PermissionCode, PermissionName, Description)
SELECT m.Id, v.code, v.name, v.description
FROM sys_menu m
CROSS JOIN (VALUES
    ('system.users:view', '查看', '查看用户列表'),
    ('system.users:create', '新建', '创建用户'),
    ('system.users:edit', '编辑', '编辑用户'),
    ('system.users:delete', '删除', '删除用户'),
    ('system.users:assign-roles', '分配角色', '为用户分配角色'),
    ('system.users:export', '导出', '导出用户列表 Excel')
) AS v(code, name, description)
WHERE m.MenuCode = 'system.users'
  AND NOT EXISTS (SELECT 1 FROM sys_permission p WHERE p.PermissionCode = v.code);

-- 角色管理
INSERT INTO sys_menu (ParentId, MenuCode, MenuName, Path, Icon, SortOrder, IsActive)
SELECT NULL, 'system.roles', '角色管理', '/system/roles', 'team', 3, TRUE
WHERE NOT EXISTS (SELECT 1 FROM sys_menu WHERE MenuCode = 'system.roles');

INSERT INTO sys_permission (MenuId, PermissionCode, PermissionName, Description)
SELECT m.Id, v.code, v.name, v.description
FROM sys_menu m
CROSS JOIN (VALUES
    ('system.roles:view', '查看', '查看角色列表'),
    ('system.roles:create', '新建', '创建角色'),
    ('system.roles:edit', '编辑', '编辑角色'),
    ('system.roles:delete', '删除', '删除角色'),
    ('system.roles:export', '导出', '导出角色列表 Excel')
) AS v(code, name, description)
WHERE m.MenuCode = 'system.roles'
  AND NOT EXISTS (SELECT 1 FROM sys_permission p WHERE p.PermissionCode = v.code);

-- 角色权限（独立菜单）
INSERT INTO sys_menu (ParentId, MenuCode, MenuName, Path, Icon, SortOrder, IsActive)
SELECT NULL, 'system.role-permissions', '角色权限', '/system/role-permissions', 'safety-certificate', 4, TRUE
WHERE NOT EXISTS (SELECT 1 FROM sys_menu WHERE MenuCode = 'system.role-permissions');

INSERT INTO sys_permission (MenuId, PermissionCode, PermissionName, Description)
SELECT m.Id, v.code, v.name, v.description
FROM sys_menu m
CROSS JOIN (VALUES
    ('system.role-permissions:view', '查看', '访问角色权限分配页'),
    ('system.role-permissions:assign', '分配', '为角色分配权限')
) AS v(code, name, description)
WHERE m.MenuCode = 'system.role-permissions'
  AND NOT EXISTS (SELECT 1 FROM sys_permission p WHERE p.PermissionCode = v.code);

-- 操作日志
INSERT INTO sys_menu (ParentId, MenuCode, MenuName, Path, Icon, SortOrder, IsActive)
SELECT NULL, 'system.logs', '操作日志', '/system/logs', 'file-text', 5, TRUE
WHERE NOT EXISTS (SELECT 1 FROM sys_menu WHERE MenuCode = 'system.logs');

INSERT INTO sys_permission (MenuId, PermissionCode, PermissionName, Description)
SELECT m.Id, 'system.logs:view', '查看', '查看操作日志'
FROM sys_menu m
WHERE m.MenuCode = 'system.logs'
  AND NOT EXISTS (SELECT 1 FROM sys_permission WHERE PermissionCode = 'system.logs:view');

INSERT INTO sys_permission (MenuId, PermissionCode, PermissionName, Description)
SELECT m.Id, 'system.logs:export', '导出', '导出操作日志 Excel'
FROM sys_menu m
WHERE m.MenuCode = 'system.logs'
  AND NOT EXISTS (SELECT 1 FROM sys_permission WHERE PermissionCode = 'system.logs:export');

-- 为 admin 角色分配全部权限
INSERT INTO sys_role_permission (RoleId, PermissionId)
SELECT r.Id, p.Id
FROM sys_role r
CROSS JOIN sys_permission p
WHERE r.RoleCode = 'admin'
  AND r.IsDeleted = FALSE
  AND NOT EXISTS (
      SELECT 1 FROM sys_role_permission rp
      WHERE rp.RoleId = r.Id AND rp.PermissionId = p.Id
  );

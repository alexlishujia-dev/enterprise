using EnterprisePlatform.Core.Entities;
using EnterprisePlatform.Repository.Repositories;

namespace EnterprisePlatform.Api.Infrastructure;

/// <summary>
/// 初始化默认管理员账号、角色、菜单与权限。
/// </summary>
public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<ISysUserRepository>();
        var roleRepository = scope.ServiceProvider.GetRequiredService<ISysRoleRepository>();
        var menuRepository = scope.ServiceProvider.GetRequiredService<ISysMenuRepository>();

        var adminRole = await roleRepository.GetByRoleCodeAsync("admin");
        if (adminRole is null)
        {
            adminRole = new SysRole
            {
                RoleCode = "admin",
                RoleName = "管理员",
                Description = "系统管理员，拥有全部权限",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            await roleRepository.InsertAsync(adminRole);
        }

        var adminUser = await userRepository.GetByUserNameAsync("admin");
        if (adminUser is null)
        {
            adminUser = new SysUser
            {
                UserName = "admin",
                PasswordHash = Utils.Security.HashHelper.Sha256("Admin@123:admin"),
                DisplayName = "系统管理员",
                Email = "admin@example.com",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            await userRepository.InsertAsync(adminUser);
        }

        var userRoles = await roleRepository.GetRolesByUserIdAsync(adminUser.Id);
        if (userRoles.Count == 0)
            await roleRepository.AssignRolesToUserAsync(adminUser.Id, [adminRole.Id]);

        await EnsureMenusAndPermissionsAsync(menuRepository, adminRole.Id);
    }

    private static async Task EnsureMenusAndPermissionsAsync(ISysMenuRepository menuRepository, long adminRoleId)
    {
        var menuSeeds = new[]
        {
            new MenuSeed(null, "dashboard", "工作台", "/dashboard", "dashboard", 1),
            new MenuSeed(null, "system.users", "用户管理", "/system/users", "user", 2),
            new MenuSeed(null, "system.roles", "角色管理", "/system/roles", "team", 3),
            new MenuSeed(null, "system.role-permissions", "角色权限", "/system/role-permissions", "safety-certificate", 4),
            new MenuSeed(null, "system.logs", "操作日志", "/system/logs", "file-text", 5)
        };

        var permissionSeeds = new Dictionary<string, (string Code, string Name, string? Description)[]>
        {
            ["dashboard"] =
            [
                ("dashboard:view", "查看", "访问工作台")
            ],
            ["system.users"] =
            [
                ("system.users:view", "查看", "查看用户列表"),
                ("system.users:create", "新建", "创建用户"),
                ("system.users:edit", "编辑", "编辑用户"),
                ("system.users:delete", "删除", "删除用户"),
                ("system.users:assign-roles", "分配角色", "为用户分配角色"),
                ("system.users:export", "导出", "导出用户列表 Excel")
            ],
            ["system.roles"] =
            [
                ("system.roles:view", "查看", "查看角色列表"),
                ("system.roles:create", "新建", "创建角色"),
                ("system.roles:edit", "编辑", "编辑角色"),
                ("system.roles:delete", "删除", "删除角色"),
                ("system.roles:export", "导出", "导出角色列表 Excel")
            ],
            ["system.role-permissions"] =
            [
                ("system.role-permissions:view", "查看", "访问角色权限分配页"),
                ("system.role-permissions:assign", "分配", "为角色分配权限")
            ],
            ["system.logs"] =
            [
                ("system.logs:view", "查看", "查看操作日志"),
                ("system.logs:export", "导出", "导出操作日志 Excel")
            ]
        };

        var existingMenus = await menuRepository.GetAllActiveAsync();
        var menuCodeToId = existingMenus.ToDictionary(m => m.MenuCode, m => m.Id);

        foreach (var menuSeed in menuSeeds)
        {
            if (menuCodeToId.ContainsKey(menuSeed.MenuCode))
                continue;

            var menuId = await menuRepository.InsertMenuAsync(
                menuSeed.ParentId,
                menuSeed.MenuCode,
                menuSeed.MenuName,
                menuSeed.Path,
                menuSeed.Icon,
                menuSeed.SortOrder);
            menuCodeToId[menuSeed.MenuCode] = menuId;
        }

        var existingPermissions = await menuRepository.GetAllPermissionsAsync();
        var existingPermCodes = existingPermissions.Select(p => p.PermissionCode).ToHashSet();

        foreach (var menuSeed in menuSeeds)
        {
            if (!permissionSeeds.TryGetValue(menuSeed.MenuCode, out var permissions))
                continue;

            if (!menuCodeToId.TryGetValue(menuSeed.MenuCode, out var menuId))
                continue;

            foreach (var (code, name, description) in permissions)
            {
                if (existingPermCodes.Contains(code))
                    continue;

                await menuRepository.InsertPermissionAsync(menuId, code, name, description);
                existingPermCodes.Add(code);
            }
        }

        var adminPermissionIds = (await menuRepository.GetPermissionIdsByRoleIdAsync(adminRoleId)).ToHashSet();
        var allPermissions = await menuRepository.GetAllPermissionsAsync();
        var missingForAdmin = allPermissions
            .Where(p => !adminPermissionIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToList();

        if (missingForAdmin.Count > 0)
        {
            var merged = adminPermissionIds.Concat(missingForAdmin);
            await menuRepository.AssignPermissionsToRoleAsync(adminRoleId, merged);
        }
    }

    private sealed record MenuSeed(
        long? ParentId,
        string MenuCode,
        string MenuName,
        string? Path,
        string? Icon,
        int SortOrder);
}

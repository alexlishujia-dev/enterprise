using EnterprisePlatform.Core.Dtos;
using EnterprisePlatform.Core.Entities;
using EnterprisePlatform.Repository.Repositories;
using EnterprisePlatform.Service.Abstractions;
using EnterprisePlatform.Utils.Logging;

namespace EnterprisePlatform.Service.Services;

public sealed class SysMenuService : ServiceBase, ISysMenuService
{
    private readonly ISysMenuRepository _menuRepository;

    public SysMenuService(ISysMenuRepository menuRepository, IFileLogWriter fileLogWriter)
        : base(fileLogWriter)
    {
        _menuRepository = menuRepository;
    }

    public Task<IReadOnlyList<SysMenuTreeDto>> GetMenuTreeAsync(CancellationToken cancellationToken = default)
        => ExecuteAsync(nameof(GetMenuTreeAsync), async () =>
        {
            var menus = await _menuRepository.GetAllActiveAsync(cancellationToken);
            var permissions = await _menuRepository.GetAllPermissionsAsync(cancellationToken);
            return BuildMenuTree(menus, permissions, parentId: null);
        });

    public Task<IReadOnlyList<SysMenuNavDto>> GetUserMenusAsync(long userId, CancellationToken cancellationToken = default)
        => ExecuteAsync(nameof(GetUserMenusAsync), async () =>
        {
            var menus = await _menuRepository.GetAccessibleMenusByUserIdAsync(userId, cancellationToken);
            return BuildNavTree(menus, parentId: null);
        });

    public Task<IReadOnlyList<string>> GetUserPermissionCodesAsync(long userId, CancellationToken cancellationToken = default)
        => ExecuteAsync(nameof(GetUserPermissionCodesAsync), () =>
            _menuRepository.GetPermissionCodesByUserIdAsync(userId, cancellationToken));

    internal static IReadOnlyList<SysMenuTreeDto> BuildMenuTree(
        IReadOnlyList<SysMenu> menus,
        IReadOnlyList<SysPermission> permissions,
        long? parentId)
    {
        var permissionLookup = permissions
            .GroupBy(p => p.MenuId)
            .ToDictionary(g => g.Key, g => g.Select(MapPermission).ToList());

        return menus
            .Where(m => m.ParentId == parentId)
            .OrderBy(m => m.SortOrder)
            .ThenBy(m => m.Id)
            .Select(m => new SysMenuTreeDto
            {
                Id = m.Id,
                ParentId = m.ParentId,
                MenuCode = m.MenuCode,
                MenuName = m.MenuName,
                Path = m.Path,
                Icon = m.Icon,
                SortOrder = m.SortOrder,
                IsActive = m.IsActive,
                Permissions = permissionLookup.TryGetValue(m.Id, out var perms) ? perms : [],
                Children = BuildMenuTree(menus, permissions, m.Id).ToList()
            })
            .ToList();
    }

    internal static IReadOnlyList<SysMenuNavDto> BuildNavTree(IReadOnlyList<SysMenu> menus, long? parentId)
    {
        return menus
            .Where(m => m.ParentId == parentId)
            .OrderBy(m => m.SortOrder)
            .ThenBy(m => m.Id)
            .Select(m => new SysMenuNavDto
            {
                Id = m.Id,
                MenuCode = m.MenuCode,
                MenuName = m.MenuName,
                Path = m.Path,
                Icon = m.Icon,
                SortOrder = m.SortOrder,
                Children = BuildNavTree(menus, m.Id).ToList()
            })
            .ToList();
    }

    private static SysPermissionDto MapPermission(SysPermission entity)
        => new()
        {
            Id = entity.Id,
            MenuId = entity.MenuId,
            PermissionCode = entity.PermissionCode,
            PermissionName = entity.PermissionName,
            Description = entity.Description
        };
}

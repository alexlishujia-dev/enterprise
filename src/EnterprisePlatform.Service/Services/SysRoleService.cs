using EnterprisePlatform.Core.Common;
using EnterprisePlatform.Core.Dtos;
using EnterprisePlatform.Core.Entities;
using EnterprisePlatform.Core.Enums;
using EnterprisePlatform.Core.Exceptions;
using EnterprisePlatform.Repository.Repositories;
using EnterprisePlatform.Service.Abstractions;
using EnterprisePlatform.Utils.Excel;
using EnterprisePlatform.Utils.Logging;

namespace EnterprisePlatform.Service.Services;

public sealed class SysRoleService : ServiceBase, ISysRoleService
{
    private readonly ISysRoleRepository _roleRepository;
    private readonly ISysUserRepository _userRepository;
    private readonly ISysMenuRepository _menuRepository;

    public SysRoleService(
        ISysRoleRepository roleRepository,
        ISysUserRepository userRepository,
        ISysMenuRepository menuRepository,
        IFileLogWriter fileLogWriter)
        : base(fileLogWriter)
    {
        _roleRepository = roleRepository;
        _userRepository = userRepository;
        _menuRepository = menuRepository;
    }

    public Task<SysRoleDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        => ExecuteAsync(nameof(GetByIdAsync), async () =>
        {
            var entity = await _roleRepository.GetByIdAsync(id, cancellationToken);
            return entity is null ? null : MapToDto(entity);
        });

    public Task<PagedResult<SysRoleDto>> GetPagedAsync(PageQuery query, CancellationToken cancellationToken = default)
        => ExecuteAsync(nameof(GetPagedAsync), async () =>
        {
            var page = await _roleRepository.GetPagedAsync(query, cancellationToken);
            return new PagedResult<SysRoleDto>
            {
                Items = page.Items.Select(MapToDto).ToList(),
                Total = page.Total,
                PageIndex = page.PageIndex,
                PageSize = page.PageSize
            };
        });

    public Task<long> CreateAsync(SysRoleCreateDto dto, CancellationToken cancellationToken = default)
        => ExecuteAsync(nameof(CreateAsync), async () =>
        {
            var existing = await _roleRepository.GetByRoleCodeAsync(dto.RoleCode, cancellationToken);
            if (existing is not null)
                throw new BusinessException("角色编码已存在", ApiStatusCode.Conflict);

            var entity = new SysRole
            {
                RoleCode = dto.RoleCode,
                RoleName = dto.RoleName,
                Description = dto.Description,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            return await _roleRepository.InsertAsync(entity, cancellationToken);
        });

    public Task UpdateAsync(long id, SysRoleUpdateDto dto, CancellationToken cancellationToken = default)
        => ExecuteAsync(nameof(UpdateAsync), async () =>
        {
            var entity = await _roleRepository.GetByIdAsync(id, cancellationToken);
            if (entity is null)
                throw new BusinessException("角色不存在", ApiStatusCode.NotFound);

            entity.RoleName = dto.RoleName;
            entity.Description = dto.Description;
            entity.IsActive = dto.IsActive;

            await _roleRepository.UpdateAsync(entity, cancellationToken);
        });

    public Task DeleteAsync(long id, CancellationToken cancellationToken = default)
        => ExecuteAsync(nameof(DeleteAsync), async () =>
        {
            var affected = await _roleRepository.SoftDeleteAsync(id, cancellationToken);
            if (affected == 0)
                throw new BusinessException("角色不存在", ApiStatusCode.NotFound);
        });

    public Task<IReadOnlyList<SysRoleDto>> GetRolesByUserIdAsync(long userId, CancellationToken cancellationToken = default)
        => ExecuteAsync(nameof(GetRolesByUserIdAsync), async () =>
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null)
                throw new BusinessException("用户不存在", ApiStatusCode.NotFound);

            var roles = await _roleRepository.GetRolesByUserIdAsync(userId, cancellationToken);
            return (IReadOnlyList<SysRoleDto>)roles.Select(MapToDto).ToList();
        });

    public Task AssignRolesToUserAsync(long userId, AssignUserRolesDto dto, CancellationToken cancellationToken = default)
        => ExecuteAsync(nameof(AssignRolesToUserAsync), async () =>
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null)
                throw new BusinessException("用户不存在", ApiStatusCode.NotFound);

            foreach (var roleId in dto.RoleIds.Distinct())
            {
                var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken);
                if (role is null)
                    throw new BusinessException($"角色 {roleId} 不存在", ApiStatusCode.NotFound);
            }

            await _roleRepository.AssignRolesToUserAsync(userId, dto.RoleIds, cancellationToken);
        });

    public Task<IReadOnlyList<long>> GetRolePermissionIdsAsync(long roleId, CancellationToken cancellationToken = default)
        => ExecuteAsync(nameof(GetRolePermissionIdsAsync), async () =>
        {
            var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken);
            if (role is null)
                throw new BusinessException("角色不存在", ApiStatusCode.NotFound);

            return await _menuRepository.GetPermissionIdsByRoleIdAsync(roleId, cancellationToken);
        });

    public Task AssignPermissionsToRoleAsync(long roleId, AssignRolePermissionsDto dto, CancellationToken cancellationToken = default)
        => ExecuteAsync(nameof(AssignPermissionsToRoleAsync), async () =>
        {
            var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken);
            if (role is null)
                throw new BusinessException("角色不存在", ApiStatusCode.NotFound);

            await _menuRepository.AssignPermissionsToRoleAsync(roleId, dto.PermissionIds, cancellationToken);
        });

    public Task<FileExportResult> ExportAsync(PageQuery query, CancellationToken cancellationToken = default)
        => ExecuteAsync(nameof(ExportAsync), async () =>
        {
            var page = await GetPagedAsync(PageQuery.CreateExport(query.Keyword), cancellationToken);
            var rows = page.Items.Select(role => new Dictionary<string, object?>
            {
                ["ID"] = role.Id,
                ["角色编码"] = role.RoleCode,
                ["角色名称"] = role.RoleName,
                ["描述"] = role.Description ?? string.Empty,
                ["状态"] = role.IsActive ? "启用" : "禁用",
                ["创建时间"] = ExcelExportHelper.FormatDateTime(role.CreatedAt)
            });

            return new FileExportResult
            {
                Content = ExcelExportHelper.CreateWorkbook(rows, "角色列表"),
                FileName = ExcelExportHelper.BuildFileName("角色列表")
            };
        });

    private static SysRoleDto MapToDto(SysRole entity)
        => new()
        {
            Id = entity.Id,
            RoleCode = entity.RoleCode,
            RoleName = entity.RoleName,
            Description = entity.Description,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt
        };
}

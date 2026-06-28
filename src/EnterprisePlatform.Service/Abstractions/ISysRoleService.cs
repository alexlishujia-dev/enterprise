using EnterprisePlatform.Core.Common;
using EnterprisePlatform.Core.Dtos;

namespace EnterprisePlatform.Service.Abstractions;

public interface ISysRoleService
{
    Task<SysRoleDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<PagedResult<SysRoleDto>> GetPagedAsync(PageQuery query, CancellationToken cancellationToken = default);

    Task<long> CreateAsync(SysRoleCreateDto dto, CancellationToken cancellationToken = default);

    Task UpdateAsync(long id, SysRoleUpdateDto dto, CancellationToken cancellationToken = default);

    Task DeleteAsync(long id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SysRoleDto>> GetRolesByUserIdAsync(long userId, CancellationToken cancellationToken = default);

    Task AssignRolesToUserAsync(long userId, AssignUserRolesDto dto, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<long>> GetRolePermissionIdsAsync(long roleId, CancellationToken cancellationToken = default);

    Task AssignPermissionsToRoleAsync(long roleId, AssignRolePermissionsDto dto, CancellationToken cancellationToken = default);

    Task<FileExportResult> ExportAsync(PageQuery query, CancellationToken cancellationToken = default);
}

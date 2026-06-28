using EnterprisePlatform.Core.Dtos;

namespace EnterprisePlatform.Service.Abstractions;

public interface ISysMenuService
{
    Task<IReadOnlyList<SysMenuTreeDto>> GetMenuTreeAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SysMenuNavDto>> GetUserMenusAsync(long userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetUserPermissionCodesAsync(long userId, CancellationToken cancellationToken = default);
}

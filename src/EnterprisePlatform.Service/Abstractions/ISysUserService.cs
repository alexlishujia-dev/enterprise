using EnterprisePlatform.Core.Common;
using EnterprisePlatform.Core.Dtos;

namespace EnterprisePlatform.Service.Abstractions;

public interface ISysUserService
{
    Task<SysUserDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<PagedResult<SysUserDto>> GetPagedAsync(PageQuery query, CancellationToken cancellationToken = default);

    Task<long> CreateAsync(SysUserCreateDto dto, CancellationToken cancellationToken = default);

    Task UpdateAsync(long id, SysUserUpdateDto dto, CancellationToken cancellationToken = default);

    Task DeleteAsync(long id, CancellationToken cancellationToken = default);

    Task<FileExportResult> ExportAsync(PageQuery query, CancellationToken cancellationToken = default);
}

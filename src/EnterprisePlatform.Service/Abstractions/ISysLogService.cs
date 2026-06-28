using EnterprisePlatform.Core.Common;
using EnterprisePlatform.Core.Dtos;
using EnterprisePlatform.Core.Entities;

namespace EnterprisePlatform.Service.Abstractions;

public interface ISysLogService
{
    Task<PagedResult<SysLogDto>> GetPagedAsync(SysLogQuery query, CancellationToken cancellationToken = default);

    Task WriteAsync(SysLog entity, CancellationToken cancellationToken = default);

    Task<FileExportResult> ExportAsync(SysLogQuery query, CancellationToken cancellationToken = default);
}

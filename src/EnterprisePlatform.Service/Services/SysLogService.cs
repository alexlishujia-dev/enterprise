using EnterprisePlatform.Core.Common;
using EnterprisePlatform.Core.Dtos;
using EnterprisePlatform.Core.Entities;
using EnterprisePlatform.Repository.Repositories;
using EnterprisePlatform.Service.Abstractions;
using EnterprisePlatform.Utils.Excel;
using EnterprisePlatform.Utils.Logging;

namespace EnterprisePlatform.Service.Services;

public sealed class SysLogService : ServiceBase, ISysLogService
{
    private readonly ISysLogRepository _logRepository;

    public SysLogService(ISysLogRepository logRepository, IFileLogWriter fileLogWriter)
        : base(fileLogWriter)
    {
        _logRepository = logRepository;
    }

    public Task<PagedResult<SysLogDto>> GetPagedAsync(SysLogQuery query, CancellationToken cancellationToken = default)
        => ExecuteAsync(nameof(GetPagedAsync), async () =>
        {
            var page = await _logRepository.GetPagedAsync(query, cancellationToken);
            return new PagedResult<SysLogDto>
            {
                Items = page.Items.Select(MapToDto).ToList(),
                Total = page.Total,
                PageIndex = page.PageIndex,
                PageSize = page.PageSize
            };
        });

    public Task WriteAsync(SysLog entity, CancellationToken cancellationToken = default)
        => ExecuteAsync(nameof(WriteAsync), () => _logRepository.InsertAsync(entity, cancellationToken));

    public Task<FileExportResult> ExportAsync(SysLogQuery query, CancellationToken cancellationToken = default)
        => ExecuteAsync(nameof(ExportAsync), async () =>
        {
            var exportQuery = new SysLogQuery
            {
                ForExport = true,
                PageIndex = 1,
                PageSize = 10000,
                Module = query.Module,
                UserId = query.UserId,
                StartDate = query.StartDate,
                EndDate = query.EndDate,
                Keyword = query.Keyword
            };

            var page = await GetPagedAsync(exportQuery, cancellationToken);
            var rows = page.Items.Select(log => new Dictionary<string, object?>
            {
                ["ID"] = log.Id,
                ["用户"] = log.UserName ?? string.Empty,
                ["模块"] = log.Module,
                ["方法"] = log.HttpMethod,
                ["路径"] = log.RequestPath,
                ["状态码"] = log.StatusCode,
                ["耗时(ms)"] = log.DurationMs,
                ["IP"] = log.IpAddress ?? string.Empty,
                ["时间"] = ExcelExportHelper.FormatDateTime(log.CreatedAt)
            });

            return new FileExportResult
            {
                Content = ExcelExportHelper.CreateWorkbook(rows, "操作日志"),
                FileName = ExcelExportHelper.BuildFileName("操作日志")
            };
        });

    private static SysLogDto MapToDto(SysLog entity)
        => new()
        {
            Id = entity.Id,
            UserId = entity.UserId,
            UserName = entity.UserName,
            Module = entity.Module,
            Action = entity.Action,
            HttpMethod = entity.HttpMethod,
            RequestPath = entity.RequestPath,
            IpAddress = entity.IpAddress,
            RequestBody = entity.RequestBody,
            StatusCode = entity.StatusCode,
            DurationMs = entity.DurationMs,
            CreatedAt = entity.CreatedAt
        };
}

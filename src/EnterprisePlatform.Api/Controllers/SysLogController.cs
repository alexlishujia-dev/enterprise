using EnterprisePlatform.Api.Filters;
using EnterprisePlatform.Core.Common;
using EnterprisePlatform.Core.Dtos;
using EnterprisePlatform.Service.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace EnterprisePlatform.Api.Controllers;

/// <summary>系统操作日志查询。</summary>
public sealed class SysLogController : AuthorizedApiControllerBase
{
    private readonly ISysLogService _logService;

    public SysLogController(ISysLogService logService)
    {
        _logService = logService;
    }

    /// <summary>分页查询操作日志。</summary>
    [HttpGet]
    [RequirePermission("system.logs:view")]
    [ProducesResponseType(typeof(ApiResult<PagedResult<SysLogDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResult<PagedResult<SysLogDto>>>> GetPaged([FromQuery] SysLogQuery query, CancellationToken cancellationToken)
    {
        var data = await _logService.GetPagedAsync(query, cancellationToken);
        return Ok(ApiResult<PagedResult<SysLogDto>>.Ok(data));
    }

    /// <summary>导出操作日志为 Excel。</summary>
    [HttpGet("export")]
    [RequirePermission("system.logs:export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Export([FromQuery] SysLogQuery query, CancellationToken cancellationToken)
    {
        var file = await _logService.ExportAsync(query, cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }
}

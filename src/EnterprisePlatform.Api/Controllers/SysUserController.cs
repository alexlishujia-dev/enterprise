using EnterprisePlatform.Api.Filters;
using EnterprisePlatform.Core.Common;
using EnterprisePlatform.Core.Dtos;
using EnterprisePlatform.Service.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace EnterprisePlatform.Api.Controllers;

/// <summary>系统用户管理（框架示例模块）。</summary>
public sealed class SysUserController : AuthorizedApiControllerBase
{
    private readonly ISysUserService _userService;

    public SysUserController(ISysUserService userService)
    {
        _userService = userService;
    }

    /// <summary>分页查询用户。</summary>
    [HttpGet]
    [RequirePermission("system.users:view")]
    [ProducesResponseType(typeof(ApiResult<PagedResult<SysUserDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResult<PagedResult<SysUserDto>>>> GetPaged([FromQuery] PageQuery query, CancellationToken cancellationToken)
    {
        var data = await _userService.GetPagedAsync(query, cancellationToken);
        return Ok(ApiResult<PagedResult<SysUserDto>>.Ok(data));
    }

    /// <summary>按 ID 获取用户。</summary>
    [HttpGet("{id:long}")]
    [RequirePermission("system.users:view")]
    [ProducesResponseType(typeof(ApiResult<SysUserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResult<SysUserDto>>> GetById(long id, CancellationToken cancellationToken)
    {
        var data = await _userService.GetByIdAsync(id, cancellationToken);
        return Ok(ApiResult<SysUserDto>.Ok(data));
    }

    /// <summary>创建用户。</summary>
    [HttpPost]
    [RequirePermission("system.users:create")]
    [ProducesResponseType(typeof(ApiResult<long>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResult<long>>> Create([FromBody] SysUserCreateDto dto, CancellationToken cancellationToken)
    {
        var id = await _userService.CreateAsync(dto, cancellationToken);
        return Ok(ApiResult<long>.Ok(id, "创建成功"));
    }

    /// <summary>更新用户。</summary>
    [HttpPut("{id:long}")]
    [RequirePermission("system.users:edit")]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResult>> Update(long id, [FromBody] SysUserUpdateDto dto, CancellationToken cancellationToken)
    {
        await _userService.UpdateAsync(id, dto, cancellationToken);
        return Ok(ApiResult.Ok("更新成功"));
    }

    /// <summary>删除用户（软删除）。</summary>
    [HttpDelete("{id:long}")]
    [RequirePermission("system.users:delete")]
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResult>> Delete(long id, CancellationToken cancellationToken)
    {
        await _userService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResult.Ok("删除成功"));
    }

    /// <summary>导出用户列表为 Excel。</summary>
    [HttpGet("export")]
    [RequirePermission("system.users:export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Export([FromQuery] PageQuery query, CancellationToken cancellationToken)
    {
        var file = await _userService.ExportAsync(query, cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }
}

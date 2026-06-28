using EnterprisePlatform.Api.Filters;
using EnterprisePlatform.Core.Common;
using EnterprisePlatform.Core.Dtos;
using EnterprisePlatform.Service.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace EnterprisePlatform.Api.Controllers;

/// <summary>系统菜单与权限查询。</summary>
public sealed class SysMenuController : AuthorizedApiControllerBase
{
    private readonly ISysMenuService _menuService;

    public SysMenuController(ISysMenuService menuService)
    {
        _menuService = menuService;
    }

    /// <summary>获取完整菜单树（含各菜单下的权限）。</summary>
    [HttpGet("tree")]
    [RequirePermission("system.role-permissions:view")]
    [ProducesResponseType(typeof(ApiResult<IReadOnlyList<SysMenuTreeDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResult<IReadOnlyList<SysMenuTreeDto>>>> GetTree(CancellationToken cancellationToken)
    {
        var data = await _menuService.GetMenuTreeAsync(cancellationToken);
        return Ok(ApiResult<IReadOnlyList<SysMenuTreeDto>>.Ok(data));
    }
}

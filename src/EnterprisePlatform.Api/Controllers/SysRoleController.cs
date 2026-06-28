using EnterprisePlatform.Api.Filters;

using EnterprisePlatform.Core.Common;

using EnterprisePlatform.Core.Dtos;

using EnterprisePlatform.Service.Abstractions;

using Microsoft.AspNetCore.Mvc;



namespace EnterprisePlatform.Api.Controllers;



/// <summary>系统角色管理。</summary>

public sealed class SysRoleController : AuthorizedApiControllerBase

{

    private readonly ISysRoleService _roleService;



    public SysRoleController(ISysRoleService roleService)

    {

        _roleService = roleService;

    }



    /// <summary>分页查询角色。</summary>

    [HttpGet]

    [RequirePermission("system.roles:view", "system.role-permissions:view", "system.users:assign-roles")]

    [ProducesResponseType(typeof(ApiResult<PagedResult<SysRoleDto>>), StatusCodes.Status200OK)]

    public async Task<ActionResult<ApiResult<PagedResult<SysRoleDto>>>> GetPaged([FromQuery] PageQuery query, CancellationToken cancellationToken)

    {

        var data = await _roleService.GetPagedAsync(query, cancellationToken);

        return Ok(ApiResult<PagedResult<SysRoleDto>>.Ok(data));

    }



    /// <summary>按 ID 获取角色。</summary>

    [HttpGet("{id:long}")]

    [RequirePermission("system.roles:view", "system.role-permissions:view", "system.users:assign-roles")]

    [ProducesResponseType(typeof(ApiResult<SysRoleDto>), StatusCodes.Status200OK)]

    public async Task<ActionResult<ApiResult<SysRoleDto>>> GetById(long id, CancellationToken cancellationToken)

    {

        var data = await _roleService.GetByIdAsync(id, cancellationToken);

        return Ok(ApiResult<SysRoleDto>.Ok(data));

    }



    /// <summary>创建角色。</summary>

    [HttpPost]

    [RequirePermission("system.roles:create")]

    [ProducesResponseType(typeof(ApiResult<long>), StatusCodes.Status200OK)]

    public async Task<ActionResult<ApiResult<long>>> Create([FromBody] SysRoleCreateDto dto, CancellationToken cancellationToken)

    {

        var id = await _roleService.CreateAsync(dto, cancellationToken);

        return Ok(ApiResult<long>.Ok(id, "创建成功"));

    }



    /// <summary>更新角色。</summary>

    [HttpPut("{id:long}")]

    [RequirePermission("system.roles:edit")]

    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status200OK)]

    public async Task<ActionResult<ApiResult>> Update(long id, [FromBody] SysRoleUpdateDto dto, CancellationToken cancellationToken)

    {

        await _roleService.UpdateAsync(id, dto, cancellationToken);

        return Ok(ApiResult.Ok("更新成功"));

    }



    /// <summary>删除角色（软删除）。</summary>

    [HttpDelete("{id:long}")]

    [RequirePermission("system.roles:delete")]

    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status200OK)]

    public async Task<ActionResult<ApiResult>> Delete(long id, CancellationToken cancellationToken)

    {

        await _roleService.DeleteAsync(id, cancellationToken);

        return Ok(ApiResult.Ok("删除成功"));

    }



    /// <summary>获取用户已分配的角色。</summary>

    [HttpGet("user/{userId:long}")]

    [RequirePermission("system.users:assign-roles")]

    [ProducesResponseType(typeof(ApiResult<IReadOnlyList<SysRoleDto>>), StatusCodes.Status200OK)]

    public async Task<ActionResult<ApiResult<IReadOnlyList<SysRoleDto>>>> GetUserRoles(long userId, CancellationToken cancellationToken)

    {

        var data = await _roleService.GetRolesByUserIdAsync(userId, cancellationToken);

        return Ok(ApiResult<IReadOnlyList<SysRoleDto>>.Ok(data));

    }



    /// <summary>为用户分配角色（覆盖式）。</summary>

    [HttpPut("user/{userId:long}")]

    [RequirePermission("system.users:assign-roles")]

    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status200OK)]

    public async Task<ActionResult<ApiResult>> AssignUserRoles(long userId, [FromBody] AssignUserRolesDto dto, CancellationToken cancellationToken)

    {

        await _roleService.AssignRolesToUserAsync(userId, dto, cancellationToken);

        return Ok(ApiResult.Ok("分配成功"));

    }



    /// <summary>获取角色已分配的权限 ID 列表。</summary>

    [HttpGet("{roleId:long}/permissions")]

    [RequirePermission("system.role-permissions:view")]

    [ProducesResponseType(typeof(ApiResult<IReadOnlyList<long>>), StatusCodes.Status200OK)]

    public async Task<ActionResult<ApiResult<IReadOnlyList<long>>>> GetRolePermissions(long roleId, CancellationToken cancellationToken)

    {

        var data = await _roleService.GetRolePermissionIdsAsync(roleId, cancellationToken);

        return Ok(ApiResult<IReadOnlyList<long>>.Ok(data));

    }



    /// <summary>为角色分配权限（覆盖式）。</summary>

    [HttpPut("{roleId:long}/permissions")]

    [RequirePermission("system.role-permissions:assign")]

    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status200OK)]

    public async Task<ActionResult<ApiResult>> AssignRolePermissions(long roleId, [FromBody] AssignRolePermissionsDto dto, CancellationToken cancellationToken)

    {

        await _roleService.AssignPermissionsToRoleAsync(roleId, dto, cancellationToken);

        return Ok(ApiResult.Ok("分配成功"));

    }

    /// <summary>导出角色列表为 Excel。</summary>
    [HttpGet("export")]
    [RequirePermission("system.roles:export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Export([FromQuery] PageQuery query, CancellationToken cancellationToken)
    {
        var file = await _roleService.ExportAsync(query, cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

}



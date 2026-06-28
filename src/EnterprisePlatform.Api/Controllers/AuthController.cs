using EnterprisePlatform.Api.Filters;
using EnterprisePlatform.Core.Common;
using EnterprisePlatform.Core.Dtos;
using EnterprisePlatform.Core.Enums;
using EnterprisePlatform.Service.Abstractions;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EnterprisePlatform.Api.Controllers;

public sealed class AuthController : ApiControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>用户登录，返回 JWT Token。</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResult<LoginResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResult<LoginResponseDto>>> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        var data = await _authService.LoginAsync(request, cancellationToken);
        return Ok(ApiResult<LoginResponseDto>.Ok(data));
    }

    /// <summary>获取当前登录用户信息（含权限与菜单）。</summary>
    [HttpGet("me")]
    [TokenAuthorize]
    [ProducesResponseType(typeof(ApiResult<SysUserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResult<SysUserDto>>> GetMe(CancellationToken cancellationToken)
    {
        var userId = GetUserId(User);
        if (userId is null)
            return Unauthorized(ApiResult.Fail(ApiStatusCode.Unauthorized, "未授权或 Token 无效"));

        var data = await _authService.GetCurrentUserAsync(userId.Value, cancellationToken);
        return Ok(ApiResult<SysUserDto>.Ok(data));
    }

    private static long? GetUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue("sub")
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

        return long.TryParse(sub, out var id) ? id : null;
    }
}
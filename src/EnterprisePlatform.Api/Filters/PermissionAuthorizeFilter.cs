using EnterprisePlatform.Core.Common;
using EnterprisePlatform.Core.Enums;
using EnterprisePlatform.Service.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
using System.Text.Json;

namespace EnterprisePlatform.Api.Filters;

/// <summary>
/// 在 Token 校验完成后，根据 <see cref="RequirePermissionAttribute"/> 校验权限。
/// 使用 Action 过滤器而非 Authorization 过滤器，避免与 TokenAuthorizeFilter 的执行顺序冲突。
/// </summary>
public sealed class PermissionAuthorizeFilter : IAsyncActionFilter, IOrderedFilter
{
    private const string PermissionCacheKey = "__user_permission_codes__";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ISysMenuService _menuService;

    public PermissionAuthorizeFilter(ISysMenuService menuService)
    {
        _menuService = menuService;
    }

    public int Order => int.MaxValue - 100;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (HasAllowAnonymous(context) || context.Result is not null)
        {
            await next();
            return;
        }

        var requiredPermissions = GetRequiredPermissions(context);
        if (requiredPermissions.Count == 0)
        {
            await next();
            return;
        }

        var userId = GetUserId(context.HttpContext.User);
        if (userId is null)
        {
            context.Result = Unauthorized(context, "未授权或 Token 无效");
            return;
        }

        var userPermissions = await GetUserPermissionsAsync(context.HttpContext, userId.Value);
        if (!requiredPermissions.Any(userPermissions.Contains))
        {
            context.Result = Forbidden(context, "无权访问该接口");
            return;
        }

        await next();
    }

    private async Task<HashSet<string>> GetUserPermissionsAsync(HttpContext httpContext, long userId)
    {
        if (httpContext.Items.TryGetValue(PermissionCacheKey, out var cached)
            && cached is HashSet<string> codes)
        {
            return codes;
        }

        var permissions = await _menuService.GetUserPermissionCodesAsync(userId, httpContext.RequestAborted);
        var set = permissions.ToHashSet(StringComparer.OrdinalIgnoreCase);
        httpContext.Items[PermissionCacheKey] = set;
        return set;
    }

    private static List<string> GetRequiredPermissions(FilterContext context)
    {
        return context.ActionDescriptor.EndpointMetadata
            .OfType<RequirePermissionAttribute>()
            .SelectMany(attr => attr.Permissions)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool HasAllowAnonymous(FilterContext context)
        => context.ActionDescriptor.EndpointMetadata.Any(metadata => metadata is IAllowAnonymous);

    private static long? GetUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue("sub")
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

        return long.TryParse(sub, out var id) ? id : null;
    }

    private static IActionResult Unauthorized(FilterContext context, string message)
    {
        var result = ApiResult.Fail(ApiStatusCode.Unauthorized, message);
        result.TraceId = context.HttpContext.TraceIdentifier;
        return new JsonResult(result, JsonOptions)
        {
            StatusCode = StatusCodes.Status401Unauthorized
        };
    }

    private static IActionResult Forbidden(FilterContext context, string message)
    {
        var result = ApiResult.Fail(ApiStatusCode.Forbidden, message);
        result.TraceId = context.HttpContext.TraceIdentifier;
        return new JsonResult(result, JsonOptions)
        {
            StatusCode = StatusCodes.Status403Forbidden
        };
    }
}

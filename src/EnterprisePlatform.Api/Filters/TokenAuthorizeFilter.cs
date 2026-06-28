using EnterprisePlatform.Core.Common;
using EnterprisePlatform.Core.Enums;
using EnterprisePlatform.Utils.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
using System.Text.Json;

namespace EnterprisePlatform.Api.Filters;

/// <summary>自定义 Token 校验过滤器（替代 [Authorize] + JwtBearer）。</summary>
public sealed class TokenAuthorizeFilter : IAsyncAuthorizationFilter, IOrderedFilter
{
    public int Order => 0;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ITokenValidator _tokenValidator;

    public TokenAuthorizeFilter(ITokenValidator tokenValidator)
    {
        _tokenValidator = tokenValidator;
    }

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (HasAllowAnonymous(context))
            return Task.CompletedTask;

        var authorizationHeader = context.HttpContext.Request.Headers.Authorization.FirstOrDefault();
        var validationResult = _tokenValidator.Validate(authorizationHeader);
        if (validationResult is null)
        {
            var result = ApiResult.Fail(ApiStatusCode.Unauthorized, "未授权或 Token 无效");
            result.TraceId = context.HttpContext.TraceIdentifier;
            context.Result = new JsonResult(result, JsonOptions)
            {
                StatusCode = StatusCodes.Status401Unauthorized
            };
            return Task.CompletedTask;
        }

        context.HttpContext.User = BuildPrincipal(validationResult);
        return Task.CompletedTask;
    }

    private static bool HasAllowAnonymous(AuthorizationFilterContext context)
        => context.ActionDescriptor.EndpointMetadata.Any(metadata => metadata is IAllowAnonymous);

    private static ClaimsPrincipal BuildPrincipal(TokenValidationResult validationResult)
    {
        var claims = new List<Claim>
        {
            new("sub", validationResult.UserId.ToString()),
            new(ClaimTypes.NameIdentifier, validationResult.UserId.ToString()),
            new(ClaimTypes.Name, validationResult.UserName),
            new("unique_name", validationResult.UserName)
        };

        foreach (var role in validationResult.Roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Token"));
    }
}

/// <summary>标记需 Token 认证的控制器或 Action。</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class TokenAuthorizeAttribute : TypeFilterAttribute
{
    public TokenAuthorizeAttribute() : base(typeof(TokenAuthorizeFilter))
    {
        Order = 0;
    }
}

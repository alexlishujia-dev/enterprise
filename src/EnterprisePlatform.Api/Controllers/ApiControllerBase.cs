using EnterprisePlatform.Api.Filters;
using Microsoft.AspNetCore.Mvc;

namespace EnterprisePlatform.Api.Controllers;

/// <summary>API 控制器基类（路由与 ApiController 行为）。</summary>
[ApiController]
[Route("api/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
}

/// <summary>需 Token 认证的 API 控制器基类（自定义校验逻辑）。</summary>
[TokenAuthorize]
public abstract class AuthorizedApiControllerBase : ApiControllerBase
{
}
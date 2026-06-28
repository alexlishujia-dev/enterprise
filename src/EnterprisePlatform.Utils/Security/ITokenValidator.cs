namespace EnterprisePlatform.Utils.Security;

/// <summary>Token 校验（自定义实现，不依赖 ASP.NET 认证中间件）。</summary>
public interface ITokenValidator
{
    /// <summary>
    /// 校验 Authorization 头中的 Bearer Token。
    /// </summary>
    /// <returns>校验成功返回用户信息；失败返回 null。</returns>
    TokenValidationResult? Validate(string? authorizationHeader);
}

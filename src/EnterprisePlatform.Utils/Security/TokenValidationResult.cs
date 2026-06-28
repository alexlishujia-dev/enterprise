namespace EnterprisePlatform.Utils.Security;

/// <summary>Token 校验成功后的用户信息。</summary>
public sealed class TokenValidationResult
{
    public long UserId { get; init; }

    public string UserName { get; init; } = string.Empty;

    public IReadOnlyList<string> Roles { get; init; } = [];
}

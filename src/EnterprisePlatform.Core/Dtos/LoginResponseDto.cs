namespace EnterprisePlatform.Core.Dtos;

/// <summary>
/// 登录响应。
/// </summary>
public class LoginResponseDto
{
    public string AccessToken { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public SysUserDto User { get; set; } = new();
}

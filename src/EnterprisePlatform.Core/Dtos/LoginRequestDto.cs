using System.ComponentModel.DataAnnotations;

namespace EnterprisePlatform.Core.Dtos;

/// <summary>
/// 登录请求。
/// </summary>
public class LoginRequestDto
{
    [Required, MaxLength(64)]
    public string UserName { get; set; } = string.Empty;

    [Required, MaxLength(128)]
    public string Password { get; set; } = string.Empty;
}

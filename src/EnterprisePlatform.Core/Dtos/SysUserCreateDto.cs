using System.ComponentModel.DataAnnotations;

namespace EnterprisePlatform.Core.Dtos;

/// <summary>
/// 创建系统用户请求。
/// </summary>
public class SysUserCreateDto
{
    [Required, MaxLength(64)]
    public string UserName { get; set; } = string.Empty;

    [Required, MinLength(6), MaxLength(128)]
    public string Password { get; set; } = string.Empty;

    [MaxLength(64)]
    public string? DisplayName { get; set; }

    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    [MaxLength(128)]
    public string? Email { get; set; }

    [MaxLength(512)]
    public string? AvatarUrl { get; set; }
}

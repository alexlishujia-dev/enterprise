using System.ComponentModel.DataAnnotations;

namespace EnterprisePlatform.Core.Dtos;

/// <summary>
/// 更新系统用户请求。
/// </summary>
public class SysUserUpdateDto
{
    [MaxLength(64)]
    public string? DisplayName { get; set; }

    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    [MaxLength(128)]
    public string? Email { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>新密码；为空则不修改。</summary>
    [MinLength(6, ErrorMessage = "密码至少 6 位")]
    [MaxLength(128)]
    public string? Password { get; set; }

    [MaxLength(512)]
    public string? AvatarUrl { get; set; }
}

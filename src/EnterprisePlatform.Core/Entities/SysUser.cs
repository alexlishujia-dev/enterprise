namespace EnterprisePlatform.Core.Entities;

/// <summary>
/// 系统用户实体（框架示例，非业务绑定）。
/// </summary>
public class SysUser : BaseEntity
{
    public string UserName { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public string? Email { get; set; }

    public string? AvatarUrl { get; set; }

    public bool IsActive { get; set; } = true;
}

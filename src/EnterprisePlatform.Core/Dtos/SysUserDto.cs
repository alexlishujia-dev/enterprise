namespace EnterprisePlatform.Core.Dtos;

/// <summary>
/// 系统用户输出 DTO。
/// </summary>
public class SysUserDto
{
    public long Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public string? Email { get; set; }

    public string? AvatarUrl { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public List<string> Roles { get; set; } = [];

    public List<string> Permissions { get; set; } = [];

    public List<SysMenuNavDto> Menus { get; set; } = [];
}

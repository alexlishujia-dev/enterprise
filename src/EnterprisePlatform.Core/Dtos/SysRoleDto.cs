namespace EnterprisePlatform.Core.Dtos;

/// <summary>
/// 系统角色输出 DTO。
/// </summary>
public class SysRoleDto
{
    public long Id { get; set; }

    public string RoleCode { get; set; } = string.Empty;

    public string RoleName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}

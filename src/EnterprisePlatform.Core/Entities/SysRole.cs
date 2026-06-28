namespace EnterprisePlatform.Core.Entities;

/// <summary>
/// 系统角色实体。
/// </summary>
public class SysRole : BaseEntity
{
    public string RoleCode { get; set; } = string.Empty;

    public string RoleName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

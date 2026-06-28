using System.ComponentModel.DataAnnotations;

namespace EnterprisePlatform.Core.Dtos;

/// <summary>
/// 更新系统角色请求。
/// </summary>
public class SysRoleUpdateDto
{
    [Required, MaxLength(64)]
    public string RoleName { get; set; } = string.Empty;

    [MaxLength(256)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

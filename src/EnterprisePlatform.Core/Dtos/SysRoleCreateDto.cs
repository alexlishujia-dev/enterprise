using System.ComponentModel.DataAnnotations;

namespace EnterprisePlatform.Core.Dtos;

/// <summary>
/// 创建系统角色请求。
/// </summary>
public class SysRoleCreateDto
{
    [Required, MaxLength(64)]
    public string RoleCode { get; set; } = string.Empty;

    [Required, MaxLength(64)]
    public string RoleName { get; set; } = string.Empty;

    [MaxLength(256)]
    public string? Description { get; set; }
}

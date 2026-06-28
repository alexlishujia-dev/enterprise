using System.ComponentModel.DataAnnotations;

namespace EnterprisePlatform.Core.Dtos;

/// <summary>
/// 为角色分配权限请求。
/// </summary>
public class AssignRolePermissionsDto
{
    [Required]
    public List<long> PermissionIds { get; set; } = [];
}

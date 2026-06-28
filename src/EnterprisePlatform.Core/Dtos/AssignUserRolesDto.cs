using System.ComponentModel.DataAnnotations;

namespace EnterprisePlatform.Core.Dtos;

/// <summary>
/// 为用户分配角色请求。
/// </summary>
public class AssignUserRolesDto
{
    [Required]
    public List<long> RoleIds { get; set; } = [];
}

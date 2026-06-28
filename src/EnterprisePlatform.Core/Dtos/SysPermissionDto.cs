namespace EnterprisePlatform.Core.Dtos;

/// <summary>
/// 系统权限输出 DTO。
/// </summary>
public class SysPermissionDto
{
    public long Id { get; set; }

    public long MenuId { get; set; }

    public string PermissionCode { get; set; } = string.Empty;

    public string PermissionName { get; set; } = string.Empty;

    public string? Description { get; set; }
}

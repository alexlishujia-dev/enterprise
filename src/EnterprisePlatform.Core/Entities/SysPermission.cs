namespace EnterprisePlatform.Core.Entities;

/// <summary>
/// 系统权限（功能点）实体。
/// </summary>
public class SysPermission
{
    public long Id { get; set; }

    public long MenuId { get; set; }

    public string PermissionCode { get; set; } = string.Empty;

    public string PermissionName { get; set; } = string.Empty;

    public string? Description { get; set; }
}

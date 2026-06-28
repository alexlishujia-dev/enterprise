namespace EnterprisePlatform.Core.Dtos;

/// <summary>
/// 菜单树节点（含权限列表）。
/// </summary>
public class SysMenuTreeDto
{
    public long Id { get; set; }

    public long? ParentId { get; set; }

    public string MenuCode { get; set; } = string.Empty;

    public string MenuName { get; set; } = string.Empty;

    public string? Path { get; set; }

    public string? Icon { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; }

    public List<SysPermissionDto> Permissions { get; set; } = [];

    public List<SysMenuTreeDto> Children { get; set; } = [];
}

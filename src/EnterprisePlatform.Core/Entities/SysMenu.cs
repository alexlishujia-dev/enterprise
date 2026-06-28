namespace EnterprisePlatform.Core.Entities;

/// <summary>
/// 系统菜单实体。
/// </summary>
public class SysMenu
{
    public long Id { get; set; }

    public long? ParentId { get; set; }

    public string MenuCode { get; set; } = string.Empty;

    public string MenuName { get; set; } = string.Empty;

    public string? Path { get; set; }

    public string? Icon { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

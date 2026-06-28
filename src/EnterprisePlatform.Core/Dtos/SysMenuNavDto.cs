namespace EnterprisePlatform.Core.Dtos;

/// <summary>
/// 用户可访问的导航菜单。
/// </summary>
public class SysMenuNavDto
{
    public long Id { get; set; }

    public string MenuCode { get; set; } = string.Empty;

    public string MenuName { get; set; } = string.Empty;

    public string? Path { get; set; }

    public string? Icon { get; set; }

    public int SortOrder { get; set; }

    public List<SysMenuNavDto> Children { get; set; } = [];
}

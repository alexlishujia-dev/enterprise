namespace EnterprisePlatform.Core.Common;

/// <summary>
/// 分页查询参数。
/// </summary>
public class PageQuery
{
    private const int DefaultPageSize = 20;
    private const int MaxListPageSize = 200;
    private const int MaxExportPageSize = 10000;

    private int _pageIndex = 1;
    private int _pageSize = DefaultPageSize;

    /// <summary>页码，从 1 开始。</summary>
    public int PageIndex
    {
        get => _pageIndex;
        set => _pageIndex = value < 1 ? 1 : value;
    }

    /// <summary>每页条数；列表最大 200，导出最大 10000。</summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value switch
        {
            < 1 => ForExport ? MaxExportPageSize : DefaultPageSize,
            _ when ForExport => Math.Min(value, MaxExportPageSize),
            > MaxListPageSize => MaxListPageSize,
            _ => value
        };
    }

    /// <summary>关键字搜索。</summary>
    public string? Keyword { get; set; }

    /// <summary>是否为导出场景（允许更大的 PageSize）。</summary>
    public bool ForExport { get; set; }

    /// <summary>创建导出查询（按当前筛选条件，最多 10000 条）。</summary>
    public static PageQuery CreateExport(string? keyword = null)
        => new() { ForExport = true, PageIndex = 1, PageSize = MaxExportPageSize, Keyword = keyword };
}

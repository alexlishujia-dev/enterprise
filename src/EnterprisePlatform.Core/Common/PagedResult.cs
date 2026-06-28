namespace EnterprisePlatform.Core.Common;

/// <summary>
/// 分页查询结果。
/// </summary>
/// <typeparam name="T">列表项类型。</typeparam>
public class PagedResult<T>
{
    /// <summary>当前页数据。</summary>
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();

    /// <summary>总记录数。</summary>
    public long Total { get; set; }

    /// <summary>当前页码，从 1 开始。</summary>
    public int PageIndex { get; set; }

    /// <summary>每页条数。</summary>
    public int PageSize { get; set; }

    /// <summary>总页数。</summary>
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(Total / (double)PageSize);
}

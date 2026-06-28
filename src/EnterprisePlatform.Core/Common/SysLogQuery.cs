namespace EnterprisePlatform.Core.Common;

/// <summary>
/// 系统日志查询参数。
/// </summary>
public class SysLogQuery : PageQuery
{
    public long? UserId { get; set; }

    public string? Module { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }
}

namespace EnterprisePlatform.Core.Entities;

/// <summary>
/// 系统操作日志实体。
/// </summary>
public class SysLog : BaseEntity
{
    public long? UserId { get; set; }

    public string? UserName { get; set; }

    public string Module { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string HttpMethod { get; set; } = string.Empty;

    public string RequestPath { get; set; } = string.Empty;

    public string? IpAddress { get; set; }

    public string? RequestBody { get; set; }

    public int StatusCode { get; set; }

    public long DurationMs { get; set; }
}

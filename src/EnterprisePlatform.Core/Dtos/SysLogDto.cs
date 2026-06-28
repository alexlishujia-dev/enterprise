namespace EnterprisePlatform.Core.Dtos;

/// <summary>
/// 系统日志输出 DTO。
/// </summary>
public class SysLogDto
{
    public long Id { get; set; }

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

    public DateTime CreatedAt { get; set; }
}

namespace EnterprisePlatform.Core.Options;

/// <summary>
/// API 请求/响应数据验签配置。
/// </summary>
public class RequestSignOptions
{
    public const string SectionName = "RequestSign";

    /// <summary>是否启用验签。</summary>
    public bool Enabled { get; set; }

    /// <summary>HMAC 签名密钥（客户端与服务端共享）。</summary>
    public string SecretKey { get; set; } = "EnterprisePlatform-Request-Sign-Key-Change-Me!";

    /// <summary>时间戳允许偏差（秒），防重放。</summary>
    public int TimestampToleranceSeconds { get; set; } = 300;

    /// <summary>是否对响应体签名，供客户端校验。</summary>
    public bool SignResponse { get; set; } = true;

    /// <summary>跳过验签的路径前缀。</summary>
    public string[] SkipPaths { get; set; } = ["/health", "/swagger"];
}

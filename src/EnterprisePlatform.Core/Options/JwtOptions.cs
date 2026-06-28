namespace EnterprisePlatform.Core.Options;

/// <summary>
/// JWT 认证配置。
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "EnterprisePlatform";

    public string Audience { get; set; } = "EnterprisePlatform.Client";

    public string SecretKey { get; set; } = "EnterprisePlatform-Dev-Secret-Key-Change-In-Production";

    public int ExpireMinutes { get; set; } = 120;
}

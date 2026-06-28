using EnterprisePlatform.Core.Options;
using EnterprisePlatform.Utils.Security;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace EnterprisePlatform.Api.Infrastructure;

public sealed class RequestSignatureVerifier : IRequestSignatureVerifier
{
    private readonly RequestSignOptions _options;
    private readonly IMemoryCache _nonceCache;

    public RequestSignatureVerifier(IOptions<RequestSignOptions> options, IMemoryCache nonceCache)
    {
        _options = options.Value;
        _nonceCache = nonceCache;
    }

    public string? VerifyRequest(
        string method,
        string path,
        string queryString,
        string? timestamp,
        string? nonce,
        string? signature,
        string? body)
    {
        if (string.IsNullOrWhiteSpace(timestamp))
            return "缺少签名请求头 X-Platform-Timestamp";

        if (string.IsNullOrWhiteSpace(nonce))
            return "缺少签名请求头 X-Platform-Nonce";

        if (string.IsNullOrWhiteSpace(signature))
            return "缺少签名请求头 X-Platform-Signature";

        if (!long.TryParse(timestamp, out var timestampValue))
            return "X-Platform-Timestamp 格式无效";

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (Math.Abs(now - timestampValue) > _options.TimestampToleranceSeconds)
            return "请求已过期，请检查客户端时间";

        var nonceKey = $"request-sign:nonce:{nonce}";
        if (_nonceCache.TryGetValue(nonceKey, out _))
            return "重复请求（Nonce 已使用）";

        var payload = RequestSignHelper.BuildRequestPayload(
            method, path, queryString, timestamp, nonce, body);

        if (!RequestSignHelper.Verify(payload, _options.SecretKey, signature))
            return "请求签名校验失败，数据可能已被篡改";

        _nonceCache.Set(
            nonceKey,
            true,
            TimeSpan.FromSeconds(_options.TimestampToleranceSeconds));

        return null;
    }
}

using System.Security.Cryptography;
using System.Text;

namespace EnterprisePlatform.Utils.Security;

/// <summary>
/// 请求/响应 HMAC-SHA256 签名工具。
/// </summary>
public static class RequestSignHelper
{
    public const string TimestampHeader = "X-Platform-Timestamp";
    public const string NonceHeader = "X-Platform-Nonce";
    public const string SignatureHeader = "X-Platform-Signature";
    public const string ResponseTimestampHeader = "X-Platform-Response-Timestamp";
    public const string ResponseSignatureHeader = "X-Platform-Response-Signature";

    public static string BuildRequestPayload(
        string method,
        string path,
        string queryString,
        string timestamp,
        string nonce,
        string? body)
    {
        var bodyHash = HashHelper.Sha256(body ?? string.Empty);
        return string.Join('\n',
        [
            method.ToUpperInvariant(),
            path,
            queryString,
            timestamp,
            nonce,
            bodyHash
        ]);
    }

    public static string BuildResponsePayload(int statusCode, string timestamp, string? body)
    {
        var bodyHash = HashHelper.Sha256(body ?? string.Empty);
        return string.Join('\n',
        [
            statusCode.ToString(),
            timestamp,
            bodyHash
        ]);
    }

    public static string Sign(string payload, string secretKey)
    {
        var key = Encoding.UTF8.GetBytes(secretKey);
        var hash = HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static bool Verify(string payload, string secretKey, string signature)
    {
        if (string.IsNullOrWhiteSpace(signature))
            return false;

        try
        {
            var expected = Sign(payload, secretKey);
            var expectedBytes = Convert.FromHexString(expected);
            var actualBytes = Convert.FromHexString(signature.Trim().ToLowerInvariant());
            return expectedBytes.Length == actualBytes.Length &&
                   CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

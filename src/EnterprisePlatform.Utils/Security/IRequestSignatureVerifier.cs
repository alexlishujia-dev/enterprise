namespace EnterprisePlatform.Utils.Security;

/// <summary>请求数据验签。</summary>
public interface IRequestSignatureVerifier
{
    /// <summary>
    /// 校验请求签名。
    /// </summary>
    /// <returns>失败时返回错误信息；成功返回 null。</returns>
    string? VerifyRequest(
        string method,
        string path,
        string queryString,
        string? timestamp,
        string? nonce,
        string? signature,
        string? body);
}

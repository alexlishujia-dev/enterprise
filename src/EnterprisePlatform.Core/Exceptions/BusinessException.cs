namespace EnterprisePlatform.Core.Exceptions;

/// <summary>
/// 业务异常，由全局中间件统一转换为 ApiResult。
/// </summary>
public class BusinessException : Exception
{
    public int Code { get; }

    public BusinessException(string message, int code = 400)
        : base(message)
    {
        Code = code;
    }
}

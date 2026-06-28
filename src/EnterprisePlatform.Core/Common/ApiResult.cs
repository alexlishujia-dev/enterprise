namespace EnterprisePlatform.Core.Common;

/// <summary>
/// 统一 API 响应包装。
/// </summary>
/// <typeparam name="T">业务数据类型。</typeparam>
public class ApiResult<T>
{
    /// <summary>状态码，200 表示成功。</summary>
    public int Code { get; set; }

    /// <summary>提示信息。</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>业务数据。</summary>
    public T? Data { get; set; }

    /// <summary>请求追踪标识。</summary>
    public string? TraceId { get; set; }

    public static ApiResult<T> Ok(T? data, string message = "success")
        => new() { Code = 200, Message = message, Data = data };

    public static ApiResult<T> Fail(int code, string message)
        => new() { Code = code, Message = message };
}

/// <summary>
/// 无泛型数据的统一响应。
/// </summary>
public class ApiResult : ApiResult<object>
{
    public static ApiResult Ok(string message = "success")
        => new() { Code = 200, Message = message };

    public new static ApiResult Fail(int code, string message)
        => new() { Code = code, Message = message };
}

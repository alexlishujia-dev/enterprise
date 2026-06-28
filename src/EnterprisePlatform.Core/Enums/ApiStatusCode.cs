namespace EnterprisePlatform.Core.Enums;

/// <summary>
/// API 业务状态码。
/// </summary>
public static class ApiStatusCode
{
    public const int Success = 200;
    public const int BadRequest = 400;
    public const int Unauthorized = 401;
    public const int Forbidden = 403;
    public const int NotFound = 404;
    public const int Conflict = 409;
    public const int InternalError = 500;
}

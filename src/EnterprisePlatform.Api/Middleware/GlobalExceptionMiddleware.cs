using EnterprisePlatform.Core.Common;
using EnterprisePlatform.Core.Enums;
using EnterprisePlatform.Core.Exceptions;
using EnterprisePlatform.Utils.Logging;
using System.Net;
using System.Text.Json;

namespace EnterprisePlatform.Api.Middleware;

public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IFileLogWriter _fileLogWriter;

    public GlobalExceptionMiddleware(RequestDelegate next, IFileLogWriter fileLogWriter)
    {
        _next = next;
        _fileLogWriter = fileLogWriter;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = context.TraceIdentifier;
        int code;
        string message;
        HttpStatusCode statusCode;

        switch (exception)
        {
            case BusinessException business:
                code = business.Code;
                message = business.Message;
                statusCode = code switch
                {
                    ApiStatusCode.Unauthorized => HttpStatusCode.Unauthorized,
                    ApiStatusCode.Forbidden => HttpStatusCode.Forbidden,
                    ApiStatusCode.NotFound => HttpStatusCode.NotFound,
                    ApiStatusCode.Conflict => HttpStatusCode.Conflict,
                    _ => HttpStatusCode.BadRequest
                };
                WriteFileLog("Warning", message, exception, context, traceId);
                break;
            default:
                code = ApiStatusCode.InternalError;
                message = "服务器内部错误";
                statusCode = HttpStatusCode.InternalServerError;
                WriteFileLog("Error", message, exception, context, traceId);
                break;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var result = ApiResult.Fail(code, message);
        result.TraceId = traceId;

        await context.Response.WriteAsync(JsonSerializer.Serialize(result, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }

    private void WriteFileLog(string level, string message, Exception exception, HttpContext context, string traceId)
    {
        _fileLogWriter.Write(level, message, exception, new Dictionary<string, string>
        {
            ["TraceId"] = traceId,
            ["Method"] = context.Request.Method,
            ["Path"] = context.Request.Path + context.Request.QueryString,
            ["StatusCode"] = context.Response.StatusCode.ToString(),
            ["IpAddress"] = context.Connection.RemoteIpAddress?.ToString() ?? string.Empty
        });
    }
}

using EnterprisePlatform.Core.Entities;
using EnterprisePlatform.Service.Abstractions;
using System.Diagnostics;
using System.Security.Claims;
using System.Text;

namespace EnterprisePlatform.Api.Middleware;

/// <summary>
/// 自动记录 API 操作日志到 sys_log 表。
/// </summary>
public sealed class OperationLogMiddleware
{
    private const int MaxBodyLength = 2000;

    private static readonly HashSet<string> SkipPaths =
    [
        "/swagger",
        "/health",
        "/uploads",
        "/api/File"
    ];

    private readonly RequestDelegate _next;
    private readonly ILogger<OperationLogMiddleware> _logger;

    public OperationLogMiddleware(RequestDelegate next, ILogger<OperationLogMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IServiceScopeFactory scopeFactory)
    {
        if (ShouldSkip(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        string? requestBody = null;

        if (ShouldReadRequestBody(context.Request))
            requestBody = await ReadRequestBodyAsync(context.Request);

        await _next(context);
        stopwatch.Stop();

        try
        {
            using var scope = scopeFactory.CreateScope();
            var logService = scope.ServiceProvider.GetRequiredService<ISysLogService>();

            var (module, action) = ParseModuleAction(context.Request.Path, context.Request.Method);
            var userId = GetUserId(context.User);
            var userName = context.User.FindFirstValue(ClaimTypes.Name)
                ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            var log = new SysLog
            {
                UserId = userId,
                UserName = userName,
                Module = module,
                Action = action,
                HttpMethod = context.Request.Method,
                RequestPath = context.Request.Path + context.Request.QueryString,
                IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                RequestBody = Truncate(requestBody, MaxBodyLength),
                StatusCode = context.Response.StatusCode,
                DurationMs = stopwatch.ElapsedMilliseconds,
                CreatedAt = DateTime.UtcNow
            };

            await logService.WriteAsync(log, context.RequestAborted);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write operation log for {Path}", context.Request.Path);
        }
    }

    private static bool ShouldSkip(PathString path)
    {
        var value = path.Value ?? string.Empty;
        return SkipPaths.Any(p => value.StartsWith(p, StringComparison.OrdinalIgnoreCase));
    }

    private static bool ShouldReadRequestBody(HttpRequest request)
    {
        if (ShouldSkip(request.Path))
            return false;

        var contentType = request.ContentType ?? string.Empty;
        if (contentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase))
            return false;

        return request.ContentLength > 0 &&
               (HttpMethods.IsPost(request.Method) ||
                HttpMethods.IsPut(request.Method) ||
                HttpMethods.IsPatch(request.Method));
    }

    private static async Task<string?> ReadRequestBodyAsync(HttpRequest request)
    {
        request.EnableBuffering();
        request.Body.Position = 0;

        using var reader = new StreamReader(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0;
        return body;
    }

    private static long? GetUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue("sub")
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

        return long.TryParse(sub, out var id) ? id : null;
    }

    private static (string Module, string Action) ParseModuleAction(PathString path, string method)
    {
        var segments = path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries) ?? [];
        var module = segments.Length >= 2 ? segments[1] : "unknown";
        var action = method.ToUpperInvariant();
        return (module, action);
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        return value.Length <= maxLength ? value : value[..maxLength];
    }
}

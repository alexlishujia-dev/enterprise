using EnterprisePlatform.Core.Common;
using EnterprisePlatform.Core.Enums;
using EnterprisePlatform.Core.Options;
using EnterprisePlatform.Utils.Security;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace EnterprisePlatform.Api.Middleware;

/// <summary>
/// 请求数据验签中间件，可选对响应体签名。
/// </summary>
public sealed class RequestSignatureMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly RequestDelegate _next;
    private readonly RequestSignOptions _options;

    public RequestSignatureMiddleware(
        RequestDelegate next,
        IOptions<RequestSignOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context, IRequestSignatureVerifier verifier)
    {
        if (!_options.Enabled || ShouldSkip(context.Request.Path))
        {
            await _next(context);
            return;
        }

        string? requestBody = null;
        if (HasRequestBody(context.Request))
        {
            context.Request.EnableBuffering();
            requestBody = await ReadRequestBodyAsync(context.Request);
            context.Request.Body.Position = 0;
        }

        var error = verifier.VerifyRequest(
            context.Request.Method,
            context.Request.Path.Value ?? "/",
            context.Request.QueryString.Value ?? string.Empty,
            context.Request.Headers[RequestSignHelper.TimestampHeader].FirstOrDefault(),
            context.Request.Headers[RequestSignHelper.NonceHeader].FirstOrDefault(),
            context.Request.Headers[RequestSignHelper.SignatureHeader].FirstOrDefault(),
            requestBody);

        if (error is not null)
        {
            await WriteErrorAsync(context, error);
            return;
        }

        if (!_options.SignResponse)
        {
            await _next(context);
            return;
        }

        var originalBodyStream = context.Response.Body;
        await using var responseBuffer = new MemoryStream();
        context.Response.Body = responseBuffer;

        try
        {
            await _next(context);

            responseBuffer.Position = 0;
            var responseBody = await new StreamReader(responseBuffer, Encoding.UTF8, leaveOpen: true)
                .ReadToEndAsync();
            responseBuffer.Position = 0;

            var responseTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            var responsePayload = RequestSignHelper.BuildResponsePayload(
                context.Response.StatusCode,
                responseTimestamp,
                responseBody);
            var responseSignature = RequestSignHelper.Sign(responsePayload, _options.SecretKey);

            if (!context.Response.HasStarted)
            {
                context.Response.Headers[RequestSignHelper.ResponseTimestampHeader] = responseTimestamp;
                context.Response.Headers[RequestSignHelper.ResponseSignatureHeader] = responseSignature;
            }

            responseBuffer.Position = 0;
            await responseBuffer.CopyToAsync(originalBodyStream, context.RequestAborted);
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private bool ShouldSkip(PathString path)
    {
        var value = path.Value ?? string.Empty;
        return _options.SkipPaths.Any(prefix =>
            value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasRequestBody(HttpRequest request)
        => request.ContentLength > 0 &&
           (HttpMethods.IsPost(request.Method) ||
            HttpMethods.IsPut(request.Method) ||
            HttpMethods.IsPatch(request.Method));

    private static async Task<string?> ReadRequestBodyAsync(HttpRequest request)
    {
        request.Body.Position = 0;
        using var reader = new StreamReader(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0;
        return body;
    }

    private static async Task WriteErrorAsync(HttpContext context, string message)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status400BadRequest;

        var result = ApiResult.Fail(ApiStatusCode.BadRequest, message);
        result.TraceId = context.TraceIdentifier;
        await context.Response.WriteAsync(JsonSerializer.Serialize(result, JsonOptions));
    }
}

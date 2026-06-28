using EnterprisePlatform.Core.Options;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace EnterprisePlatform.Utils.Security;

/// <summary>
/// 手写 JWT 校验：Base64Url 解码、HMAC-SHA256 验签、exp/iss/aud 检查。
/// </summary>
public sealed class JwtTokenValidator : ITokenValidator
{
    private const string Hs256Algorithm = "HS256";
    private const int ClockSkewSeconds = 60;

    private readonly JwtOptions _options;
    private readonly byte[] _signingKey;

    public JwtTokenValidator(JwtOptions options)
    {
        _options = options;
        _signingKey = Encoding.UTF8.GetBytes(options.SecretKey);
    }

    public TokenValidationResult? Validate(string? authorizationHeader)
    {
        var token = ExtractBearerToken(authorizationHeader);
        if (string.IsNullOrEmpty(token))
            return null;

        var parts = token.Split('.');
        if (parts.Length != 3)
            return null;

        if (!ValidateHeader(parts[0]))
            return null;

        if (!VerifySignature(parts[0], parts[1], parts[2]))
            return null;

        JsonDocument payloadDocument;
        try
        {
            payloadDocument = JsonDocument.Parse(Base64UrlHelper.DecodeToString(parts[1]));
        }
        catch (JsonException)
        {
            return null;
        }

        using (payloadDocument)
        {
            var payload = payloadDocument.RootElement;
            if (!ValidateLifetime(payload) || !ValidateIssuer(payload) || !ValidateAudience(payload))
                return null;

            return BuildResult(payload);
        }
    }

    private static string? ExtractBearerToken(string? authorizationHeader)
    {
        if (string.IsNullOrWhiteSpace(authorizationHeader))
            return null;

        const string bearerPrefix = "Bearer ";
        if (authorizationHeader.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
            return authorizationHeader[bearerPrefix.Length..].Trim();

        return authorizationHeader.Trim();
    }

    private static bool ValidateHeader(string headerPart)
    {
        try
        {
            using var header = JsonDocument.Parse(Base64UrlHelper.DecodeToString(headerPart));
            if (!header.RootElement.TryGetProperty("alg", out var alg))
                return false;

            return string.Equals(alg.GetString(), Hs256Algorithm, StringComparison.Ordinal);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private bool VerifySignature(string headerPart, string payloadPart, string signaturePart)
    {
        byte[] signature;
        try
        {
            signature = Base64UrlHelper.DecodeToBytes(signaturePart);
        }
        catch (FormatException)
        {
            return false;
        }

        var signingInput = Encoding.UTF8.GetBytes($"{headerPart}.{payloadPart}");
        var computed = HMACSHA256.HashData(_signingKey, signingInput);

        return signature.Length == computed.Length &&
               CryptographicOperations.FixedTimeEquals(computed, signature);
    }

    private static bool ValidateLifetime(JsonElement payload)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        if (payload.TryGetProperty("nbf", out var nbf) && nbf.GetInt64() > now + ClockSkewSeconds)
            return false;

        if (payload.TryGetProperty("exp", out var exp) && exp.GetInt64() < now - ClockSkewSeconds)
            return false;

        return true;
    }

    private bool ValidateIssuer(JsonElement payload)
    {
        if (!payload.TryGetProperty("iss", out var iss))
            return false;

        return string.Equals(iss.GetString(), _options.Issuer, StringComparison.Ordinal);
    }

    private bool ValidateAudience(JsonElement payload)
    {
        if (!payload.TryGetProperty("aud", out var aud))
            return false;

        return aud.ValueKind switch
        {
            JsonValueKind.String => string.Equals(aud.GetString(), _options.Audience, StringComparison.Ordinal),
            JsonValueKind.Array => aud.EnumerateArray().Any(item =>
                item.ValueKind == JsonValueKind.String &&
                string.Equals(item.GetString(), _options.Audience, StringComparison.Ordinal)),
            _ => false
        };
    }

    private static TokenValidationResult? BuildResult(JsonElement payload)
    {
        if (!payload.TryGetProperty("sub", out var sub) || !long.TryParse(sub.GetString(), out var userId))
            return null;

        var userName = GetClaimString(payload, "unique_name")
            ?? GetClaimString(payload, ClaimTypes.Name)
            ?? sub.GetString()
            ?? string.Empty;

        return new TokenValidationResult
        {
            UserId = userId,
            UserName = userName,
            Roles = ExtractRoles(payload)
        };
    }

    private static string? GetClaimString(JsonElement payload, string claimType)
    {
        if (!payload.TryGetProperty(claimType, out var value))
            return null;

        return value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }

    private static IReadOnlyList<string> ExtractRoles(JsonElement payload)
    {
        var roles = new List<string>();
        CollectRoleValues(payload, ClaimTypes.Role, roles);
        CollectRoleValues(payload, "role", roles);
        return roles;
    }

    private static void CollectRoleValues(JsonElement payload, string claimType, List<string> roles)
    {
        if (!payload.TryGetProperty(claimType, out var value))
            return;

        switch (value.ValueKind)
        {
            case JsonValueKind.String:
                var role = value.GetString();
                if (!string.IsNullOrEmpty(role))
                    roles.Add(role);
                break;
            case JsonValueKind.Array:
                foreach (var item in value.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        var itemRole = item.GetString();
                        if (!string.IsNullOrEmpty(itemRole))
                            roles.Add(itemRole);
                    }
                }
                break;
        }
    }
}

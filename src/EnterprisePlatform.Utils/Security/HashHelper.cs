using System.Security.Cryptography;
using System.Text;

namespace EnterprisePlatform.Utils.Security;

public static class HashHelper
{
    public static string Sha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static string HashPassword(string password, string salt)
        => Sha256($"{password}:{salt}");

    public static string CreateSalt(int length = 16)
    {
        var bytes = RandomNumberGenerator.GetBytes(length);
        return Convert.ToBase64String(bytes);
    }
}

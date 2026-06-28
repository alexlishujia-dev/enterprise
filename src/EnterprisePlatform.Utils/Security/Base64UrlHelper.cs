using System.Text;

namespace EnterprisePlatform.Utils.Security;

internal static class Base64UrlHelper
{
    public static byte[] DecodeToBytes(string input)
    {
        var output = input.Replace('-', '+').Replace('_', '/');
        switch (output.Length % 4)
        {
            case 2:
                output += "==";
                break;
            case 3:
                output += "=";
                break;
        }

        return Convert.FromBase64String(output);
    }

    public static string DecodeToString(string input)
        => Encoding.UTF8.GetString(DecodeToBytes(input));
}

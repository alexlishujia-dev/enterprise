namespace EnterprisePlatform.Utils.Extensions;

public static class DateTimeExtensions
{
    public static DateTime ToUtc(this DateTime value)
        => value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

    public static string ToIso8601(this DateTime value)
        => value.ToUtc().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
}

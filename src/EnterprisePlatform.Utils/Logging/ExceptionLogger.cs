using EnterprisePlatform.Core.Exceptions;
using System.Data.Common;

namespace EnterprisePlatform.Utils.Logging;

/// <summary>
/// 分层异常文件日志辅助类。
/// </summary>
public static class ExceptionLogger
{
    public static void Log(
        IFileLogWriter writer,
        string layer,
        string source,
        string operation,
        Exception exception,
        IReadOnlyDictionary<string, string>? properties = null)
    {
        if (exception is BusinessException)
            return;

        var context = new Dictionary<string, string>
        {
            ["Layer"] = layer,
            ["Source"] = source,
            ["Operation"] = operation
        };

        if (properties is not null)
        {
            foreach (var (key, value) in properties)
                context[key] = value;
        }

        writer.Write("Error", $"{layer}[{source}] {operation} failed: {exception.Message}", exception, context);
    }

    public static bool IsDatabaseException(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is DbException)
                return true;
        }

        return false;
    }
}

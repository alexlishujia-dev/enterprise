using EnterprisePlatform.Core.Options;
using System.Text;

namespace EnterprisePlatform.Utils.Logging;

/// <summary>
/// 线程安全的按日滚动文本日志写入器。
/// </summary>
public sealed class FileLogWriter : IFileLogWriter, IDisposable
{
    private readonly FileLogOptions _options;
    private readonly string _directory;
    private readonly object _sync = new();

    public FileLogWriter(string directory, FileLogOptions options)
    {
        _directory = directory;
        _options = options;
        Directory.CreateDirectory(_directory);
    }

    public void Write(string level, string message, Exception? exception = null, IReadOnlyDictionary<string, string>? properties = null)
    {
        if (!_options.Enabled || !IsLevelEnabled(level))
            return;

        AppendEntry(level, message, exception, properties);
    }

    public void WriteSql(string message, IReadOnlyDictionary<string, string>? properties = null)
    {
        if (!_options.Enabled || !_options.LogSql)
            return;

        AppendEntry("Information", message, null, properties);
    }

    private void AppendEntry(
        string level,
        string message,
        Exception? exception,
        IReadOnlyDictionary<string, string>? properties)
    {
        var line = BuildLogEntry(level, message, exception, properties);
        var filePath = GetLogFilePath(DateTime.Now);

        lock (_sync)
        {
            File.AppendAllText(filePath, line, Encoding.UTF8);
        }
    }

    private bool IsLevelEnabled(string level)
        => GetLevelRank(level) >= GetLevelRank(_options.MinimumLevel);

    private static int GetLevelRank(string level)
        => level.ToUpperInvariant() switch
        {
            "TRACE" => 0,
            "DEBUG" => 1,
            "INFORMATION" or "INFO" => 2,
            "WARNING" or "WARN" => 3,
            "ERROR" => 4,
            "CRITICAL" or "FATAL" => 5,
            _ => 2
        };

    private string GetLogFilePath(DateTime time)
        => Path.Combine(_directory, $"{_options.FilePrefix}{time:yyyy-MM-dd}.log");

    private static string BuildLogEntry(
        string level,
        string message,
        Exception? exception,
        IReadOnlyDictionary<string, string>? properties)
    {
        var builder = new StringBuilder();
        builder.Append('[').Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")).Append("] ");
        builder.Append('[').Append(level).Append("] ");
        builder.AppendLine(message);

        if (properties is { Count: > 0 })
        {
            foreach (var (key, value) in properties)
                builder.AppendLine($"{key}={value}");
        }

        if (exception is not null)
            AppendException(builder, exception);

        builder.AppendLine(new string('-', 80));
        return builder.ToString();
    }

    private static void AppendException(StringBuilder builder, Exception exception)
    {
        var current = exception;
        var depth = 0;

        while (current is not null)
        {
            builder.AppendLine(depth == 0 ? "Exception:" : $"InnerException[{depth}]:");
            builder.AppendLine(current.GetType().FullName);
            builder.AppendLine(current.Message);
            if (!string.IsNullOrWhiteSpace(current.StackTrace))
                builder.AppendLine(current.StackTrace);

            current = current.InnerException;
            depth++;
        }
    }

    public void Dispose()
    {
    }
}

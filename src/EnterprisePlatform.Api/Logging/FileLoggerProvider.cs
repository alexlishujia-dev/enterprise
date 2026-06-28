using EnterprisePlatform.Core.Options;
using EnterprisePlatform.Utils.Logging;
using Microsoft.Extensions.Options;

namespace EnterprisePlatform.Api.Logging;

/// <summary>
/// 将 ILogger 输出桥接到文本文件。
/// </summary>
public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly IFileLogWriter _writer;
    private readonly LogLevel _minimumLevel;

    public FileLoggerProvider(IFileLogWriter writer, IOptions<FileLogOptions> options)
    {
        _writer = writer;
        _minimumLevel = ParseLevel(options.Value.MinimumLevel);
    }

    public ILogger CreateLogger(string categoryName)
        => new FileLogger(categoryName, _writer, _minimumLevel);

    public void Dispose()
    {
    }

    private static LogLevel ParseLevel(string? level)
        => Enum.TryParse<LogLevel>(level, ignoreCase: true, out var parsed)
            ? parsed
            : LogLevel.Warning;
}

internal sealed class FileLogger : ILogger
{
    private readonly string _categoryName;
    private readonly IFileLogWriter _writer;
    private readonly LogLevel _minimumLevel;

    public FileLogger(string categoryName, IFileLogWriter writer, LogLevel minimumLevel)
    {
        _categoryName = categoryName;
        _writer = writer;
        _minimumLevel = minimumLevel;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= _minimumLevel;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);
        var properties = new Dictionary<string, string>
        {
            ["Category"] = _categoryName,
            ["EventId"] = eventId.Id.ToString()
        };

        _writer.Write(logLevel.ToString(), message, exception, properties);
    }
}

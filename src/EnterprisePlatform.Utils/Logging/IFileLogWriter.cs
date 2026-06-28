namespace EnterprisePlatform.Utils.Logging;

/// <summary>
/// 文本文件日志写入器。
/// </summary>
public interface IFileLogWriter
{
    void Write(string level, string message, Exception? exception = null, IReadOnlyDictionary<string, string>? properties = null);

    /// <summary>写入正常 SQL 执行日志（Information 级别，受 LogSql 配置控制）。</summary>
    void WriteSql(string message, IReadOnlyDictionary<string, string>? properties = null);
}

namespace EnterprisePlatform.Core.Options;

/// <summary>
/// 文本文件日志配置。
/// </summary>
public class FileLogOptions
{
    public const string SectionName = "FileLog";

    public bool Enabled { get; set; } = true;

    /// <summary>日志目录，相对路径基于 ContentRoot。</summary>
    public string Directory { get; set; } = "logs";

    /// <summary>文件名前缀，实际文件名为 {Prefix}yyyy-MM-dd.log。</summary>
    public string FilePrefix { get; set; } = "app-";

    /// <summary>写入文件的最低级别：Trace/Debug/Information/Warning/Error/Critical。</summary>
    public string MinimumLevel { get; set; } = "Warning";

    /// <summary>是否将正常执行的 SQL 写入 Information 日志。</summary>
    public bool LogSql { get; set; } = true;
}

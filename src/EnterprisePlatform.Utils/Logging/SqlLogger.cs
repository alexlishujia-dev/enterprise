namespace EnterprisePlatform.Utils.Logging;

/// <summary>
/// 正常 SQL 执行文件日志辅助类。
/// </summary>
public static class SqlLogger
{
    public static void LogSuccess(
        IFileLogWriter writer,
        string tableName,
        string operation,
        (string Before, string After) sqlPair)
    {
        writer.WriteSql($"Repository[{tableName}] {operation}", new Dictionary<string, string>
        {
            ["Layer"] = "Repository",
            ["Source"] = tableName,
            ["Operation"] = operation,
            ["SqlBefore"] = sqlPair.Before,
            ["SqlAfter"] = sqlPair.After
        });
    }
}

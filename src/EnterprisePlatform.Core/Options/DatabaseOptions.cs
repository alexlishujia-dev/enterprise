using EnterprisePlatform.Core.Enums;

namespace EnterprisePlatform.Core.Options;

/// <summary>
/// 数据库连接配置，支持多数据库切换。
/// </summary>
public class DatabaseOptions
{
    public const string SectionName = "Database";

    public DatabaseProvider Provider { get; set; } = DatabaseProvider.PostgreSql;

    public string ConnectionString { get; set; } = string.Empty;

    public int CommandTimeoutSeconds { get; set; } = 30;
}

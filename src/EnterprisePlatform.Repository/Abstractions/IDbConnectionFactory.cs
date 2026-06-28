using EnterprisePlatform.Core.Enums;
using System.Data.Common;

namespace EnterprisePlatform.Repository.Abstractions;

/// <summary>
/// 数据库连接工厂，按配置创建 ADO.NET 连接。
/// </summary>
public interface IDbConnectionFactory
{
    DatabaseProvider Provider { get; }

    DbConnection CreateConnection();

    DbParameter CreateParameter(string name, object? value);

    string WrapPaging(string sql, string orderBy, int skip, int take);

    string GetLastInsertIdSql(string tableName, string idColumn);

    /// <summary>未删除记录的 WHERE 片段，例如 IsDeleted = @NotDeleted。</summary>
    string NotDeletedFilter { get; }

    /// <summary>激活记录的 WHERE 片段，例如 IsActive = @IsActive。</summary>
    string ActiveFilter { get; }
}

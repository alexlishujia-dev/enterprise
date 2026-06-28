using EnterprisePlatform.Core.Enums;
using EnterprisePlatform.Core.Options;
using EnterprisePlatform.Repository.Abstractions;
using EnterprisePlatform.Utils.Logging;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using MySqlConnector;
using Npgsql;
using System.Data.Common;

namespace EnterprisePlatform.Repository.Data;

public sealed class DbConnectionFactory : IDbConnectionFactory
{
    private readonly DatabaseOptions _options;
    private readonly IFileLogWriter _fileLogWriter;

    public DbConnectionFactory(IOptions<DatabaseOptions> options, IFileLogWriter fileLogWriter)
    {
        _options = options.Value;
        _fileLogWriter = fileLogWriter;
        Provider = _options.Provider;
    }

    public DatabaseProvider Provider { get; }

    public string NotDeletedFilter => "IsDeleted = @NotDeleted";

    public string ActiveFilter => "IsActive = @IsActive";

    public DbConnection CreateConnection()
    {
        try
        {
            DbConnection connection = Provider switch
            {
                DatabaseProvider.SqlServer => new SqlConnection(_options.ConnectionString),
                DatabaseProvider.MySql => new MySqlConnection(_options.ConnectionString),
                DatabaseProvider.PostgreSql => new NpgsqlConnection(_options.ConnectionString),
                DatabaseProvider.Sqlite => new SqliteConnection(_options.ConnectionString),
                _ => throw new NotSupportedException($"Unsupported database provider: {Provider}")
            };

            connection.Open();
            return connection;
        }
        catch (Exception ex)
        {
            ExceptionLogger.Log(_fileLogWriter, "Repository", "DbConnectionFactory", nameof(CreateConnection), ex, new Dictionary<string, string>
            {
                ["Provider"] = Provider.ToString()
            });
            throw;
        }
    }

    public DbParameter CreateParameter(string name, object? value)
    {
        var parameterName = name.StartsWith('@') ? name : $"@{name}";
        DbParameter parameter = Provider switch
        {
            DatabaseProvider.SqlServer => new SqlParameter(parameterName, value ?? DBNull.Value),
            DatabaseProvider.MySql => new MySqlParameter(parameterName, value ?? DBNull.Value),
            DatabaseProvider.PostgreSql => new NpgsqlParameter(parameterName, value ?? DBNull.Value),
            DatabaseProvider.Sqlite => new SqliteParameter(parameterName, value ?? DBNull.Value),
            _ => throw new NotSupportedException($"Unsupported database provider: {Provider}")
        };

        return parameter;
    }

    public string WrapPaging(string sql, string orderBy, int skip, int take)
    {
        return Provider switch
        {
            DatabaseProvider.SqlServer =>
                $"{sql} ORDER BY {orderBy} OFFSET {skip} ROWS FETCH NEXT {take} ROWS ONLY",
            DatabaseProvider.MySql =>
                $"{sql} ORDER BY {orderBy} LIMIT {take} OFFSET {skip}",
            DatabaseProvider.PostgreSql =>
                $"{sql} ORDER BY {orderBy} LIMIT {take} OFFSET {skip}",
            DatabaseProvider.Sqlite =>
                $"{sql} ORDER BY {orderBy} LIMIT {take} OFFSET {skip}",
            _ => throw new NotSupportedException($"Unsupported database provider: {Provider}")
        };
    }

    public string GetLastInsertIdSql(string tableName, string idColumn)
    {
        return Provider switch
        {
            DatabaseProvider.SqlServer => "SELECT CAST(SCOPE_IDENTITY() AS bigint);",
            DatabaseProvider.MySql => "SELECT LAST_INSERT_ID();",
            DatabaseProvider.PostgreSql => "SELECT lastval();",
            DatabaseProvider.Sqlite => "SELECT last_insert_rowid();",
            _ => throw new NotSupportedException($"Unsupported database provider: {Provider}")
        };
    }
}

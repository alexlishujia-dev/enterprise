using EnterprisePlatform.Core.Common;
using EnterprisePlatform.Core.Entities;
using EnterprisePlatform.Repository.Abstractions;
using EnterprisePlatform.Repository.Sql;
using EnterprisePlatform.Utils.Logging;
using System.Data.Common;

namespace EnterprisePlatform.Repository.Repositories;

/// <summary>
/// ADO.NET 通用仓储基类，全参数化查询防 SQL 注入。
/// </summary>
public abstract class BaseRepository<TEntity> : IRepository<TEntity> where TEntity : BaseEntity, new()
{
    protected BaseRepository(IDbConnectionFactory connectionFactory, IFileLogWriter fileLogWriter)
    {
        ConnectionFactory = connectionFactory;
        FileLogWriter = fileLogWriter;
    }

    protected IDbConnectionFactory ConnectionFactory { get; }

    protected IFileLogWriter FileLogWriter { get; }

    protected abstract string TableName { get; }

    protected abstract string PrimaryKeyColumn { get; }

    protected abstract TEntity MapEntity(DbDataReader reader);

    protected abstract (string Sql, List<DbParameter> Parameters) BuildInsertCommand(TEntity entity);

    protected abstract (string Sql, List<DbParameter> Parameters) BuildUpdateCommand(TEntity entity);

    protected Task<T> ExecuteAsync<T>(string operation, Func<Task<T>> action)
        => action();

    protected Task ExecuteAsync(string operation, Func<Task> action)
        => action();

    protected void LogSqlError(string operation, Exception exception, DbCommand command)
        => LogSqlError(operation, exception, SqlLogFormatter.FormatPair(command));

    protected void LogSqlError(string operation, Exception exception, (string Before, string After) sqlPair)
    {
        ExceptionLogger.Log(FileLogWriter, "Repository", TableName, operation, exception, new Dictionary<string, string>
        {
            ["SqlBefore"] = sqlPair.Before,
            ["SqlAfter"] = sqlPair.After
        });
    }

    protected void LogSqlError(string operation, Exception exception, string sql, IEnumerable<DbParameter>? parameters = null)
        => LogSqlError(operation, exception, SqlLogFormatter.FormatPair(sql, parameters));

    protected void LogSqlInfo(string operation, DbCommand command)
        => SqlLogger.LogSuccess(FileLogWriter, TableName, operation, SqlLogFormatter.FormatPair(command));

    protected async Task<int> ExecuteNonQueryAsync(string operation, DbCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var affected = await command.ExecuteNonQueryAsync(cancellationToken);
            LogSqlInfo(operation, command);
            return affected;
        }
        catch (Exception ex)
        {
            LogSqlError(operation, ex, command);
            throw;
        }
    }

    protected async Task<object?> ExecuteScalarAsync(string operation, DbCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await command.ExecuteScalarAsync(cancellationToken);
            LogSqlInfo(operation, command);
            return result;
        }
        catch (Exception ex)
        {
            LogSqlError(operation, ex, command);
            throw;
        }
    }

    protected async Task<DbDataReader> ExecuteReaderAsync(string operation, DbCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var reader = await command.ExecuteReaderAsync(cancellationToken);
            LogSqlInfo(operation, command);
            return reader;
        }
        catch (Exception ex)
        {
            LogSqlError(operation, ex, command);
            throw;
        }
    }

    protected DbParameter CreateNotDeletedParameter()
        => ConnectionFactory.CreateParameter("NotDeleted", false);

    protected DbParameter CreateDeletedParameter()
        => ConnectionFactory.CreateParameter("Deleted", true);

    protected DbParameter CreateActiveParameter()
        => ConnectionFactory.CreateParameter("IsActive", true);

    public virtual Task<TEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        => ExecuteAsync(nameof(GetByIdAsync), () => GetByIdCoreAsync(id, cancellationToken));

    private async Task<TEntity?> GetByIdCoreAsync(long id, CancellationToken cancellationToken)
    {
        const string operation = nameof(GetByIdAsync);
        var sql = $"SELECT * FROM {TableName} WHERE {PrimaryKeyColumn} = @Id AND {ConnectionFactory.NotDeletedFilter}";
        await using var connection = ConnectionFactory.CreateConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.Add(ConnectionFactory.CreateParameter("Id", id));
        command.Parameters.Add(CreateNotDeletedParameter());

        await using var reader = await ExecuteReaderAsync(operation, command, cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapEntity(reader) : null;
    }

    public virtual Task<PagedResult<TEntity>> GetPagedAsync(PageQuery query, CancellationToken cancellationToken = default)
        => ExecuteAsync(nameof(GetPagedAsync), () => GetPagedCoreAsync(query, cancellationToken));

    private async Task<PagedResult<TEntity>> GetPagedCoreAsync(PageQuery query, CancellationToken cancellationToken)
    {
        const string operation = nameof(GetPagedAsync);
        var where = $"WHERE {ConnectionFactory.NotDeletedFilter}";
        var countSql = $"SELECT COUNT(1) FROM {TableName} {where}";
        var dataSql = $"SELECT * FROM {TableName} {where}";
        var skip = (query.PageIndex - 1) * query.PageSize;
        var pagedSql = ConnectionFactory.WrapPaging(dataSql, $"{PrimaryKeyColumn} DESC", skip, query.PageSize);

        await using var connection = ConnectionFactory.CreateConnection();

        long total;
        await using (var countCommand = connection.CreateCommand())
        {
            countCommand.CommandText = countSql;
            countCommand.Parameters.Add(CreateNotDeletedParameter());
            total = Convert.ToInt64(await ExecuteScalarAsync($"{operation}:Count", countCommand, cancellationToken) ?? 0L);
        }

        var items = new List<TEntity>();
        await using (var dataCommand = connection.CreateCommand())
        {
            dataCommand.CommandText = pagedSql;
            dataCommand.Parameters.Add(CreateNotDeletedParameter());
            await using var reader = await ExecuteReaderAsync($"{operation}:Query", dataCommand, cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                items.Add(MapEntity(reader));
        }

        return new PagedResult<TEntity>
        {
            Items = items,
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
    }

    public virtual Task<long> InsertAsync(TEntity entity, CancellationToken cancellationToken = default)
        => ExecuteAsync(nameof(InsertAsync), () => InsertCoreAsync(entity, cancellationToken));

    private async Task<long> InsertCoreAsync(TEntity entity, CancellationToken cancellationToken)
    {
        const string operation = nameof(InsertAsync);
        var (sql, parameters) = BuildInsertCommand(entity);
        await using var connection = ConnectionFactory.CreateConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var parameter in parameters)
            command.Parameters.Add(parameter);

        await ExecuteNonQueryAsync(operation, command, cancellationToken);

        command.Parameters.Clear();
        var lastInsertSql = ConnectionFactory.GetLastInsertIdSql(TableName, PrimaryKeyColumn);
        command.CommandText = lastInsertSql;
        var id = Convert.ToInt64(await ExecuteScalarAsync($"{operation}:LastInsertId", command, cancellationToken) ?? 0L);
        entity.Id = id;
        return id;
    }

    public virtual Task<int> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
        => ExecuteAsync(nameof(UpdateAsync), () => UpdateCoreAsync(entity, cancellationToken));

    private async Task<int> UpdateCoreAsync(TEntity entity, CancellationToken cancellationToken)
    {
        const string operation = nameof(UpdateAsync);
        entity.UpdatedAt = DateTime.UtcNow;
        var (sql, parameters) = BuildUpdateCommand(entity);
        await using var connection = ConnectionFactory.CreateConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var parameter in parameters)
            command.Parameters.Add(parameter);

        return await ExecuteNonQueryAsync(operation, command, cancellationToken);
    }

    public virtual Task<int> SoftDeleteAsync(long id, CancellationToken cancellationToken = default)
        => ExecuteAsync(nameof(SoftDeleteAsync), () => SoftDeleteCoreAsync(id, cancellationToken));

    private async Task<int> SoftDeleteCoreAsync(long id, CancellationToken cancellationToken)
    {
        const string operation = nameof(SoftDeleteAsync);
        var sql = $"UPDATE {TableName} SET IsDeleted = @Deleted, UpdatedAt = @UpdatedAt WHERE {PrimaryKeyColumn} = @Id AND {ConnectionFactory.NotDeletedFilter}";
        await using var connection = ConnectionFactory.CreateConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.Add(ConnectionFactory.CreateParameter("UpdatedAt", DateTime.UtcNow));
        command.Parameters.Add(ConnectionFactory.CreateParameter("Id", id));
        command.Parameters.Add(CreateDeletedParameter());
        command.Parameters.Add(CreateNotDeletedParameter());
        return await ExecuteNonQueryAsync(operation, command, cancellationToken);
    }

    protected Task<IReadOnlyList<TEntity>> QueryAsync(string sql, IEnumerable<DbParameter>? parameters = null, CancellationToken cancellationToken = default)
        => ExecuteAsync(nameof(QueryAsync), () => QueryCoreAsync(sql, parameters, cancellationToken));

    private async Task<IReadOnlyList<TEntity>> QueryCoreAsync(string sql, IEnumerable<DbParameter>? parameters, CancellationToken cancellationToken)
    {
        const string operation = nameof(QueryAsync);
        await using var connection = ConnectionFactory.CreateConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        if (parameters != null)
        {
            foreach (var parameter in parameters)
                command.Parameters.Add(CloneParameter(parameter));
        }

        var items = new List<TEntity>();
        await using var reader = await ExecuteReaderAsync(operation, command, cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            items.Add(MapEntity(reader));

        return items;
    }

    protected Task<TEntity?> QuerySingleAsync(string sql, IEnumerable<DbParameter>? parameters = null, CancellationToken cancellationToken = default)
        => ExecuteAsync(nameof(QuerySingleAsync), async () =>
        {
            var items = await QueryCoreAsync(sql, parameters, cancellationToken);
            return items.FirstOrDefault();
        });

    protected static T GetValue<T>(DbDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        if (reader.IsDBNull(ordinal))
            return default!;

        var value = reader.GetValue(ordinal);
        return (T)Convert.ChangeType(value, typeof(T));
    }

    private DbParameter CloneParameter(DbParameter source)
    {
        var clone = ConnectionFactory.CreateParameter(source.ParameterName.TrimStart('@'), source.Value);
        clone.Direction = source.Direction;
        return clone;
    }
}

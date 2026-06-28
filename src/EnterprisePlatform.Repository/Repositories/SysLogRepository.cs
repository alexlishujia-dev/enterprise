using EnterprisePlatform.Core.Common;
using EnterprisePlatform.Core.Entities;
using EnterprisePlatform.Repository.Abstractions;
using EnterprisePlatform.Utils.Logging;
using System.Data.Common;

namespace EnterprisePlatform.Repository.Repositories;

public interface ISysLogRepository : IRepository<SysLog>
{
    Task<PagedResult<SysLog>> GetPagedAsync(SysLogQuery query, CancellationToken cancellationToken = default);
}

public sealed class SysLogRepository : BaseRepository<SysLog>, ISysLogRepository
{
    public SysLogRepository(IDbConnectionFactory connectionFactory, IFileLogWriter fileLogWriter)
        : base(connectionFactory, fileLogWriter)
    {
    }

    protected override string TableName => "sys_log";

    protected override string PrimaryKeyColumn => "Id";

    public Task<PagedResult<SysLog>> GetPagedAsync(SysLogQuery query, CancellationToken cancellationToken = default)
        => ExecuteAsync(nameof(GetPagedAsync), () => GetPagedCoreAsync(query, cancellationToken));

    private async Task<PagedResult<SysLog>> GetPagedCoreAsync(SysLogQuery query, CancellationToken cancellationToken)
    {
        const string operation = nameof(GetPagedAsync);
        var where = $"WHERE {ConnectionFactory.NotDeletedFilter}";
        var parameters = new List<DbParameter> { CreateNotDeletedParameter() };

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            where += " AND (UserName LIKE @Keyword OR Module LIKE @Keyword OR Action LIKE @Keyword OR RequestPath LIKE @Keyword)";
            parameters.Add(ConnectionFactory.CreateParameter("Keyword", $"%{query.Keyword}%"));
        }

        if (query.UserId.HasValue)
        {
            where += " AND UserId = @UserId";
            parameters.Add(ConnectionFactory.CreateParameter("UserId", query.UserId.Value));
        }

        if (!string.IsNullOrWhiteSpace(query.Module))
        {
            where += " AND Module = @Module";
            parameters.Add(ConnectionFactory.CreateParameter("Module", query.Module));
        }

        if (query.StartDate.HasValue)
        {
            where += " AND CreatedAt >= @StartDate";
            parameters.Add(ConnectionFactory.CreateParameter("StartDate", query.StartDate.Value));
        }

        if (query.EndDate.HasValue)
        {
            where += " AND CreatedAt <= @EndDate";
            parameters.Add(ConnectionFactory.CreateParameter("EndDate", query.EndDate.Value));
        }

        var countSql = $"SELECT COUNT(1) FROM {TableName} {where}";
        var dataSql = $"SELECT * FROM {TableName} {where}";
        var skip = (query.PageIndex - 1) * query.PageSize;
        var pagedSql = ConnectionFactory.WrapPaging(dataSql, $"{PrimaryKeyColumn} DESC", skip, query.PageSize);

        await using var connection = ConnectionFactory.CreateConnection();

        long total;
        await using (var countCommand = connection.CreateCommand())
        {
            countCommand.CommandText = countSql;
            foreach (var parameter in parameters)
                countCommand.Parameters.Add(parameter);
            total = Convert.ToInt64(await ExecuteScalarAsync($"{operation}:Count", countCommand, cancellationToken) ?? 0L);
        }

        var items = new List<SysLog>();
        await using (var dataCommand = connection.CreateCommand())
        {
            dataCommand.CommandText = pagedSql;
            foreach (var parameter in parameters)
                dataCommand.Parameters.Add(ConnectionFactory.CreateParameter(parameter.ParameterName.TrimStart('@'), parameter.Value));

            await using var reader = await ExecuteReaderAsync($"{operation}:Query", dataCommand, cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                items.Add(MapEntity(reader));
        }

        return new PagedResult<SysLog>
        {
            Items = items,
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
    }

    protected override SysLog MapEntity(DbDataReader reader)
        => new()
        {
            Id = GetValue<long>(reader, "Id"),
            UserId = reader.IsDBNull(reader.GetOrdinal("UserId")) ? null : GetValue<long>(reader, "UserId"),
            UserName = reader["UserName"] as string,
            Module = GetValue<string>(reader, "Module"),
            Action = GetValue<string>(reader, "Action"),
            HttpMethod = GetValue<string>(reader, "HttpMethod"),
            RequestPath = GetValue<string>(reader, "RequestPath"),
            IpAddress = reader["IpAddress"] as string,
            RequestBody = reader["RequestBody"] as string,
            StatusCode = GetValue<int>(reader, "StatusCode"),
            DurationMs = GetValue<long>(reader, "DurationMs"),
            CreatedAt = GetValue<DateTime>(reader, "CreatedAt"),
            UpdatedAt = reader["UpdatedAt"] as DateTime?,
            IsDeleted = GetValue<bool>(reader, "IsDeleted")
        };

    protected override (string Sql, List<DbParameter> Parameters) BuildInsertCommand(SysLog entity)
    {
        const string sql = """
            INSERT INTO sys_log (UserId, UserName, Module, Action, HttpMethod, RequestPath, IpAddress, RequestBody, StatusCode, DurationMs, CreatedAt, IsDeleted)
            VALUES (@UserId, @UserName, @Module, @Action, @HttpMethod, @RequestPath, @IpAddress, @RequestBody, @StatusCode, @DurationMs, @CreatedAt, @IsDeleted)
            """;

        var parameters = new List<DbParameter>
        {
            ConnectionFactory.CreateParameter("UserId", entity.UserId),
            ConnectionFactory.CreateParameter("UserName", entity.UserName),
            ConnectionFactory.CreateParameter("Module", entity.Module),
            ConnectionFactory.CreateParameter("Action", entity.Action),
            ConnectionFactory.CreateParameter("HttpMethod", entity.HttpMethod),
            ConnectionFactory.CreateParameter("RequestPath", entity.RequestPath),
            ConnectionFactory.CreateParameter("IpAddress", entity.IpAddress),
            ConnectionFactory.CreateParameter("RequestBody", entity.RequestBody),
            ConnectionFactory.CreateParameter("StatusCode", entity.StatusCode),
            ConnectionFactory.CreateParameter("DurationMs", entity.DurationMs),
            ConnectionFactory.CreateParameter("CreatedAt", entity.CreatedAt),
            ConnectionFactory.CreateParameter("IsDeleted", entity.IsDeleted)
        };

        return (sql, parameters);
    }

    protected override (string Sql, List<DbParameter> Parameters) BuildUpdateCommand(SysLog entity)
    {
        var sql = $"UPDATE sys_log SET UpdatedAt = @UpdatedAt WHERE Id = @Id AND {ConnectionFactory.NotDeletedFilter}";

        var parameters = new List<DbParameter>
        {
            ConnectionFactory.CreateParameter("UpdatedAt", entity.UpdatedAt),
            ConnectionFactory.CreateParameter("Id", entity.Id),
            CreateNotDeletedParameter()
        };

        return (sql, parameters);
    }
}

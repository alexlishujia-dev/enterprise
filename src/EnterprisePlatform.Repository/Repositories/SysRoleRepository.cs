using EnterprisePlatform.Core.Common;
using EnterprisePlatform.Core.Entities;
using EnterprisePlatform.Repository.Abstractions;
using EnterprisePlatform.Utils.Logging;
using System.Data.Common;

namespace EnterprisePlatform.Repository.Repositories;

public interface ISysRoleRepository : IRepository<SysRole>
{
    Task<SysRole?> GetByRoleCodeAsync(string roleCode, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SysRole>> GetRolesByUserIdAsync(long userId, CancellationToken cancellationToken = default);

    Task AssignRolesToUserAsync(long userId, IEnumerable<long> roleIds, CancellationToken cancellationToken = default);
}

public sealed class SysRoleRepository : BaseRepository<SysRole>, ISysRoleRepository
{
    public SysRoleRepository(IDbConnectionFactory connectionFactory, IFileLogWriter fileLogWriter)
        : base(connectionFactory, fileLogWriter)
    {
    }

    protected override string TableName => "sys_role";

    protected override string PrimaryKeyColumn => "Id";

    public Task<SysRole?> GetByRoleCodeAsync(string roleCode, CancellationToken cancellationToken = default)
    {
        var sql = $"SELECT * FROM sys_role WHERE RoleCode = @RoleCode AND {ConnectionFactory.NotDeletedFilter}";
        var parameters = new List<DbParameter>
        {
            ConnectionFactory.CreateParameter("RoleCode", roleCode),
            CreateNotDeletedParameter()
        };
        return QuerySingleAsync(sql, parameters, cancellationToken);
    }

    public async Task<IReadOnlyList<SysRole>> GetRolesByUserIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        var sql = $"""
            SELECT r.* FROM sys_role r
            INNER JOIN sys_user_role ur ON ur.RoleId = r.Id
            WHERE ur.UserId = @UserId AND r.{ConnectionFactory.NotDeletedFilter} AND r.{ConnectionFactory.ActiveFilter}
            ORDER BY r.Id
            """;

        var parameters = new List<DbParameter>
        {
            ConnectionFactory.CreateParameter("UserId", userId),
            CreateNotDeletedParameter(),
            CreateActiveParameter()
        };
        return await QueryAsync(sql, parameters, cancellationToken);
    }

    public Task AssignRolesToUserAsync(long userId, IEnumerable<long> roleIds, CancellationToken cancellationToken = default)
        => ExecuteAsync(nameof(AssignRolesToUserAsync), () => AssignRolesCoreAsync(userId, roleIds, cancellationToken));

    private async Task AssignRolesCoreAsync(long userId, IEnumerable<long> roleIds, CancellationToken cancellationToken)
    {
        const string operation = nameof(AssignRolesToUserAsync);
        await using var connection = ConnectionFactory.CreateConnection();
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using (var deleteCommand = connection.CreateCommand())
            {
                deleteCommand.Transaction = transaction;
                deleteCommand.CommandText = "DELETE FROM sys_user_role WHERE UserId = @UserId";
                deleteCommand.Parameters.Add(ConnectionFactory.CreateParameter("UserId", userId));
                await ExecuteNonQueryAsync($"{operation}:Delete", deleteCommand, cancellationToken);
            }

            foreach (var roleId in roleIds.Distinct())
            {
                await using var insertCommand = connection.CreateCommand();
                insertCommand.Transaction = transaction;
                insertCommand.CommandText = """
                    INSERT INTO sys_user_role (UserId, RoleId, CreatedAt)
                    VALUES (@UserId, @RoleId, @CreatedAt)
                    """;
                insertCommand.Parameters.Add(ConnectionFactory.CreateParameter("UserId", userId));
                insertCommand.Parameters.Add(ConnectionFactory.CreateParameter("RoleId", roleId));
                insertCommand.Parameters.Add(ConnectionFactory.CreateParameter("CreatedAt", DateTime.UtcNow));
                await ExecuteNonQueryAsync($"{operation}:Insert", insertCommand, cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public override Task<PagedResult<SysRole>> GetPagedAsync(PageQuery query, CancellationToken cancellationToken = default)
        => ExecuteAsync(nameof(GetPagedAsync), () => GetPagedCoreAsync(query, cancellationToken));

    private async Task<PagedResult<SysRole>> GetPagedCoreAsync(PageQuery query, CancellationToken cancellationToken)
    {
        const string operation = nameof(GetPagedAsync);
        var where = $"WHERE {ConnectionFactory.NotDeletedFilter}";
        var parameters = new List<DbParameter> { CreateNotDeletedParameter() };

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            where += " AND (RoleCode LIKE @Keyword OR RoleName LIKE @Keyword OR Description LIKE @Keyword)";
            parameters.Add(ConnectionFactory.CreateParameter("Keyword", $"%{query.Keyword}%"));
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

        var items = new List<SysRole>();
        await using (var dataCommand = connection.CreateCommand())
        {
            dataCommand.CommandText = pagedSql;
            foreach (var parameter in parameters)
                dataCommand.Parameters.Add(ConnectionFactory.CreateParameter(parameter.ParameterName.TrimStart('@'), parameter.Value));

            await using var reader = await ExecuteReaderAsync($"{operation}:Query", dataCommand, cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                items.Add(MapEntity(reader));
        }

        return new PagedResult<SysRole>
        {
            Items = items,
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
    }

    protected override SysRole MapEntity(DbDataReader reader)
        => new()
        {
            Id = GetValue<long>(reader, "Id"),
            RoleCode = GetValue<string>(reader, "RoleCode"),
            RoleName = GetValue<string>(reader, "RoleName"),
            Description = reader["Description"] as string,
            IsActive = GetValue<bool>(reader, "IsActive"),
            CreatedAt = GetValue<DateTime>(reader, "CreatedAt"),
            UpdatedAt = reader["UpdatedAt"] as DateTime?,
            IsDeleted = GetValue<bool>(reader, "IsDeleted")
        };

    protected override (string Sql, List<DbParameter> Parameters) BuildInsertCommand(SysRole entity)
    {
        const string sql = """
            INSERT INTO sys_role (RoleCode, RoleName, Description, IsActive, CreatedAt, IsDeleted)
            VALUES (@RoleCode, @RoleName, @Description, @IsActive, @CreatedAt, @IsDeleted)
            """;

        var parameters = new List<DbParameter>
        {
            ConnectionFactory.CreateParameter("RoleCode", entity.RoleCode),
            ConnectionFactory.CreateParameter("RoleName", entity.RoleName),
            ConnectionFactory.CreateParameter("Description", entity.Description),
            ConnectionFactory.CreateParameter("IsActive", entity.IsActive),
            ConnectionFactory.CreateParameter("CreatedAt", entity.CreatedAt),
            ConnectionFactory.CreateParameter("IsDeleted", entity.IsDeleted)
        };

        return (sql, parameters);
    }

    protected override (string Sql, List<DbParameter> Parameters) BuildUpdateCommand(SysRole entity)
    {
        var sql = $"""
            UPDATE sys_role
            SET RoleName = @RoleName,
                Description = @Description,
                IsActive = @IsActive,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id AND {ConnectionFactory.NotDeletedFilter}
            """;

        var parameters = new List<DbParameter>
        {
            ConnectionFactory.CreateParameter("RoleName", entity.RoleName),
            ConnectionFactory.CreateParameter("Description", entity.Description),
            ConnectionFactory.CreateParameter("IsActive", entity.IsActive),
            ConnectionFactory.CreateParameter("UpdatedAt", entity.UpdatedAt),
            ConnectionFactory.CreateParameter("Id", entity.Id),
            CreateNotDeletedParameter()
        };

        return (sql, parameters);
    }
}

using EnterprisePlatform.Core.Common;
using EnterprisePlatform.Core.Entities;
using EnterprisePlatform.Repository.Abstractions;
using EnterprisePlatform.Utils.Logging;
using System.Data.Common;

namespace EnterprisePlatform.Repository.Repositories;

public interface ISysUserRepository : IRepository<SysUser>
{
    Task<SysUser?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default);
}

public sealed class SysUserRepository : BaseRepository<SysUser>, ISysUserRepository
{
    public SysUserRepository(IDbConnectionFactory connectionFactory, IFileLogWriter fileLogWriter)
        : base(connectionFactory, fileLogWriter)
    {
    }

    protected override string TableName => "sys_user";

    protected override string PrimaryKeyColumn => "Id";

    public Task<SysUser?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default)
    {
        var sql = $"SELECT * FROM sys_user WHERE UserName = @UserName AND {ConnectionFactory.NotDeletedFilter}";
        var parameters = new List<DbParameter>
        {
            ConnectionFactory.CreateParameter("UserName", userName),
            CreateNotDeletedParameter()
        };
        return QuerySingleAsync(sql, parameters, cancellationToken);
    }

    public override Task<PagedResult<SysUser>> GetPagedAsync(PageQuery query, CancellationToken cancellationToken = default)
        => ExecuteAsync(nameof(GetPagedAsync), () => GetPagedCoreAsync(query, cancellationToken));

    private async Task<PagedResult<SysUser>> GetPagedCoreAsync(PageQuery query, CancellationToken cancellationToken)
    {
        const string operation = nameof(GetPagedAsync);
        var where = $"WHERE {ConnectionFactory.NotDeletedFilter}";
        var parameters = new List<DbParameter> { CreateNotDeletedParameter() };

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            where += " AND (UserName LIKE @Keyword OR DisplayName LIKE @Keyword OR Email LIKE @Keyword)";
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

        var items = new List<SysUser>();
        await using (var dataCommand = connection.CreateCommand())
        {
            dataCommand.CommandText = pagedSql;
            foreach (var parameter in parameters)
                dataCommand.Parameters.Add(ConnectionFactory.CreateParameter(parameter.ParameterName.TrimStart('@'), parameter.Value));

            await using var reader = await ExecuteReaderAsync($"{operation}:Query", dataCommand, cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                items.Add(MapEntity(reader));
        }

        return new PagedResult<SysUser>
        {
            Items = items,
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
    }

    protected override SysUser MapEntity(DbDataReader reader)
        => new()
        {
            Id = GetValue<long>(reader, "Id"),
            UserName = GetValue<string>(reader, "UserName"),
            PasswordHash = GetValue<string>(reader, "PasswordHash"),
            DisplayName = reader["DisplayName"] as string,
            Email = reader["Email"] as string,
            AvatarUrl = reader["AvatarUrl"] as string,
            IsActive = GetValue<bool>(reader, "IsActive"),
            CreatedAt = GetValue<DateTime>(reader, "CreatedAt"),
            UpdatedAt = reader["UpdatedAt"] as DateTime?,
            IsDeleted = GetValue<bool>(reader, "IsDeleted")
        };

    protected override (string Sql, List<DbParameter> Parameters) BuildInsertCommand(SysUser entity)
    {
        const string sql = """
            INSERT INTO sys_user (UserName, PasswordHash, DisplayName, Email, AvatarUrl, IsActive, CreatedAt, IsDeleted)
            VALUES (@UserName, @PasswordHash, @DisplayName, @Email, @AvatarUrl, @IsActive, @CreatedAt, @IsDeleted)
            """;

        var parameters = new List<DbParameter>
        {
            ConnectionFactory.CreateParameter("UserName", entity.UserName),
            ConnectionFactory.CreateParameter("PasswordHash", entity.PasswordHash),
            ConnectionFactory.CreateParameter("DisplayName", entity.DisplayName),
            ConnectionFactory.CreateParameter("Email", entity.Email),
            ConnectionFactory.CreateParameter("AvatarUrl", entity.AvatarUrl),
            ConnectionFactory.CreateParameter("IsActive", entity.IsActive),
            ConnectionFactory.CreateParameter("CreatedAt", entity.CreatedAt),
            ConnectionFactory.CreateParameter("IsDeleted", entity.IsDeleted)
        };

        return (sql, parameters);
    }

    protected override (string Sql, List<DbParameter> Parameters) BuildUpdateCommand(SysUser entity)
    {
        var sql = $"""
            UPDATE sys_user
            SET UserName = @UserName,
                PasswordHash = @PasswordHash,
                DisplayName = @DisplayName,
                Email = @Email,
                AvatarUrl = @AvatarUrl,
                IsActive = @IsActive,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id AND {ConnectionFactory.NotDeletedFilter}
            """;

        var parameters = new List<DbParameter>
        {
            ConnectionFactory.CreateParameter("UserName", entity.UserName),
            ConnectionFactory.CreateParameter("PasswordHash", entity.PasswordHash),
            ConnectionFactory.CreateParameter("DisplayName", entity.DisplayName),
            ConnectionFactory.CreateParameter("Email", entity.Email),
            ConnectionFactory.CreateParameter("AvatarUrl", entity.AvatarUrl),
            ConnectionFactory.CreateParameter("IsActive", entity.IsActive),
            ConnectionFactory.CreateParameter("UpdatedAt", entity.UpdatedAt),
            ConnectionFactory.CreateParameter("Id", entity.Id),
            CreateNotDeletedParameter()
        };

        return (sql, parameters);
    }
}

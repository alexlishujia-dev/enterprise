using EnterprisePlatform.Core.Entities;

using EnterprisePlatform.Repository.Abstractions;

using EnterprisePlatform.Utils.Logging;

using System.Data.Common;



namespace EnterprisePlatform.Repository.Repositories;



public interface ISysMenuRepository

{

    Task<IReadOnlyList<SysMenu>> GetAllActiveAsync(CancellationToken cancellationToken = default);



    Task<IReadOnlyList<SysPermission>> GetAllPermissionsAsync(CancellationToken cancellationToken = default);



    Task<IReadOnlyList<string>> GetPermissionCodesByUserIdAsync(long userId, CancellationToken cancellationToken = default);



    Task<IReadOnlyList<SysMenu>> GetAccessibleMenusByUserIdAsync(long userId, CancellationToken cancellationToken = default);



    Task<IReadOnlyList<long>> GetPermissionIdsByRoleIdAsync(long roleId, CancellationToken cancellationToken = default);



    Task AssignPermissionsToRoleAsync(long roleId, IEnumerable<long> permissionIds, CancellationToken cancellationToken = default);

    Task<long> InsertMenuAsync(long? parentId, string menuCode, string menuName, string? path, string? icon, int sortOrder, CancellationToken cancellationToken = default);

    Task<long> InsertPermissionAsync(long menuId, string permissionCode, string permissionName, string? description, CancellationToken cancellationToken = default);

}



public sealed class SysMenuRepository : ISysMenuRepository

{

    private readonly IDbConnectionFactory _connectionFactory;

    private readonly IFileLogWriter _fileLogWriter;



    public SysMenuRepository(IDbConnectionFactory connectionFactory, IFileLogWriter fileLogWriter)

    {

        _connectionFactory = connectionFactory;

        _fileLogWriter = fileLogWriter;

    }



    public async Task<IReadOnlyList<SysMenu>> GetAllActiveAsync(CancellationToken cancellationToken = default)

    {

        const string sql = """

            SELECT Id, ParentId, MenuCode, MenuName, Path, Icon, SortOrder, IsActive

            FROM sys_menu

            WHERE IsActive = TRUE

            ORDER BY SortOrder, Id

            """;



        return await QueryMenusAsync("GetAllActive", sql, null, cancellationToken);

    }



    public async Task<IReadOnlyList<SysPermission>> GetAllPermissionsAsync(CancellationToken cancellationToken = default)

    {

        const string sql = """

            SELECT Id, MenuId, PermissionCode, PermissionName, Description

            FROM sys_permission

            ORDER BY MenuId, Id

            """;



        return await QueryPermissionsAsync("GetAllPermissions", sql, null, cancellationToken);

    }



    public async Task<IReadOnlyList<string>> GetPermissionCodesByUserIdAsync(long userId, CancellationToken cancellationToken = default)

    {

        const string sql = """

            SELECT DISTINCT p.PermissionCode

            FROM sys_permission p

            INNER JOIN sys_role_permission rp ON rp.PermissionId = p.Id

            INNER JOIN sys_user_role ur ON ur.RoleId = rp.RoleId

            INNER JOIN sys_role r ON r.Id = ur.RoleId

            WHERE ur.UserId = @UserId

              AND r.IsDeleted = FALSE

              AND r.IsActive = TRUE

            ORDER BY p.PermissionCode

            """;



        var parameters = new List<DbParameter>

        {

            _connectionFactory.CreateParameter("UserId", userId)

        };



        var codes = new List<string>();

        await using var connection = _connectionFactory.CreateConnection();

        await using var command = connection.CreateCommand();

        command.CommandText = sql;

        foreach (var parameter in parameters)

            command.Parameters.Add(parameter);



        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))

            codes.Add(reader.GetString(reader.GetOrdinal("PermissionCode")));



        return codes;

    }



    public async Task<IReadOnlyList<SysMenu>> GetAccessibleMenusByUserIdAsync(long userId, CancellationToken cancellationToken = default)

    {

        const string sql = """

            SELECT DISTINCT m.Id, m.ParentId, m.MenuCode, m.MenuName, m.Path, m.Icon, m.SortOrder, m.IsActive

            FROM sys_menu m

            INNER JOIN sys_permission p ON p.MenuId = m.Id

            INNER JOIN sys_role_permission rp ON rp.PermissionId = p.Id

            INNER JOIN sys_user_role ur ON ur.RoleId = rp.RoleId

            INNER JOIN sys_role r ON r.Id = ur.RoleId

            WHERE ur.UserId = @UserId

              AND m.IsActive = TRUE

              AND r.IsDeleted = FALSE

              AND r.IsActive = TRUE

              AND p.PermissionCode LIKE '%:view'

            ORDER BY m.SortOrder, m.Id

            """;



        var parameters = new List<DbParameter>

        {

            _connectionFactory.CreateParameter("UserId", userId)

        };



        return await QueryMenusAsync("GetAccessibleMenusByUserId", sql, parameters, cancellationToken);

    }



    public async Task<IReadOnlyList<long>> GetPermissionIdsByRoleIdAsync(long roleId, CancellationToken cancellationToken = default)

    {

        const string sql = """

            SELECT PermissionId

            FROM sys_role_permission

            WHERE RoleId = @RoleId

            ORDER BY PermissionId

            """;



        var parameters = new List<DbParameter>

        {

            _connectionFactory.CreateParameter("RoleId", roleId)

        };



        var ids = new List<long>();

        await using var connection = _connectionFactory.CreateConnection();

        await using var command = connection.CreateCommand();

        command.CommandText = sql;

        foreach (var parameter in parameters)

            command.Parameters.Add(parameter);



        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))

            ids.Add(reader.GetInt64(reader.GetOrdinal("PermissionId")));



        return ids;

    }



    public Task AssignPermissionsToRoleAsync(long roleId, IEnumerable<long> permissionIds, CancellationToken cancellationToken = default)

        => AssignPermissionsCoreAsync(roleId, permissionIds, cancellationToken);



    private async Task AssignPermissionsCoreAsync(long roleId, IEnumerable<long> permissionIds, CancellationToken cancellationToken)

    {

        const string operation = nameof(AssignPermissionsToRoleAsync);

        await using var connection = _connectionFactory.CreateConnection();

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);



        try

        {

            await using (var deleteCommand = connection.CreateCommand())

            {

                deleteCommand.Transaction = transaction;

                deleteCommand.CommandText = "DELETE FROM sys_role_permission WHERE RoleId = @RoleId";

                deleteCommand.Parameters.Add(_connectionFactory.CreateParameter("RoleId", roleId));

                await deleteCommand.ExecuteNonQueryAsync(cancellationToken);

            }



            foreach (var permissionId in permissionIds.Distinct())

            {

                await using var insertCommand = connection.CreateCommand();

                insertCommand.Transaction = transaction;

                insertCommand.CommandText = """

                    INSERT INTO sys_role_permission (RoleId, PermissionId)

                    VALUES (@RoleId, @PermissionId)

                    """;

                insertCommand.Parameters.Add(_connectionFactory.CreateParameter("RoleId", roleId));

                insertCommand.Parameters.Add(_connectionFactory.CreateParameter("PermissionId", permissionId));

                await insertCommand.ExecuteNonQueryAsync(cancellationToken);

            }



            await transaction.CommitAsync(cancellationToken);

        }

        catch (Exception ex) when (ex is not OperationCanceledException)

        {

            await transaction.RollbackAsync(cancellationToken);

            ExceptionLogger.Log(_fileLogWriter, "Repository", "sys_role_permission", operation, ex);

            throw;

        }

    }



    private async Task<IReadOnlyList<SysMenu>> QueryMenusAsync(

        string operation,

        string sql,

        List<DbParameter>? parameters,

        CancellationToken cancellationToken)

    {

        var items = new List<SysMenu>();

        await using var connection = _connectionFactory.CreateConnection();

        await using var command = connection.CreateCommand();

        command.CommandText = sql;

        if (parameters is not null)

        {

            foreach (var parameter in parameters)

                command.Parameters.Add(parameter);

        }



        try

        {

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))

            {

                items.Add(new SysMenu

                {

                    Id = reader.GetInt64(reader.GetOrdinal("Id")),

                    ParentId = reader.IsDBNull(reader.GetOrdinal("ParentId")) ? null : reader.GetInt64(reader.GetOrdinal("ParentId")),

                    MenuCode = reader.GetString(reader.GetOrdinal("MenuCode")),

                    MenuName = reader.GetString(reader.GetOrdinal("MenuName")),

                    Path = reader["Path"] as string,

                    Icon = reader["Icon"] as string,

                    SortOrder = reader.GetInt32(reader.GetOrdinal("SortOrder")),

                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))

                });

            }

        }

        catch (Exception ex)

        {

            ExceptionLogger.Log(_fileLogWriter, "Repository", "sys_menu", operation, ex);

            throw;

        }



        return items;

    }



    private async Task<IReadOnlyList<SysPermission>> QueryPermissionsAsync(

        string operation,

        string sql,

        List<DbParameter>? parameters,

        CancellationToken cancellationToken)

    {

        var items = new List<SysPermission>();

        await using var connection = _connectionFactory.CreateConnection();

        await using var command = connection.CreateCommand();

        command.CommandText = sql;

        if (parameters is not null)

        {

            foreach (var parameter in parameters)

                command.Parameters.Add(parameter);

        }



        try

        {

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))

            {

                items.Add(new SysPermission

                {

                    Id = reader.GetInt64(reader.GetOrdinal("Id")),

                    MenuId = reader.GetInt64(reader.GetOrdinal("MenuId")),

                    PermissionCode = reader.GetString(reader.GetOrdinal("PermissionCode")),

                    PermissionName = reader.GetString(reader.GetOrdinal("PermissionName")),

                    Description = reader["Description"] as string

                });

            }

        }

        catch (Exception ex)

        {

            ExceptionLogger.Log(_fileLogWriter, "Repository", "sys_permission", operation, ex);

            throw;

        }



        return items;

    }

    public async Task<long> InsertMenuAsync(
        long? parentId,
        string menuCode,
        string menuName,
        string? path,
        string? icon,
        int sortOrder,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO sys_menu (ParentId, MenuCode, MenuName, Path, Icon, SortOrder, IsActive)
            VALUES (@ParentId, @MenuCode, @MenuName, @Path, @Icon, @SortOrder, TRUE)
            """;

        await using var connection = _connectionFactory.CreateConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.Add(_connectionFactory.CreateParameter("ParentId", parentId ?? (object)DBNull.Value));
        command.Parameters.Add(_connectionFactory.CreateParameter("MenuCode", menuCode));
        command.Parameters.Add(_connectionFactory.CreateParameter("MenuName", menuName));
        command.Parameters.Add(_connectionFactory.CreateParameter("Path", path ?? (object)DBNull.Value));
        command.Parameters.Add(_connectionFactory.CreateParameter("Icon", icon ?? (object)DBNull.Value));
        command.Parameters.Add(_connectionFactory.CreateParameter("SortOrder", sortOrder));
        await command.ExecuteNonQueryAsync(cancellationToken);

        command.Parameters.Clear();
        command.CommandText = _connectionFactory.GetLastInsertIdSql("sys_menu", "Id");
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(result);
    }

    public async Task<long> InsertPermissionAsync(
        long menuId,
        string permissionCode,
        string permissionName,
        string? description,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO sys_permission (MenuId, PermissionCode, PermissionName, Description)
            VALUES (@MenuId, @PermissionCode, @PermissionName, @Description)
            """;

        await using var connection = _connectionFactory.CreateConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.Add(_connectionFactory.CreateParameter("MenuId", menuId));
        command.Parameters.Add(_connectionFactory.CreateParameter("PermissionCode", permissionCode));
        command.Parameters.Add(_connectionFactory.CreateParameter("PermissionName", permissionName));
        command.Parameters.Add(_connectionFactory.CreateParameter("Description", description ?? (object)DBNull.Value));
        await command.ExecuteNonQueryAsync(cancellationToken);

        command.Parameters.Clear();
        command.CommandText = _connectionFactory.GetLastInsertIdSql("sys_permission", "Id");
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(result);
    }

}



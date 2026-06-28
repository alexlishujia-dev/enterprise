using EnterprisePlatform.Core.Enums;
using EnterprisePlatform.Core.Options;
using EnterprisePlatform.Repository.Abstractions;
using Microsoft.Extensions.Options;

namespace EnterprisePlatform.Api.Infrastructure;

/// <summary>
/// 应用启动时初始化数据库表结构（PostgreSQL 默认，其他库可替换为迁移脚本）。
/// </summary>
public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;

        var scripts = GetScripts(options.Provider);
        await using var connection = factory.CreateConnection();

        foreach (var script in scripts)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = script;
            await command.ExecuteNonQueryAsync();
        }

        foreach (var migration in GetSchemaMigrations(options.Provider))
        {
            await using var command = connection.CreateCommand();
            command.CommandText = migration;
            try
            {
                await command.ExecuteNonQueryAsync();
            }
            catch
            {
                // SQLite 等不支持 IF NOT EXISTS 时忽略重复列错误
            }
        }

        await DataSeeder.SeedAsync(services);
    }

    private static string[] GetSchemaMigrations(DatabaseProvider provider)
        => provider switch
        {
            DatabaseProvider.SqlServer =>
            [
                "IF COL_LENGTH('sys_user', 'AvatarUrl') IS NULL ALTER TABLE sys_user ADD AvatarUrl NVARCHAR(512) NULL;"
            ],
            DatabaseProvider.MySql =>
            [
                "ALTER TABLE sys_user ADD COLUMN IF NOT EXISTS AvatarUrl VARCHAR(512) NULL;"
            ],
            DatabaseProvider.PostgreSql =>
            [
                "ALTER TABLE sys_user ADD COLUMN IF NOT EXISTS AvatarUrl VARCHAR(512) NULL;"
            ],
            _ =>
            [
                "ALTER TABLE sys_user ADD COLUMN AvatarUrl TEXT NULL;"
            ]
        };

    private static string[] GetScripts(DatabaseProvider provider)
        => provider switch
        {
            DatabaseProvider.SqlServer => [SqlServerScript],
            DatabaseProvider.MySql => [MySqlScript],
            DatabaseProvider.PostgreSql => [PostgreSqlScript],
            _ => [SqliteScript]
        };

    private const string SqlServerScript = """
        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='sys_user' AND xtype='U')
        CREATE TABLE sys_user (
            Id BIGINT IDENTITY(1,1) PRIMARY KEY,
            UserName NVARCHAR(64) NOT NULL,
            PasswordHash NVARCHAR(256) NOT NULL,
            DisplayName NVARCHAR(64) NULL,
            Email NVARCHAR(128) NULL,
            IsActive BIT NOT NULL DEFAULT 1,
            CreatedAt DATETIME2 NOT NULL,
            UpdatedAt DATETIME2 NULL,
            IsDeleted BIT NOT NULL DEFAULT 0
        );
        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IX_sys_user_UserName')
        CREATE UNIQUE INDEX IX_sys_user_UserName ON sys_user(UserName) WHERE IsDeleted = 0;

        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='sys_role' AND xtype='U')
        CREATE TABLE sys_role (
            Id BIGINT IDENTITY(1,1) PRIMARY KEY,
            RoleCode NVARCHAR(64) NOT NULL,
            RoleName NVARCHAR(64) NOT NULL,
            Description NVARCHAR(256) NULL,
            IsActive BIT NOT NULL DEFAULT 1,
            CreatedAt DATETIME2 NOT NULL,
            UpdatedAt DATETIME2 NULL,
            IsDeleted BIT NOT NULL DEFAULT 0
        );
        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IX_sys_role_RoleCode')
        CREATE UNIQUE INDEX IX_sys_role_RoleCode ON sys_role(RoleCode) WHERE IsDeleted = 0;

        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='sys_user_role' AND xtype='U')
        CREATE TABLE sys_user_role (
            Id BIGINT IDENTITY(1,1) PRIMARY KEY,
            UserId BIGINT NOT NULL,
            RoleId BIGINT NOT NULL,
            CreatedAt DATETIME2 NOT NULL
        );
        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IX_sys_user_role_UserId_RoleId')
        CREATE UNIQUE INDEX IX_sys_user_role_UserId_RoleId ON sys_user_role(UserId, RoleId);

        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='sys_log' AND xtype='U')
        CREATE TABLE sys_log (
            Id BIGINT IDENTITY(1,1) PRIMARY KEY,
            UserId BIGINT NULL,
            UserName NVARCHAR(64) NULL,
            Module NVARCHAR(64) NOT NULL,
            Action NVARCHAR(32) NOT NULL,
            HttpMethod NVARCHAR(16) NOT NULL,
            RequestPath NVARCHAR(512) NOT NULL,
            IpAddress NVARCHAR(64) NULL,
            RequestBody NVARCHAR(2000) NULL,
            StatusCode INT NOT NULL,
            DurationMs BIGINT NOT NULL,
            CreatedAt DATETIME2 NOT NULL,
            UpdatedAt DATETIME2 NULL,
            IsDeleted BIT NOT NULL DEFAULT 0
        );

        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='sys_menu' AND xtype='U')
        CREATE TABLE sys_menu (
            Id BIGINT IDENTITY(1,1) PRIMARY KEY,
            ParentId BIGINT NULL,
            MenuCode NVARCHAR(64) NOT NULL,
            MenuName NVARCHAR(64) NOT NULL,
            Path NVARCHAR(256) NULL,
            Icon NVARCHAR(64) NULL,
            SortOrder INT NOT NULL DEFAULT 0,
            IsActive BIT NOT NULL DEFAULT 1
        );
        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IX_sys_menu_MenuCode')
        CREATE UNIQUE INDEX IX_sys_menu_MenuCode ON sys_menu(MenuCode);

        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='sys_permission' AND xtype='U')
        CREATE TABLE sys_permission (
            Id BIGINT IDENTITY(1,1) PRIMARY KEY,
            MenuId BIGINT NOT NULL,
            PermissionCode NVARCHAR(128) NOT NULL,
            PermissionName NVARCHAR(64) NOT NULL,
            Description NVARCHAR(256) NULL
        );
        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IX_sys_permission_Code')
        CREATE UNIQUE INDEX IX_sys_permission_Code ON sys_permission(PermissionCode);

        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='sys_role_permission' AND xtype='U')
        CREATE TABLE sys_role_permission (
            RoleId BIGINT NOT NULL,
            PermissionId BIGINT NOT NULL,
            PRIMARY KEY (RoleId, PermissionId)
        );
        """;

    private const string MySqlScript = """
        CREATE TABLE IF NOT EXISTS sys_user (
            Id BIGINT AUTO_INCREMENT PRIMARY KEY,
            UserName VARCHAR(64) NOT NULL,
            PasswordHash VARCHAR(256) NOT NULL,
            DisplayName VARCHAR(64) NULL,
            Email VARCHAR(128) NULL,
            IsActive TINYINT(1) NOT NULL DEFAULT 1,
            CreatedAt DATETIME NOT NULL,
            UpdatedAt DATETIME NULL,
            IsDeleted TINYINT(1) NOT NULL DEFAULT 0,
            UNIQUE KEY UX_sys_user_UserName (UserName, IsDeleted)
        );
        CREATE TABLE IF NOT EXISTS sys_role (
            Id BIGINT AUTO_INCREMENT PRIMARY KEY,
            RoleCode VARCHAR(64) NOT NULL,
            RoleName VARCHAR(64) NOT NULL,
            Description VARCHAR(256) NULL,
            IsActive TINYINT(1) NOT NULL DEFAULT 1,
            CreatedAt DATETIME NOT NULL,
            UpdatedAt DATETIME NULL,
            IsDeleted TINYINT(1) NOT NULL DEFAULT 0,
            UNIQUE KEY UX_sys_role_RoleCode (RoleCode, IsDeleted)
        );
        CREATE TABLE IF NOT EXISTS sys_user_role (
            Id BIGINT AUTO_INCREMENT PRIMARY KEY,
            UserId BIGINT NOT NULL,
            RoleId BIGINT NOT NULL,
            CreatedAt DATETIME NOT NULL,
            UNIQUE KEY UX_sys_user_role (UserId, RoleId)
        );
        CREATE TABLE IF NOT EXISTS sys_log (
            Id BIGINT AUTO_INCREMENT PRIMARY KEY,
            UserId BIGINT NULL,
            UserName VARCHAR(64) NULL,
            Module VARCHAR(64) NOT NULL,
            Action VARCHAR(32) NOT NULL,
            HttpMethod VARCHAR(16) NOT NULL,
            RequestPath VARCHAR(512) NOT NULL,
            IpAddress VARCHAR(64) NULL,
            RequestBody VARCHAR(2000) NULL,
            StatusCode INT NOT NULL,
            DurationMs BIGINT NOT NULL,
            CreatedAt DATETIME NOT NULL,
            UpdatedAt DATETIME NULL,
            IsDeleted TINYINT(1) NOT NULL DEFAULT 0
        );
        CREATE TABLE IF NOT EXISTS sys_menu (
            Id BIGINT AUTO_INCREMENT PRIMARY KEY,
            ParentId BIGINT NULL,
            MenuCode VARCHAR(64) NOT NULL,
            MenuName VARCHAR(64) NOT NULL,
            Path VARCHAR(256) NULL,
            Icon VARCHAR(64) NULL,
            SortOrder INT NOT NULL DEFAULT 0,
            IsActive TINYINT(1) NOT NULL DEFAULT 1,
            UNIQUE KEY UX_sys_menu_MenuCode (MenuCode)
        );
        CREATE TABLE IF NOT EXISTS sys_permission (
            Id BIGINT AUTO_INCREMENT PRIMARY KEY,
            MenuId BIGINT NOT NULL,
            PermissionCode VARCHAR(128) NOT NULL,
            PermissionName VARCHAR(64) NOT NULL,
            Description VARCHAR(256) NULL,
            UNIQUE KEY UX_sys_permission_Code (PermissionCode)
        );
        CREATE TABLE IF NOT EXISTS sys_role_permission (
            RoleId BIGINT NOT NULL,
            PermissionId BIGINT NOT NULL,
            PRIMARY KEY (RoleId, PermissionId)
        );
        """;

    private const string PostgreSqlScript = """
        CREATE TABLE IF NOT EXISTS sys_user (
            Id BIGSERIAL PRIMARY KEY,
            UserName VARCHAR(64) NOT NULL,
            PasswordHash VARCHAR(256) NOT NULL,
            DisplayName VARCHAR(64) NULL,
            Email VARCHAR(128) NULL,
            IsActive BOOLEAN NOT NULL DEFAULT TRUE,
            CreatedAt TIMESTAMPTZ NOT NULL,
            UpdatedAt TIMESTAMPTZ NULL,
            IsDeleted BOOLEAN NOT NULL DEFAULT FALSE
        );
        CREATE UNIQUE INDEX IF NOT EXISTS UX_sys_user_UserName ON sys_user(UserName) WHERE IsDeleted = FALSE;

        CREATE TABLE IF NOT EXISTS sys_role (
            Id BIGSERIAL PRIMARY KEY,
            RoleCode VARCHAR(64) NOT NULL,
            RoleName VARCHAR(64) NOT NULL,
            Description VARCHAR(256) NULL,
            IsActive BOOLEAN NOT NULL DEFAULT TRUE,
            CreatedAt TIMESTAMPTZ NOT NULL,
            UpdatedAt TIMESTAMPTZ NULL,
            IsDeleted BOOLEAN NOT NULL DEFAULT FALSE
        );
        CREATE UNIQUE INDEX IF NOT EXISTS UX_sys_role_RoleCode ON sys_role(RoleCode) WHERE IsDeleted = FALSE;

        CREATE TABLE IF NOT EXISTS sys_user_role (
            Id BIGSERIAL PRIMARY KEY,
            UserId BIGINT NOT NULL,
            RoleId BIGINT NOT NULL,
            CreatedAt TIMESTAMPTZ NOT NULL
        );
        CREATE UNIQUE INDEX IF NOT EXISTS UX_sys_user_role ON sys_user_role(UserId, RoleId);

        CREATE TABLE IF NOT EXISTS sys_log (
            Id BIGSERIAL PRIMARY KEY,
            UserId BIGINT NULL,
            UserName VARCHAR(64) NULL,
            Module VARCHAR(64) NOT NULL,
            Action VARCHAR(32) NOT NULL,
            HttpMethod VARCHAR(16) NOT NULL,
            RequestPath VARCHAR(512) NOT NULL,
            IpAddress VARCHAR(64) NULL,
            RequestBody VARCHAR(2000) NULL,
            StatusCode INT NOT NULL,
            DurationMs BIGINT NOT NULL,
            CreatedAt TIMESTAMPTZ NOT NULL,
            UpdatedAt TIMESTAMPTZ NULL,
            IsDeleted BOOLEAN NOT NULL DEFAULT FALSE
        );

        CREATE TABLE IF NOT EXISTS sys_menu (
            Id BIGSERIAL PRIMARY KEY,
            ParentId BIGINT NULL,
            MenuCode VARCHAR(64) NOT NULL,
            MenuName VARCHAR(64) NOT NULL,
            Path VARCHAR(256) NULL,
            Icon VARCHAR(64) NULL,
            SortOrder INT NOT NULL DEFAULT 0,
            IsActive BOOLEAN NOT NULL DEFAULT TRUE
        );
        CREATE UNIQUE INDEX IF NOT EXISTS UX_sys_menu_MenuCode ON sys_menu(MenuCode);

        CREATE TABLE IF NOT EXISTS sys_permission (
            Id BIGSERIAL PRIMARY KEY,
            MenuId BIGINT NOT NULL,
            PermissionCode VARCHAR(128) NOT NULL,
            PermissionName VARCHAR(64) NOT NULL,
            Description VARCHAR(256) NULL
        );
        CREATE UNIQUE INDEX IF NOT EXISTS UX_sys_permission_Code ON sys_permission(PermissionCode);

        CREATE TABLE IF NOT EXISTS sys_role_permission (
            RoleId BIGINT NOT NULL,
            PermissionId BIGINT NOT NULL,
            PRIMARY KEY (RoleId, PermissionId)
        );
        """;

    private const string SqliteScript = """
        CREATE TABLE IF NOT EXISTS sys_user (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            UserName TEXT NOT NULL,
            PasswordHash TEXT NOT NULL,
            DisplayName TEXT NULL,
            Email TEXT NULL,
            IsActive INTEGER NOT NULL DEFAULT 1,
            CreatedAt TEXT NOT NULL,
            UpdatedAt TEXT NULL,
            IsDeleted INTEGER NOT NULL DEFAULT 0
        );
        CREATE UNIQUE INDEX IF NOT EXISTS UX_sys_user_UserName ON sys_user(UserName) WHERE IsDeleted = 0;

        CREATE TABLE IF NOT EXISTS sys_role (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            RoleCode TEXT NOT NULL,
            RoleName TEXT NOT NULL,
            Description TEXT NULL,
            IsActive INTEGER NOT NULL DEFAULT 1,
            CreatedAt TEXT NOT NULL,
            UpdatedAt TEXT NULL,
            IsDeleted INTEGER NOT NULL DEFAULT 0
        );
        CREATE UNIQUE INDEX IF NOT EXISTS UX_sys_role_RoleCode ON sys_role(RoleCode) WHERE IsDeleted = 0;

        CREATE TABLE IF NOT EXISTS sys_user_role (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            UserId INTEGER NOT NULL,
            RoleId INTEGER NOT NULL,
            CreatedAt TEXT NOT NULL
        );
        CREATE UNIQUE INDEX IF NOT EXISTS UX_sys_user_role ON sys_user_role(UserId, RoleId);

        CREATE TABLE IF NOT EXISTS sys_log (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            UserId INTEGER NULL,
            UserName TEXT NULL,
            Module TEXT NOT NULL,
            Action TEXT NOT NULL,
            HttpMethod TEXT NOT NULL,
            RequestPath TEXT NOT NULL,
            IpAddress TEXT NULL,
            RequestBody TEXT NULL,
            StatusCode INTEGER NOT NULL,
            DurationMs INTEGER NOT NULL,
            CreatedAt TEXT NOT NULL,
            UpdatedAt TEXT NULL,
            IsDeleted INTEGER NOT NULL DEFAULT 0
        );

        CREATE TABLE IF NOT EXISTS sys_menu (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            ParentId INTEGER NULL,
            MenuCode TEXT NOT NULL,
            MenuName TEXT NOT NULL,
            Path TEXT NULL,
            Icon TEXT NULL,
            SortOrder INTEGER NOT NULL DEFAULT 0,
            IsActive INTEGER NOT NULL DEFAULT 1
        );
        CREATE UNIQUE INDEX IF NOT EXISTS UX_sys_menu_MenuCode ON sys_menu(MenuCode);

        CREATE TABLE IF NOT EXISTS sys_permission (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            MenuId INTEGER NOT NULL,
            PermissionCode TEXT NOT NULL,
            PermissionName TEXT NOT NULL,
            Description TEXT NULL
        );
        CREATE UNIQUE INDEX IF NOT EXISTS UX_sys_permission_Code ON sys_permission(PermissionCode);

        CREATE TABLE IF NOT EXISTS sys_role_permission (
            RoleId INTEGER NOT NULL,
            PermissionId INTEGER NOT NULL,
            PRIMARY KEY (RoleId, PermissionId)
        );
        """;
}

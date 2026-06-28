-- EnterprisePlatform PostgreSQL 表结构
-- 数据库：enterprise
-- 用法：psql -U platform -d enterprise -f 01_schema.sql

CREATE TABLE IF NOT EXISTS sys_user (
    Id           BIGSERIAL PRIMARY KEY,
    UserName     VARCHAR(64)  NOT NULL,
    PasswordHash VARCHAR(256) NOT NULL,
    DisplayName  VARCHAR(64)  NULL,
    Email        VARCHAR(128) NULL,
    AvatarUrl    VARCHAR(512) NULL,
    IsActive     BOOLEAN      NOT NULL DEFAULT TRUE,
    CreatedAt    TIMESTAMPTZ  NOT NULL,
    UpdatedAt    TIMESTAMPTZ  NULL,
    IsDeleted    BOOLEAN      NOT NULL DEFAULT FALSE
);

CREATE UNIQUE INDEX IF NOT EXISTS UX_sys_user_UserName
    ON sys_user (UserName)
    WHERE IsDeleted = FALSE;

CREATE TABLE IF NOT EXISTS sys_role (
    Id          BIGSERIAL PRIMARY KEY,
    RoleCode    VARCHAR(64)  NOT NULL,
    RoleName    VARCHAR(64)  NOT NULL,
    Description VARCHAR(256) NULL,
    IsActive    BOOLEAN      NOT NULL DEFAULT TRUE,
    CreatedAt   TIMESTAMPTZ  NOT NULL,
    UpdatedAt   TIMESTAMPTZ  NULL,
    IsDeleted   BOOLEAN      NOT NULL DEFAULT FALSE
);

CREATE UNIQUE INDEX IF NOT EXISTS UX_sys_role_RoleCode
    ON sys_role (RoleCode)
    WHERE IsDeleted = FALSE;

CREATE TABLE IF NOT EXISTS sys_user_role (
    Id        BIGSERIAL PRIMARY KEY,
    UserId    BIGINT      NOT NULL,
    RoleId    BIGINT      NOT NULL,
    CreatedAt TIMESTAMPTZ NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS UX_sys_user_role
    ON sys_user_role (UserId, RoleId);

CREATE TABLE IF NOT EXISTS sys_log (
    Id          BIGSERIAL PRIMARY KEY,
    UserId      BIGINT        NULL,
    UserName    VARCHAR(64)   NULL,
    Module      VARCHAR(64)   NOT NULL,
    Action      VARCHAR(32)   NOT NULL,
    HttpMethod  VARCHAR(16)   NOT NULL,
    RequestPath VARCHAR(512)  NOT NULL,
    IpAddress   VARCHAR(64)   NULL,
    RequestBody VARCHAR(2000) NULL,
    StatusCode  INT           NOT NULL,
    DurationMs  BIGINT        NOT NULL,
    CreatedAt   TIMESTAMPTZ   NOT NULL,
    UpdatedAt   TIMESTAMPTZ   NULL,
    IsDeleted   BOOLEAN       NOT NULL DEFAULT FALSE
);

CREATE TABLE IF NOT EXISTS sys_menu (
    Id           BIGSERIAL PRIMARY KEY,
    ParentId     BIGINT       NULL,
    MenuCode     VARCHAR(64)  NOT NULL,
    MenuName     VARCHAR(64)  NOT NULL,
    Path         VARCHAR(256) NULL,
    Icon         VARCHAR(64)  NULL,
    SortOrder    INT          NOT NULL DEFAULT 0,
    IsActive     BOOLEAN      NOT NULL DEFAULT TRUE
);

CREATE UNIQUE INDEX IF NOT EXISTS UX_sys_menu_MenuCode
    ON sys_menu (MenuCode);

CREATE TABLE IF NOT EXISTS sys_permission (
    Id              BIGSERIAL PRIMARY KEY,
    MenuId          BIGINT       NOT NULL,
    PermissionCode  VARCHAR(128) NOT NULL,
    PermissionName  VARCHAR(64)  NOT NULL,
    Description     VARCHAR(256) NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS UX_sys_permission_Code
    ON sys_permission (PermissionCode);

CREATE TABLE IF NOT EXISTS sys_role_permission (
    RoleId       BIGINT NOT NULL,
    PermissionId BIGINT NOT NULL,
    PRIMARY KEY (RoleId, PermissionId)
);

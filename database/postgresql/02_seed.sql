-- EnterprisePlatform PostgreSQL 初始数据
-- 默认管理员：admin / Admin@123
-- 用法：psql -U platform -d enterprise -f 02_seed.sql

CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- 管理员角色
INSERT INTO sys_role (RoleCode, RoleName, Description, IsActive, CreatedAt, IsDeleted)
SELECT 'admin', '管理员', '系统管理员，拥有全部权限', TRUE, NOW() AT TIME ZONE 'UTC', FALSE
WHERE NOT EXISTS (
    SELECT 1 FROM sys_role WHERE RoleCode = 'admin' AND IsDeleted = FALSE
);

-- 管理员用户（密码哈希算法与 HashHelper.Sha256 一致：SHA256("Admin@123:admin")）
INSERT INTO sys_user (UserName, PasswordHash, DisplayName, Email, IsActive, CreatedAt, IsDeleted)
SELECT
    'admin',
    encode(digest('Admin@123:admin', 'sha256'), 'hex'),
    '系统管理员',
    'admin@example.com',
    TRUE,
    NOW() AT TIME ZONE 'UTC',
    FALSE
WHERE NOT EXISTS (
    SELECT 1 FROM sys_user WHERE UserName = 'admin' AND IsDeleted = FALSE
);

-- 为管理员分配 admin 角色
INSERT INTO sys_user_role (UserId, RoleId, CreatedAt)
SELECT u.Id, r.Id, NOW() AT TIME ZONE 'UTC'
FROM sys_user u
CROSS JOIN sys_role r
WHERE u.UserName = 'admin'
  AND u.IsDeleted = FALSE
  AND r.RoleCode = 'admin'
  AND r.IsDeleted = FALSE
  AND NOT EXISTS (
      SELECT 1 FROM sys_user_role ur
      WHERE ur.UserId = u.Id AND ur.RoleId = r.Id
  );

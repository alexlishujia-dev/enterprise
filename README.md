# EnterprisePlatform

ASP.NET Core 10 + ADO.NET 多数据库企业级通用框架（与具体业务系统解耦）。

## 架构分层

```
Api        → 路由、校验、中间件、统一响应
Service    → 业务逻辑、事务、DTO 转换
Repository → ADO.NET + DbFactory + 通用 CRUD
Core       → 实体、DTO、枚举、配置
Utils      → 加密、扩展、工具
```

## 数据库

默认使用 **PostgreSQL**。在 `appsettings.json` 中可切换 `Database:Provider`：

| Provider     | 值          |
|--------------|-------------|
| PostgreSQL   | `PostgreSql`（默认）|
| SQL Server   | `SqlServer` |
| MySQL        | `MySql`     |
| SQLite       | `Sqlite`    |

默认连接字符串：

```
Host=localhost;Port=5432;Database=enterprise;Username=platform;Password=platform123
```

数据库脚本位于 `database/postgresql/`：

| 文件 | 说明 |
|------|------|
| `01_schema.sql` | 建表语句 |
| `02_seed.sql` | 初始管理员与角色 |
| `init.sql` | 一键执行（需 psql `\ir` 支持） |

手动初始化：

```bash
psql -U platform -d enterprise -f database/postgresql/01_schema.sql
psql -U platform -d enterprise -f database/postgresql/02_seed.sql
```

Docker 首次启动时会自动执行 `01_schema.sql` 与 `02_seed.sql`。

## 快速启动

### 方式一：Docker（推荐）

```bash
docker compose up -d --build
```

- API：http://localhost:8080/health
- PostgreSQL：localhost:5432

### 方式二：本地开发

先启动 PostgreSQL（可仅启动数据库容器）：

```bash
docker compose up -d postgres
```

再运行 API：

```bash
cd src/EnterprisePlatform.Api
dotnet restore
dotnet run
```

浏览器访问：https://localhost:7088/swagger

默认管理员账号（首次启动自动创建）：

| 用户名 | 密码 | 角色 |
|--------|------|------|
| admin  | Admin@123 | admin |

## Docker 部署

```bash
docker compose up -d --build
```

环境变量可通过 `docker-compose.yml` 覆盖，常用配置：

| 变量 | 说明 |
|------|------|
| `Database__Provider` | 数据库类型 |
| `Database__ConnectionString` | 连接字符串 |
| `Jwt__SecretKey` | JWT 签名密钥（生产环境务必修改） |
| `ASPNETCORE_ENVIRONMENT` | 运行环境 |

## Swagger 泛型响应修复

框架使用 `ApiResult<T>` 统一包装响应。Swashbuckle 默认无法正确生成泛型 Schema，已通过以下方式解决：

- `ApiResultSchemaFilter` — 展开 `ApiResult<T>` 的 code/message/data/traceId 结构
- `PagedResultSchemaFilter` — 展开分页结果 Schema
- `CustomSchemaIds` — 使用完整类型名避免 SchemaId 冲突

## 默认接口

| 方法 | 路径 | 说明 |
|------|------|------|
| POST | `/api/auth/login` | 登录获取 JWT |
| GET  | `/api/sysuser` | 分页查询用户 |
| POST | `/api/sysuser` | 创建用户 |
| GET  | `/api/sysuser/{id}` | 获取用户 |
| DELETE | `/api/sysuser/{id}` | 删除用户 |
| GET  | `/api/sysrole` | 分页查询角色 |
| POST | `/api/sysrole` | 创建角色 |
| GET  | `/api/sysrole/{id}` | 获取角色 |
| PUT  | `/api/sysrole/{id}` | 更新角色 |
| DELETE | `/api/sysrole/{id}` | 删除角色 |
| GET  | `/api/sysrole/user/{userId}` | 获取用户角色 |
| PUT  | `/api/sysrole/user/{userId}` | 分配用户角色 |
| GET  | `/api/syslog` | 分页查询操作日志 |
| GET  | `/health` | 健康检查 |

## 扩展新业务模块

1. 在 `Core` 添加 Entity / DTO
2. 在 `Repository` 继承 `BaseRepository<T>` 实现 ADO.NET 仓储
3. 在 `Service` 编写业务服务并注册 DI
4. 在 `Api` 添加 Controller，返回 `ApiResult<T>`

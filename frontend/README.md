# EnterprisePlatform Web 前端

基于 **Angular 19 + NG-ZORRO** 的自适应管理后台，分层对齐现有 ASP.NET Core + ADO.NET 后端。

## 技术栈

- Angular 19（Standalone + 懒加载路由）
- NG-ZORRO 19（企业 UI）
- TypeScript 5.7（严格模式）
- RxJS + HttpClient

## 目录结构

```
src/app/
├── core/           # 模型、拦截器、路由守卫、常量（对应后端 Core）
├── shared/         # 工具、指令、通用组件（对应 Utils）
├── api/            # HTTP 接口封装（对应 Repository）
├── service/        # 前端业务层（对应 Service）
└── pages/          # 页面与布局（对应 Api 视图）
    ├── layout/
    ├── login/
    ├── dashboard/
    └── system/
```

## 已对接后端能力

| 模块 | API | 说明 |
|------|-----|------|
| 认证 | `POST /api/Auth/login` | JWT Token |
| 用户 | `/api/SysUser` | 分页、创建、删除 |
| 角色 | `/api/SysRole` | 分页、CRUD、用户角色 |
| 日志 | `/api/SysLog` | 分页查询 |

## 全局拦截器

1. **requestSignInterceptor** — HMAC 请求验签（与后端 `RequestSign` 对齐）
2. **authInterceptor** — 自动携带 `Bearer Token`
3. **apiResultInterceptor** — 统一解包 `ApiResult<T>`，处理 401 跳转登录

## 环境要求

- Node.js 20+
- npm 10+

## 启动步骤

```bash
# 1. 安装依赖
cd frontend
npm install

# 2. 确保后端 API 已运行（默认 https://localhost:7088）

# 3. 启动前端（代理到后端）
npm start
```

浏览器访问：http://localhost:4200

默认账号：`admin` / `Admin@123`

## 配置说明

`src/environments/environment.ts`：

| 配置项 | 开发默认值 | 说明 |
|--------|-----------|------|
| `apiBaseUrl` | `/api` | 通过 proxy 转发到后端 |
| `requestSignEnabled` | `false` | 开发环境关闭验签，便于调试 |
| `requestSignSecretKey` | 与后端一致 | 生产环境需开启验签 |

生产构建：

```bash
npm run build:prod
```

## 响应式断点

- 手机：< 768px（侧栏折叠、表格横向滚动）
- 平板：768px ~ 1200px
- 桌面：≥ 1200px

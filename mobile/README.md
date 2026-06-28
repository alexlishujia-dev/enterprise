# EnterprisePlatform 移动端（UniApp）

对接现有 **ASP.NET Core + ADO.NET** 后端，与 Angular PC 前端共享同一套 API、JWT 认证、`ApiResult` 响应格式与权限体系。

支持：**Android / iOS / 微信小程序 / H5**（HBuilderX 或 CLI 编译）。

## 目录结构

```
mobile/
├── pages/           # 页面（login、工作台、用户管理）
├── components/      # 通用组件（卡片、列表项、空状态）
├── client/          # 接口层（勿用 api/apis 命名，H5 代理 /api 会误匹配）
├── service/         # 业务层
├── model/           # 数据模型（对齐 Core DTO）
├── store/           # Pinia 全局状态
├── utils/           # 工具（storage、auth、request-sign、format）
├── config/env.js    # 环境配置
├── manifest.json    # 多端打包配置
└── pages.json       # 路由与页面配置
```

## 快速开始

### 方式一：HBuilderX（推荐）

1. 安装 [HBuilderX](https://www.dcloud.io/hbuilderx.html)（选择 App 开发版）
2. **文件 → 导入 → 从本地目录导入**，选择 `mobile` 目录
3. 在项目根目录终端执行 `npm install`（安装 uView Plus、Pinia、crypto-js）
4. 修改 `config/env.js`：
   - **H5 调试**：保持 `apiBaseUrl: '/api'`，用 HBuilder「运行 → 运行到浏览器」
   - **App / 小程序真机**：改为局域网 API 地址，例如：
     ```js
     apiBaseUrl: 'http://192.168.1.100:5089/api',
     assetBaseUrl: 'http://192.168.1.100:5089'
     ```
5. 确保后端 API 已启动（默认 `https://localhost:7088` 或 `http://localhost:5089`）
6. HBuilderX：**运行 → 运行到浏览器 / 运行到手机模拟器 / 运行到小程序模拟器**

> 编译由 HBuilderX 内置 UniApp 引擎完成，无需单独安装 `@dcloudio` CLI 包。

### 方式二：UniApp CLI（可选）

若需命令行构建，请先用 HBuilderX 创建 Vue3 空项目，再将本目录代码合并，或参考 [uni-app 官方 CLI 文档](https://uniapp.dcloud.net.cn/quickstart-cli.html) 初始化后覆盖业务代码。

## 后端对接说明

| 能力 | 移动端实现 | 后端 |
|------|-----------|------|
| 登录 | `POST /api/Auth/login` | `AuthController` |
| 当前用户 | `GET /api/Auth/me` | JWT + 菜单/权限 |
| 用户列表 | `GET /api/SysUser` | 需 `system.users:view` |
| 头像 | `assetBaseUrl` + `/uploads/...` | 静态文件 |
| 响应格式 | `ApiResult { code, message, data }` | 全局统一 |
| 请求签名 | `config/env.js` → `requestSignEnabled` | `RequestSignatureMiddleware` |

默认测试账号：`admin` / `Admin@123`

## 与 PC 前端对齐

- 分层：`pages` → `service` → `api/request` → 后端（同 Angular 的 component → service → api-http）
- Token 存储键：`ep_access_token`、`ep_current_user`
- 权限码：如 `system.users:view`，与后端 `RequirePermission` 一致
- 401 自动清 session 并跳转登录页

## 扩展新业务模块

1. 在 `client/` 新增 `xxx.api.js`
2. 在 `service/` 新增 `xxx.service.js`
3. 在 `pages/` 新增页面并在 `pages.json` 注册
4. 模型字段与 `EnterprisePlatform.Core/Dtos` 保持一致

## 打包发布

- **Android APK**：HBuilderX → 发行 → 原生 App-云打包 / 本地打包
- **iOS**：需 Apple 开发者证书，HBuilderX 导出 Xcode 工程
- **微信小程序**：配置 `manifest.json` 中 `mp-weixin.appid` 后上传

## 注意事项

- App / 小程序无法使用 `localhost`，请改用电脑局域网 IP
- 生产环境开启 `requestSignEnabled` 时需与 `appsettings.Production.json` 密钥一致
- 微信小程序需在后台配置 request 合法域名

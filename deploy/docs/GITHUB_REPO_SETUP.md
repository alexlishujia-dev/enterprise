# GitHub 仓库创建指南（前端 + 后端 API）

> 针对 **EnterprisePlatform** 项目：`frontend/`（Angular）+ `src/`（.NET API）+ `mobile/`（UniApp）+ `deploy/`（部署脚本）。  
> 配合 [DEPLOYMENT_GUIDE.md 第 4 章](./DEPLOYMENT_GUIDE.md#4-git-与-github-环境搭建) 使用。

---

## 一、先看清你的项目结构

你的项目 **不是只有一个前端或只有一个后端**，而是一个 **多模块单体仓库**：

```
EnterprisePlatform/                 ← 项目根目录（建议作为 Git 仓库根）
├── EnterprisePlatform.sln          ← .NET 解决方案（后端入口）
├── src/
│   ├── EnterprisePlatform.Api/     ← 后端 API（ASP.NET Core）
│   ├── EnterprisePlatform.Core/
│   ├── EnterprisePlatform.Service/
│   ├── EnterprisePlatform.Repository/
│   └── EnterprisePlatform.Utils/
├── frontend/                       ← PC  Web 前端（Angular 19）
├── mobile/                         ← 移动端（UniApp）
└── deploy/                         ← CI/CD 部署脚本
```

| 目录 | 技术 | 作用 |
|------|------|------|
| `src/EnterprisePlatform.Api` | ASP.NET Core | 提供 REST API（如 `/api/Auth/login`） |
| `frontend` | Angular | PC 管理后台，调用后端 API |
| `mobile` | UniApp | 手机端，调用同一套 API |
| `deploy` | Shell/K8s | Jenkins + K3s 部署配置 |

**前端和后端的关系：** 前端/mobile 通过 HTTP 请求访问后端 API，代码在同一个项目里，但运行时是 **两个（或多个）独立服务**。

---

## 二、两种建仓库方式怎么选？

### 方式对比

| | **方式 A：一个仓库（Monorepo）** | **方式 B：多个仓库（Multi-repo）** |
|--|--------------------------------|-----------------------------------|
| 结构 | 根目录一个仓库，含 frontend + src + mobile | `xxx-api`、`xxx-web`、`xxx-mobile` 各一个仓库 |
| 适合 | **小团队、校务系统、你当前项目** | 大团队、前后端完全独立发布 |
| push | 一次 push 提交所有改动 | 改前端/后端要分别 push |
| Jenkins | 一个 Pipeline，按目录分别构建 | 每个仓库一条 Pipeline |
| 版本对齐 | 天然一致（同一 commit） | 需自己管理 API 与前端版本匹配 |
| 推荐度 | ✅ **强烈推荐** | 可选 |

### 结论（针对你的项目）

> **推荐：创建一个仓库 `EnterprisePlatform`，把整个项目根目录 push 上去。**

原因：
- 后端 API 和 Angular/mobile 本来就在同一解决方案里协作
- 改 API 接口时，前端/mobile 往往要一起改，放同一仓库更方便
- 部署文档、Jenkinsfile 可以放在根目录或 `deploy/`，统一管理

---

## 三、方式 A：一个仓库（推荐，详细步骤）

### 步骤 A1：登录 GitHub 并创建空仓库

1. 打开 https://github.com 并登录  
2. 右上角 **+** → **New repository**  
3. 填写：

| 字段 | 填什么 | 说明 |
|------|--------|------|
| Repository name | `EnterprisePlatform` | 与本地文件夹名一致 |
| Description | 校务系统企业平台 | 可选 |
| Visibility | **Private**（推荐） | 校务系统代码建议私有 |
| Add a README file | ❌ **不要勾选** | 本地已有代码 |
| Add .gitignore | ❌ **不要选** | 项目根目录已有 `.gitignore` |
| Choose a license | None | 按需 |

4. 点击 **Create repository**  
5. 记下页面上的地址：

```
HTTPS: https://github.com/你的用户名/EnterprisePlatform.git
SSH:   git@github.com:你的用户名/EnterprisePlatform.git
```

---

### 步骤 A2：完善根目录 `.gitignore`（重要）

在 push 前，确保 **不要把依赖包和编译产物** 传上去（体积大、无意义）。

项目根目录 `EnterprisePlatform/.gitignore` 建议包含：

```gitignore
# .NET 后端
bin/
obj/
.vs/
*.user
*.db
*.db-shm
*.db-wal
appsettings.*.local.json
logs/
uploads/

# 前端 Angular
frontend/node_modules/
frontend/dist/
frontend/.angular/

# 移动端 UniApp
mobile/node_modules/
mobile/dist/
mobile/unpackage/
mobile/.hbuilderx/

# 通用
.DS_Store
*.log
.env
.env.local
```

> `frontend/`、`mobile/` 下各有 `.gitignore`，根目录这份是 **整仓统一规则**，避免误提交。

---

### 步骤 A3：Windows 本地初始化 Git 并关联远程仓库

在 **PowerShell** 中（路径按你的实际目录）：

```powershell
cd "D:\hj\校务系统\EnterprisePlatform"

# 若从未初始化过 Git
git init
git branch -M main

# 关联 GitHub 远程仓库（SSH 方式，推荐）
git remote add origin git@github.com:你的用户名/EnterprisePlatform.git

# 若已存在 origin 但地址不对，先删除再添加：
# git remote remove origin
# git remote add origin git@github.com:你的用户名/EnterprisePlatform.git

# 查看远程地址是否正确
git remote -v
```

**HTTPS 方式（不用 SSH 时）：**

```powershell
git remote add origin https://github.com/你的用户名/EnterprisePlatform.git
```

---

### 步骤 A4：首次提交并 push

```powershell
cd "D:\hj\校务系统\EnterprisePlatform"

# 查看哪些文件会被提交（应看不到 node_modules、bin、obj）
git status

# 添加所有应跟踪的文件
git add .

# 再次确认：不应出现 frontend/node_modules、src/**/bin 等
git status

# 首次提交
git commit -m "Initial commit: 后端 API + Angular 前端 + UniApp 移动端 + 部署脚本"

# 推送到 GitHub
git push -u origin main
```

**首次 push 可能较慢**（不含 node_modules 的话通常几分钟内）。若提示输入凭据，HTTPS 方式密码处填 **GitHub Token**（不是登录密码）。

---

### 步骤 A5：在 GitHub 上验证仓库结构

浏览器打开 `https://github.com/你的用户名/EnterprisePlatform`，应能看到：

```
EnterprisePlatform/
├── EnterprisePlatform.sln
├── src/
│   └── EnterprisePlatform.Api/
├── frontend/
│   └── src/ ...
├── mobile/
│   └── pages/ ...
├── deploy/
└── .gitignore
```

**不应出现：** `frontend/node_modules/`、`src/**/bin/`、`mobile/unpackage/`。

---

### 步骤 A6：日常开发怎么 commit（前后端一起改时）

```powershell
# 例：同时改了 API 和 Angular 页面
git add src/EnterprisePlatform.Api/Controllers/AuthController.cs
git add frontend/src/app/...

git commit -m "feat: 登录接口增加验证码，前端同步调整"
git push
```

**习惯建议：**

| 改动范围 | commit 说明示例 |
|----------|-----------------|
| 仅后端 API | `feat(api): 用户列表增加分页` |
| 仅 PC 前端 | `feat(web): 用户管理页表格优化` |
| 仅移动端 | `feat(mobile): 登录页 UI 调整` |
| 前后端联动 | `feat: 新增通知模块（API + Web + Mobile）` |

---

## 四、方式 B：多个仓库（可选，详细步骤）

若公司要求 **前后端分仓库**，可按下面拆分：

| 仓库名 | 包含目录 | 说明 |
|--------|----------|------|
| `EnterprisePlatform-Api` | `src/` + `EnterprisePlatform.sln` + 根 `.gitignore` 后端部分 | 仅 .NET |
| `EnterprisePlatform-Web` | `frontend/` 整个目录 | 仅 Angular |
| `EnterprisePlatform-Mobile` | `mobile/` 整个目录 | 仅 UniApp |

### 步骤 B1：创建 3 个空仓库

在 GitHub 分别创建（均 **不要** 勾选 Initialize README）：

- `EnterprisePlatform-Api`
- `EnterprisePlatform-Web`
- `EnterprisePlatform-Mobile`

### 步骤 B2：后端 API 仓库

```powershell
# 新建临时目录或使用 git subtree/filter-repo（进阶）
# 简单做法：复制后端相关文件到新文件夹

mkdir D:\projects\EnterprisePlatform-Api
cd D:\projects\EnterprisePlatform-Api

# 从原项目复制：EnterprisePlatform.sln、src/、.gitignore（后端规则）
# 然后：
git init
git branch -M main
git remote add origin git@github.com:你的用户名/EnterprisePlatform-Api.git
git add .
git commit -m "Initial commit: ASP.NET Core API"
git push -u origin main
```

### 步骤 B3：前端 Web 仓库

```powershell
cd "D:\hj\校务系统\EnterprisePlatform\frontend"

git init
git branch -M main
git remote add origin git@github.com:你的用户名/EnterprisePlatform-Web.git
git add .
git commit -m "Initial commit: Angular Web"
git push -u origin main
```

### 步骤 B4：移动端仓库

```powershell
cd "D:\hj\校务系统\EnterprisePlatform\mobile"

git init
git branch -M main
git remote add origin git@github.com:你的用户名/EnterprisePlatform-Mobile.git
git add .
git commit -m "Initial commit: UniApp Mobile"
git push -u origin main
```

### 多仓库的 Jenkins 配置

需要 **3 条 Pipeline**（或 1 条 orchestration 流水线依次触发）：

| Jenkins 任务 | 拉取仓库 | 构建命令 |
|--------------|----------|----------|
| `api-deploy` | EnterprisePlatform-Api | `dotnet publish` → Docker → K3s |
| `web-deploy` | EnterprisePlatform-Web | `npm run build:prod` → Nginx 镜像 → K3s |
| `mobile-build` | EnterprisePlatform-Mobile | UniApp 打包（通常不走 K8s Web 部署） |

> **注意：** API 变更时，要手动或脚本触发 Web 流水线重新构建，否则前端可能仍指向旧接口。

---

## 五、自建 Git（Gitea / 公司 Git 服务器）怎么建？

若不用 github.com，而在 **自己服务器** 上建仓库，步骤类似，只是网页地址不同：

1. 浏览器打开 `http://服务器IP:3000`（Gitea）或 `https://git.公司域名`  
2. 登录 → **+** → **New Repository**  
3. 仓库名仍建议 `EnterprisePlatform`  
4. 本地 remote 改为：

```powershell
git remote add origin http://192.168.1.100:3000/你的用户名/EnterprisePlatform.git
# 或
git remote add origin git@git.company.com:你的用户名/EnterprisePlatform.git
```

5. push 方式与 GitHub 相同

**同机部署 Gitea + Jenkins 时：** Webhook 填 Jenkins 内网地址即可，比公网 GitHub 更简单。

---

## 六、与 Jenkins CI/CD 如何配合（Monorepo）

推荐在 **仓库根目录** 放 `Jenkinsfile`，按变更目录决定构建什么：

```groovy
// 简化示例：push 后 Jenkins 自动判断
stage('Detect Changes') {
    // 若 src/ 有变更 → 构建并部署 API
    // 若 frontend/ 有变更 → 构建并部署 Web
    // 若 deploy/ 有变更 → 仅更新 K8s 配置
}
```

**首次部署最小方案（先只部署 API）：**

1. 仓库根目录有 `Dockerfile`（针对 `EnterprisePlatform.Api`）  
2. Jenkins Pipeline 指向 **同一个** `EnterprisePlatform` 仓库  
3. K8s 先部署 API；前端可第二阶段再加 Nginx 静态站镜像  

详见 [DEPLOYMENT_GUIDE.md 第 13 章](./DEPLOYMENT_GUIDE.md#13-第九步创建-cicd-流水线)。

---

## 七、常见问题

### Q1：前端和后端要建两个 GitHub 账号吗？

**不要。** 一个 GitHub 账号下可以建多个仓库；一个仓库里也可以同时有 frontend 和 src。

### Q2：push 很慢或失败，提示文件太大？

检查是否误提交了 `node_modules` 或 `bin/`：

```powershell
git rm -r --cached frontend/node_modules
git rm -r --cached src/EnterprisePlatform.Api/bin
git commit -m "fix: 移除误提交的依赖和编译产物"
```

并在 `.gitignore` 中补全规则后重新 push。

### Q3：已有仓库，如何改成 Monorepo？

若之前只 push 了 frontend，把后端代码复制进同一目录，再：

```powershell
git add src/ EnterprisePlatform.sln
git commit -m "chore: 合并后端 API 到同一仓库"
git push
```

### Q4：Private 仓库 Jenkins 怎么拉代码？

在 Jenkins 配置 **Credentials**：
- HTTPS：GitHub Token（`github-token`）  
- SSH：jenkins 用户的 SSH 公钥添加到 GitHub  

见 [DEPLOYMENT_GUIDE 第 11 章](./DEPLOYMENT_GUIDE.md#11-第七步github-与-jenkins-集成)。

### Q5：mobile 要不要和 frontend 分开？

**一般不需要。** 三者共用同一套 API，放同一 Monorepo 最省事。只有移动端单独团队、单独发版周期时才考虑独立仓库。

### Q6：我是用 Google（谷歌）登录 GitHub 的，需要重新注册吗？

**不需要。** 用 Google 登录时，GitHub 会为你 **创建或绑定一个 GitHub 账号**，和普通邮箱注册是同一个平台，功能完全一样。

你需要确认这几件事：

| 事项 | 怎么做 | 为什么 |
|------|--------|--------|
| 记住 GitHub 用户名 | 右上角头像 → Settings → 左侧最上方 **Account** | clone 地址里是 `@用户名`，不是 Google 邮箱 |
| 确认邮箱 | Settings → Emails | 接收通知；部分功能依赖已验证邮箱 |
| Jenkins / Git 凭据 | 用 **Personal Access Token** 或 **SSH 密钥** | 命令行和 Jenkins **不能**用 Google 登录，必须用 Token/SSH |
| （可选）设置密码 | Settings → Password | 备用登录方式，非必须 |

**本地 `git push` 时：**

- **HTTPS**：用户名填 GitHub 用户名，密码处填 **Token**（不是 Google 密码）
- **SSH**：配置 SSH 密钥后无需每次登录（推荐）

**创建 Token：** GitHub → Settings → Developer settings → Personal access tokens → Generate new token (classic)，勾选 `repo` 等权限。详见 [DEPLOYMENT_GUIDE 第 11 章](./DEPLOYMENT_GUIDE.md#11-第七步github-与-jenkins-集成)。

---

## 八、检查清单（创建仓库完成后勾选）

- [ ] GitHub 上已创建 `EnterprisePlatform` 空仓库（未勾选 README）  
- [ ] 根目录 `.gitignore` 已包含 node_modules、bin、obj  
- [ ] 本地 `git remote -v` 指向正确地址  
- [ ] `git push` 成功，GitHub 上能看到 `src/`、`frontend/`、`mobile/`、`deploy/`  
- [ ] GitHub 上 **没有** `node_modules`、 `bin/` 目录  
- [ ] （可选）已为 Jenkins 准备 Token 或 SSH 密钥  

---

## 九、下一步

仓库创建并 push 成功后：

1. 继续 [DEPLOYMENT_GUIDE.md 第 5 章](./DEPLOYMENT_GUIDE.md#5-第一步连接服务器并上传部署文件) — 上传 deploy 到服务器  
2. 或 [STEP_BY_STEP.md 步骤 3](./STEP_BY_STEP.md#步骤-3连接服务器上传部署脚本) — 逐步理解后续环节  

---

**推荐路径回顾：** 一个 GitHub 仓库 `EnterprisePlatform` → 根目录包含后端 API + Angular 前端 + mobile + deploy → Jenkins 从同一仓库拉代码构建部署。

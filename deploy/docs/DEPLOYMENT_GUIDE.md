# GitHub + Jenkins + K3s 详细部署手册

> **适用环境：** Ubuntu 20.04 LTS · 4GB 内存 · 国内网络  
> **预计耗时：** 首次部署约 1～2 小时（含 Jenkins 插件安装）  
> **文档版本：** 2026-06

**新手阅读路线：**

1. [BEGINNER_GUIDE.md](./BEGINNER_GUIDE.md) — 理解各组件有什么用  
2. **[STEP_BY_STEP.md](./STEP_BY_STEP.md) — 每一步详细说明（强烈推荐）**  
3. 本文 — 按章节动手操作  
4. [CHECKLIST.md](./CHECKLIST.md) — 逐项勾选  

---

## 目录

0. [新手必读：整体是做什么的](#0-新手必读整体是做什么的)（**不熟悉请先读**）
   - 更详细的逐步说明 → [STEP_BY_STEP.md](./STEP_BY_STEP.md)
1. [你将搭建什么](#1-你将搭建什么)
2. [部署前准备清单](#2-部署前准备清单)
3. [整体流程一览](#3-整体流程一览)
4. [Git 与 GitHub 环境搭建](#4-git-与-github-环境搭建)
5. [第一步：连接服务器并上传部署文件](#5-第一步连接服务器并上传部署文件)
6. [第二步：系统初始化](#6-第二步系统初始化)
7. [第三步：安装 Docker](#7-第三步安装-docker)
8. [第四步：安装 K3s（Kubernetes）](#8-第四步安装-k3skubernetes)
9. [第五步：安装 Jenkins](#9-第五步安装-jenkins)
10. [第六步：初始化 Jenkins 控制台](#10-第六步初始化-jenkins-控制台)
11. [第七步：GitHub 与 Jenkins 集成](#11-第七步github-与-jenkins-集成)
12. [第八步：配置阿里云镜像仓库](#12-第八步配置阿里云镜像仓库)
13. [第九步：创建 CI/CD 流水线](#13-第九步创建-cicd-流水线)
14. [第十步：首次部署验证](#14-第十步首次部署验证)
15. [权限配置说明](#15-权限配置说明)
16. [日常运维命令](#16-日常运维命令)
17. [常见问题 FAQ](#17-常见问题-faq)
18. [附录](#18-附录)

---

## 0. 新手必读：整体是做什么的

> **如果你对 Git、Jenkins、Docker、K8s 不太了解，请先阅读 [新手必读专文](./BEGINNER_GUIDE.md)**，里面有通俗解释、生活类比、完整流程图和名词表。  
> **每一步在干什么、为什么、成功是什么样？** 请看 **[逐步详解手册 STEP_BY_STEP.md](./STEP_BY_STEP.md)**。  
> 下面是一版精简摘要，方便边部署边回顾。

### 0.1 一句话总结

你在搭一条 **自动流水线**：**改代码 → push → 自动编译打包 → 自动部署上线**，不用每次手动拷 jar、重启服务。

### 0.2 各组件干什么（精简版）

| 组件 | 一句话 | 类比 |
|------|--------|------|
| **Git** | 管理代码版本的工具 | 带历史记录的「另存为」 |
| **GitHub / Gitea** | 存放代码的远程仓库 | 项目代码的网盘 |
| **Jenkins** | 自动化干活的调度中心 | 收到通知就按步骤编译、部署的机器人 |
| **Docker** | 把应用打成标准镜像 | 统一规格的「餐盒」 |
| **阿里云 ACR** | 存放 Docker 镜像 | 餐盒的临时仓库 |
| **K3s** | 运行和管理容器的平台 | 真正对外提供服务的「餐厅」 |
| **Webhook** | Git 通知 Jenkins 的方式 | 按门铃：「代码更新了！」 |

### 0.3 一次 push 之后发生了什么

```
你在电脑上 git push
    → Git 仓库收到代码，Webhook 通知 Jenkins
    → Jenkins 拉代码 → mvn 编译 → docker build 打镜像 → push 到阿里云
    → Jenkins 执行 kubectl，让 K3s 用新镜像替换旧版本
    → 用户访问到的是新版本
```

### 0.4 文档各章在整条链路中的位置

| 章节 | 在流水线中的角色 |
|------|------------------|
| 第 4 章 Git/GitHub | 准备「代码源头」 |
| 第 6～8 章 系统/Docker/K3s | 准备「运行环境」 |
| 第 9～10 章 Jenkins | 准备「自动化工人」 |
| 第 11 章 GitHub 集成 | 让 push 能自动触发 Jenkins |
| 第 12 章 阿里云 ACR | 准备「镜像中转站」 |
| 第 13 章 Pipeline | 告诉 Jenkins 具体做哪些步骤 |
| 第 14 章 验证 | 确认整条链路通了 |

---

## 1. 你将搭建什么

完成后，你的服务器将具备以下能力：

```
开发者 push 代码到 GitHub
        │
        ▼ (Webhook 自动触发)
    Jenkins 拉代码 → 编译 → 构建 Docker 镜像 → 推送到镜像仓库
        │
        ▼ (kubectl 命令)
    K3s 集群滚动更新应用 Pod
```

| 组件 | 作用 | 访问地址 |
|------|------|----------|
| Git / GitHub / Gitea | 存放源代码，触发 Webhook | 见第 4 章 |
| Jenkins | 自动编译、打包、部署（CI/CD） | `http://<服务器IP>:8080` |
| Docker | 把应用打成可移植的镜像 | 命令行 |
| 阿里云 ACR | 存放镜像，供 K3s 拉取 | 阿里云控制台 |
| K3s | 运行应用的 Kubernetes 集群 | 命令行 `kubectl` |

**为何使用 K3s 而不是完整 Kubernetes？**  
4GB 内存的服务器同时跑 Jenkins + 完整 K8s 很容易内存不足（OOM）。K3s 是 Rancher 出品的轻量 K8s 发行版，单节点占用约 512MB～1GB，功能上满足 CI/CD 部署需求。

---

## 2. 部署前准备清单

在开始前，请逐项确认：

### 2.1 服务器要求

| 项目 | 要求 | 如何检查 |
|------|------|----------|
| 操作系统 | Ubuntu **20.04** LTS（64位） | `lsb_release -a` |
| CPU | ≥ 2 核 | `nproc` |
| 内存 | ≥ 4 GB | `free -h` |
| 磁盘 | ≥ 40 GB 可用 | `df -h` |
| 网络 | 能访问外网（GitHub、镜像源） | `ping -c 3 mirrors.aliyun.com` |
| 权限 | 有 root 或 sudo 权限 | `sudo whoami` |

> **云服务器用户：** 在安全组中放行端口 `22、80、443、8080、6443、8472/udp、10250`（脚本也会通过 UFW 配置）。

### 2.2 需要提前注册的账号

| 账号 | 用途 | 注册地址 |
|------|------|----------|
| GitHub | 代码仓库 + Webhook | https://github.com |
| 阿里云 | 容器镜像仓库 ACR（国内推镜像） | https://www.aliyun.com |

### 2.3 需要提前准备的信息（建议记录在记事本）

```
服务器 IP：___________________
SSH 用户名：________________（通常是 root 或 ubuntu）
GitHub 用户名：________________
GitHub 仓库地址：________________
阿里云镜像命名空间：________________
阿里云镜像仓库用户名/密码：________________
```

### 2.4 本地工具

- SSH 客户端（Windows 可用 PowerShell / PuTTY / MobaXterm）
- **Git for Windows**（必需，用于 push 代码到 GitHub，安装步骤见 [第 4.2 节](#42-安装-gitwindows-本地开发机)）
- 可选：GitHub CLI（`gh`，见 [第 4.8 节](#48-可选安装-github-cli)）

---

## 3. 整体流程一览

```
┌─────────────────────────────────────────────────────────────┐
│  阶段 0   Git 安装 + GitHub 注册/建仓库 + push 代码（第 4 章）  │
│  阶段 1   上传 deploy 目录到服务器                            │
│  阶段 2   bash 01-system-init.sh    → 系统源/swap/防火墙      │
│  阶段 3   bash 02-docker-install.sh → Docker + 镜像加速       │
│  阶段 4   bash 03-k3s-install.sh    → K3s + RBAC + 命名空间   │
│  阶段 5   bash 04-jenkins-install.sh → Jenkins + JVM 限制    │
│  阶段 6   bash 06-git-github-setup.sh → 服务器 Git/SSH 配置    │
│  阶段 7   浏览器打开 Jenkins → 安装插件 → 配置凭据             │
│  阶段 8   GitHub Token + Webhook + Jenkins 凭据（第 11 章）    │
│  阶段 9   阿里云 ACR 创建仓库                                 │
│  阶段 10  Jenkins 创建 Pipeline 任务                         │
│  阶段 11  push 代码 → 验证自动构建部署                         │
└─────────────────────────────────────────────────────────────┘
```

**重要：** 服务器脚本必须按 **01 → 02 → 03 → 04 → 06** 顺序执行；GitHub 仓库需在第 4 章提前或同步完成。

---

## 4. Git 与 GitHub 环境搭建

> 📖 **新手逐步详解：[STEP_BY_STEP 步骤 2](./STEP_BY_STEP.md#步骤-2git-与代码仓库)**  
> 📖 **前端+后端如何建仓库：[GITHUB_REPO_SETUP.md](./GITHUB_REPO_SETUP.md)**

> **本章是做什么的？** 安装 Git 工具，在 GitHub（或自建 Gitea）上创建代码仓库，并把项目代码 push 上去。  
> **为什么需要？** 这是整个流水线的「起点」——没有代码仓库，Jenkins 不知道拉什么、Webhook 也无法触发。  
> **不做会怎样？** 后续 Jenkins 无法 checkout，流水线第一步就会失败。

本章完成 **Git 工具安装**、**GitHub 账号与仓库创建**、**本地/服务器代码推送**，为后续 Jenkins CI/CD 提供代码源。

> **说明：** GitHub 是 SaaS 平台，无需在服务器上「安装 GitHub 服务」；需要安装的是 **Git 客户端**，并在 GitHub 网站创建远程仓库。

---

### 4.1 注册 GitHub 账号

1. 打开 https://github.com/signup
2. 填写邮箱、密码、用户名（Username，即 `@` 后面的名字，**不可随意更改**）
3. 完成邮箱验证
4. 登录后建议开启 **两步验证（2FA）**：Settings → Password and authentication → Two-factor authentication

**国内访问提示：**

| 现象 | 建议 |
|------|------|
| github.com 打开慢 | 多刷新几次；或使用稳定网络；开发机可配置 HTTP 代理 |
| `git clone` 超时 | 优先使用 **HTTPS + Token**（见 4.7 节），比 SSH 更易穿透网络 |
| 完全无法访问 | 检查 DNS；临时改用 Gitee 镜像同步到 GitHub（进阶） |

验证账号可用：

```bash
# 在本地或服务器执行
curl -I https://github.com
# 期望：HTTP/2 200 或 301
```

---

### 4.2 安装 Git（Windows 本地开发机）

若你在 Windows 上开发并 push 代码，需先安装 Git：

1. 下载 [Git for Windows](https://git-scm.com/download/win)（国内可搜「Git 淘宝镜像」获取安装包）
2. 安装时建议选项：
   - **Default branch name:** `main`
   - **PATH:** Git from the command line and also from 3rd-party software
   - **HTTPS:** Use native Windows Secure Channel library
3. 安装完成后打开 **PowerShell** 或 **Git Bash** 验证：

```powershell
git --version
# 期望：git version 2.x.x
```

配置全局身份（提交代码时显示的作者信息）：

```powershell
git config --global user.name "你的姓名或昵称"
git config --global user.email "你的GitHub注册邮箱"
git config --global init.defaultBranch main
git config --global core.autocrlf true
```

查看配置：

```powershell
git config --global --list
```

---

### 4.3 安装 Git（Ubuntu 服务器）

服务器上 Git 会在 **01-system-init.sh** 中自动安装。也可单独执行：

```bash
sudo apt update
sudo apt install -y git
git --version
```

Jenkins 安装完成后，运行 GitHub 专用配置脚本：

```bash
cd /opt/EnterprisePlatform/deploy
sudo bash scripts/06-git-github-setup.sh
```

脚本会完成：

- 检测 GitHub 网络连通性
- 为 `jenkins` 用户配置 `user.name` / `user.email`
- （可选）生成 SSH 密钥并输出公钥，供添加到 GitHub

---

### 4.4 创建 GitHub 仓库（含前端 + 后端 API）

> 📖 **前端 + 后端 + 移动端如何建仓库？** 详见专文 **[GITHUB_REPO_SETUP.md](./GITHUB_REPO_SETUP.md)**（含 Monorepo / 多仓库对比、逐步命令、检查清单）。

#### 4.4.1 本项目应怎么建？（重要）

你的 **EnterprisePlatform** 包含：

| 目录 | 内容 |
|------|------|
| `src/EnterprisePlatform.Api/` | 后端 REST API（ASP.NET Core） |
| `frontend/` | PC Web 前端（Angular） |
| `mobile/` | 移动端（UniApp） |
| `deploy/` | 部署脚本 |

**推荐：只建 1 个仓库 `EnterprisePlatform`**，把 **整个项目根目录** push 上去（Monorepo）。  
前端、后端、移动端在同一仓库，改 API 时前端可一起提交，Jenkins 也从同一地址拉代码。

```
❌ 不推荐新手：分别为 API、Web、Mobile 建 3 个仓库（除非公司强制要求）
✅ 推荐：一个仓库 EnterprisePlatform，内含 src/ + frontend/ + mobile/ + deploy/
```

#### 4.4.2 在 GitHub 网页创建空仓库

1. 登录 GitHub → 右上角 **+** → **New repository**
2. 填写：

| 字段 | 建议值 | 说明 |
|------|--------|------|
| Repository name | `EnterprisePlatform` | 与本地根目录名一致 |
| Description | 校务系统企业平台 | 可选 |
| Visibility | **Private**（推荐） | 校务代码建议私有 |
| Add a README file | ❌ **不要勾选** | 本地已有完整项目 |
| Add .gitignore | ❌ **选 None** | 根目录已有 `.gitignore` |
| Choose a license | None | 按需 |

3. 点击 **Create repository**
4. 记录仓库地址：

```
HTTPS: https://github.com/YOUR_USERNAME/EnterprisePlatform.git
SSH:   git@github.com:YOUR_USERNAME/EnterprisePlatform.git
```

#### 4.4.3 push 前确认 `.gitignore`

确保 **不要** 把 `node_modules`、`bin/`、`obj/` 传上去。根目录 `.gitignore` 应包含：

- `frontend/node_modules/`、`frontend/dist/`
- `mobile/node_modules/`、`mobile/unpackage/`
- `bin/`、`obj/`（.NET 编译产物）

检查命令：

```powershell
cd "D:\hj\校务系统\EnterprisePlatform"
git status
# 不应出现 node_modules 或 bin 目录
```

完整说明见 [GITHUB_REPO_SETUP.md 步骤 A2](./GITHUB_REPO_SETUP.md#步骤-a2完善根目录-gitignore重要)。

---

### 4.5 配置 SSH 密钥（推荐，免密 push/pull）

> 📖 **看不懂 SSH？** 请看 **[Windows SSH 密钥新手完全指南](./GITHUB_SSH_WINDOWS.md)**（逐步解释每条命令、每个提示是什么意思）。  
> 若觉得复杂，可跳过本节，直接用 [4.7 HTTPS + Token](#47-使用-https--personal-access-token备选)。

#### 4.5.1 Windows 本地生成密钥

在 PowerShell 或 Git Bash 中：

```powershell
# 生成 ED25519 密钥（推荐）
ssh-keygen -t ed25519 -C "你的GitHub邮箱" -f "$env:USERPROFILE\.ssh\id_ed25519_github"

# 一路回车（或设置 passphrase）
# 启动 ssh-agent 并添加密钥
Get-Service ssh-agent | Set-Service -StartupType Manual
Start-Service ssh-agent
ssh-add $env:USERPROFILE\.ssh\id_ed25519_github
```

配置 SSH 使用专用密钥连接 GitHub，创建/编辑 `%USERPROFILE%\.ssh\config`：

```
Host github.com
  HostName github.com
  User git
  IdentityFile ~/.ssh/id_ed25519_github
  StrictHostKeyChecking accept-new
```

查看公钥（复制全部内容）：

```powershell
Get-Content $env:USERPROFILE\.ssh\id_ed25519_github.pub
```

#### 4.5.2 将公钥添加到 GitHub

1. GitHub → 右上角头像 → **Settings**
2. 左侧 **SSH and GPG keys** → **New SSH key**
3. Title：`windows-dev` 或 `jenkins-server`
4. Key type：Authentication Key
5. Key：粘贴公钥内容（以 `ssh-ed25519` 开头）
6. 点击 **Add SSH key**

#### 4.5.3 测试 SSH 连接

```powershell
ssh -T git@github.com
# 期望：Hi YOUR_USERNAME! You've successfully authenticated...
```

#### 4.5.4 Jenkins 服务器 SSH 密钥

> 📖 **详细逐步说明：[GITHUB_SSH_JENKINS.md](./GITHUB_SSH_JENKINS.md)**（含脚本交互说明、手动配置、权限、FAQ）

**为什么 Windows 配了还要在服务器再配？**  
Jenkins 以 **`jenkins` 系统用户** 运行，拉代码时用的是 `/var/lib/jenkins/.ssh/` 里的密钥，**不会**使用你 Windows 电脑上的密钥。

**何时做：** 必须 **先** 执行完 `04-jenkins-install.sh`（创建 `jenkins` 用户），**再** 运行本脚本。

若出现 `unknown user jenkins` 或公钥文件不存在 → 说明 Jenkins 还没装，先装 Jenkins。

---

**快速步骤：**

```bash
cd /opt/enterprise/deploy
sudo bash scripts/06-git-github-setup.sh
```

| 脚本提问 | 你输入 |
|----------|--------|
| Git 用户名 | `jenkins` 或回车 |
| Git 邮箱 | `alexlishujia@gmail.com` |
| 是否为 jenkins 生成 SSH 密钥? | **`y`** |
| GitHub 用户名 | `alexlishujia-dev` |

脚本会输出 **公钥**（以 `ssh-ed25519` 开头）→ 复制到：  
**https://github.com/settings/keys → New SSH key**（Title: `jenkins-server`）

**若脚本已跑完、需要再次查看公钥，在服务器执行：**

```bash
sudo cat /var/lib/jenkins/.ssh/id_ed25519_github.pub
```

复制输出的 **整一行**，粘贴到 GitHub → Settings → SSH and GPG keys → New SSH key。

**验证：**

```bash
# ① SSH 认证
sudo -u jenkins ssh -T git@github.com
# 期望：Hi alexlishujia-dev! You've successfully authenticated...

# ② 能否访问仓库
sudo -u jenkins git ls-remote git@github.com:alexlishujia-dev/enterprise.git
# 期望：输出 commit hash 列表
```

**常见错误：**

| 现象 | 处理 |
|------|------|
| `jenkins 用户不存在` | 先执行 `04-jenkins-install.sh` |
| `Permission denied (publickey)` | 公钥加到 **SSH and GPG keys**，不是 Deploy keys |
| 不想用 SSH | 改用 HTTPS + Token，见 [4.7 节](#47-使用-https--personal-access-token备选) |

---

### 4.6 将本地项目推送到 GitHub（前端 + 后端一起）

#### 方式 A：Monorepo 首次 push（推荐）

在项目 **根目录**（含 `src/`、`frontend/`、`mobile/` 的那一层）：

```powershell
cd "D:\hj\校务系统\EnterprisePlatform"

git init
git branch -M main
git remote add origin git@github.com:YOUR_USERNAME/EnterprisePlatform.git

git add .
git status
# 确认：有 src/、frontend/、mobile/、deploy/
# 确认：没有 node_modules、bin、obj

git commit -m "Initial commit: API + Angular Web + UniApp Mobile + deploy"
git push -u origin main
```

push 成功后，在 GitHub 网页应看到：

```
EnterprisePlatform/
├── EnterprisePlatform.sln
├── src/EnterprisePlatform.Api/
├── frontend/
├── mobile/
└── deploy/
```

#### 方式 B：仅推送某一子目录（多仓库方案，一般不推荐）

若公司要求 API 与 Web **分仓库**，见 [GITHUB_REPO_SETUP.md 第四节](./GITHUB_REPO_SETUP.md#四方式-b多个仓库可选详细步骤)。

#### 方式 C：从 GitHub 克隆到本地（空仓库已开始）

```powershell
git clone git@github.com:YOUR_USERNAME/EnterprisePlatform.git
cd EnterprisePlatform
# 复制项目文件到此目录后 commit、push
```

#### 方式 C：服务器上 clone（用于手动部署测试）

```bash
cd /opt
sudo git clone git@github.com:YOUR_USERNAME/EnterprisePlatform.git
# 或使用 HTTPS（需 Token，见下节）
```

---

### 4.7 使用 HTTPS + Personal Access Token（备选）

若 SSH 不可用（公司网络限制等），使用 HTTPS：

#### 4.7.1 创建 Token

1. GitHub → Settings → Developer settings → Personal access tokens → **Tokens (classic)**
2. **Generate new token (classic)**
3. Note：`git-clone-push`
4. 勾选：`repo`（完整仓库权限）
5. 生成并**立即复制** Token（形如 `ghp_xxxxxxxx`）

#### 4.7.2 使用 Token clone / push

```powershell
# clone 时用户名填 GitHub 用户名，密码处粘贴 Token（不是登录密码）
git clone https://github.com/YOUR_USERNAME/EnterprisePlatform.git

# 已添加 remote 时，push 会提示输入凭据
git push origin main
```

Windows 凭据缓存（避免每次输入）：

```powershell
git config --global credential.helper manager
```

> Jenkins 集成同样使用该 Token，凭据 ID 设为 `github-token`（见第 11 章）。

---

### 4.8 （可选）安装 GitHub CLI

GitHub CLI (`gh`) 可在命令行管理仓库、PR、Webhook：

**Windows：**

```powershell
winget install GitHub.cli
gh auth login
# 选择 GitHub.com → HTTPS → Login with web browser
```

**Ubuntu 服务器：**

```bash
curl -fsSL https://cli.github.com/packages/githubcli-archive-keyring.gpg | sudo dd of=/usr/share/keyrings/githubcli-archive-keyring.gpg
echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main" | sudo tee /etc/apt/sources.list.d/github-cli.list > /dev/null
sudo apt update && sudo apt install gh -y
gh auth login
```

常用命令：

```bash
gh repo view
gh repo clone YOUR_USERNAME/EnterprisePlatform
gh pr list
```

---

### 4.9 验证 GitHub 环境就绪

在 push 代码成功后，确认：

| 检查项 | 命令/操作 | 期望结果 |
|--------|-----------|----------|
| 远程仓库有代码 | 浏览器打开 GitHub 仓库 | 能看到 `src/`、`frontend/`、`mobile/`、`deploy/` |
| 本地 remote 正确 | `git remote -v` | origin 指向你的仓库 |
| SSH 可用 | `ssh -T git@github.com` | 认证成功提示 |
| 服务器 jenkins 可拉代码 | `sudo -u jenkins git ls-remote git@github.com:USER/REPO.git` | 输出 commit hash |

**GitHub 环境就绪后**，继续 [第 5 章 连接服务器](#5-第一步连接服务器并上传部署文件) 部署 CI/CD。

---

## 5. 第一步：连接服务器并上传部署文件

> 📖 **新手逐步详解：[STEP_BY_STEP 步骤 3](./STEP_BY_STEP.md#步骤-3连接服务器上传部署脚本)**

### 5.1 SSH 登录服务器

```bash
# 将 YOUR_IP 替换为服务器公网 IP
ssh root@YOUR_IP
# 或
ssh ubuntu@YOUR_IP
```

首次登录成功后，更新一下基础包（可选）：

```bash
sudo apt update
```

### 5.2 获取 deploy 目录

**方式 A：从 GitHub 克隆整个项目（推荐）**

```bash
cd /opt
sudo git clone https://github.com/YOUR_ORG/EnterprisePlatform.git
cd EnterprisePlatform/deploy
```

**方式 B：仅上传 deploy 目录（无 Git 时）**

在本地 Windows PowerShell 中：

```powershell
scp -r "D:\hj\校务系统\EnterprisePlatform\deploy" root@YOUR_IP:/opt/
```

然后在服务器上：

```bash
cd /opt/deploy
```

### 5.3 确认文件完整

```bash
ls -la scripts/
# 应看到 01~06 脚本和 health-check.sh

ls -la configs/
# 应看到 apt-sources-cn.list、daemon.json、registries.yaml
```

### 5.4 赋予脚本执行权限

```bash
chmod +x scripts/*.sh
```

---

## 6. 第二步：系统初始化

> 📖 **新手逐步详解：[STEP_BY_STEP 步骤 4](./STEP_BY_STEP.md#步骤-4系统初始化)**

> **本章是做什么的？** 给 Ubuntu 系统「打地基」：换国内软件源、加 swap 虚拟内存、开端口、设内核参数。  
> **为什么需要？** 4GB 内存同时跑 Jenkins + K3s 很吃紧；国内源加速安装；防火墙要放行 Jenkins 8080 等端口。  
> **不做会怎样？** 装软件慢或失败、内存不足系统崩溃、外网访问不了 Jenkins。

### 6.1 执行脚本

```bash
cd /opt/EnterprisePlatform/deploy   # 或 /opt/deploy
sudo bash scripts/01-system-init.sh
```

**脚本会自动完成：**

- 更换 apt 源为阿里云（加速软件安装）
- 安装 curl、git、vim 等基础工具
- 设置时区为 `Asia/Shanghai`
- 创建 4GB swap（防止 4GB 内存 OOM）
- 配置 K8s 所需内核参数
- 配置 UFW 防火墙规则
- 创建 `deploy` 运维用户（可选）

**预计耗时：** 5～15 分钟（取决于网络）

### 6.2 验证是否成功

```bash
# 1. 检查时区
timedatectl
# 期望：Time zone: Asia/Shanghai

# 2. 检查 swap
free -h
# 期望：Swap 行显示约 4.0G

# 3. 检查防火墙
sudo ufw status
# 期望：22、80、443、8080、6443 等为 ALLOW

# 4. 检查内核参数
sysctl net.ipv4.ip_forward
# 期望：net.ipv4.ip_forward = 1
```

### 6.3 此步骤失败怎么办？

| 错误现象 | 原因 | 解决方法 |
|----------|------|----------|
| `apt-get update` 失败 | 网络或 DNS 问题 | `ping mirrors.aliyun.com`；检查 `/etc/resolv.conf` |
| swap 创建失败 | 磁盘空间不足 | `df -h` 确认根分区有 ≥ 5GB 可用 |
| UFW 命令不存在 | 最小化安装缺包 | `sudo apt install ufw -y` 后重跑脚本 |

---

## 7. 第三步：安装 Docker

> 📖 **新手逐步详解：[STEP_BY_STEP 步骤 5](./STEP_BY_STEP.md#步骤-5安装-docker)**

> **本章是做什么的？** 安装 Docker，用于把应用打成 **镜像**（标准化打包）。  
> **为什么需要？** Jenkins 构建阶段会执行 `docker build`；没有 Docker 就无法 container 化。  
> **不做会怎样？** 无法构建镜像，流水线在 Docker 阶段失败。

### 7.1 执行脚本

```bash
sudo bash scripts/02-docker-install.sh
```

**脚本会自动完成：**

- 添加 Docker 官方国内 apt 镜像源
- 安装 Docker CE
- 写入镜像加速配置（DaoCloud 等国内镜像）
- 启动 Docker 并设置开机自启
- 将 `jenkins`、`deploy` 用户加入 `docker` 组

**预计耗时：** 3～10 分钟

### 7.2 验证是否成功

```bash
# 1. 版本
docker --version
# 期望：Docker version 24.x 或更高

# 2. 镜像加速是否生效
docker info | grep -A5 "Registry Mirrors"
# 期望：列出 docker.m.daocloud.io 等地址

# 3. 拉取测试镜像
docker run --rm hello-world
# 期望：输出 "Hello from Docker!"
```

### 7.3 （强烈推荐）配置阿里云个人镜像加速

公共镜像源可能不稳定，建议额外配置阿里云专属加速：

1. 登录 [阿里云容器镜像服务 - 镜像加速器](https://cr.console.aliyun.com/cn-hangzhou/instances/mirrors)
2. 复制你的专属地址，形如：`https://xxxxxx.mirror.aliyuncs.com`
3. 编辑配置：

```bash
sudo vim /etc/docker/daemon.json
```

在 `registry-mirrors` 数组**最前面**加入你的地址：

```json
{
  "registry-mirrors": [
    "https://xxxxxx.mirror.aliyuncs.com",
    "https://docker.m.daocloud.io",
    "https://docker.1panel.live"
  ],
  ...
}
```

4. 重启 Docker：

```bash
sudo systemctl restart docker
docker info | grep -A5 "Registry Mirrors"
```

---

## 8. 第四步：安装 K3s（Kubernetes）

> 📖 **新手逐步详解：[STEP_BY_STEP 步骤 6](./STEP_BY_STEP.md#步骤-6安装-k3s)**

> **本章是做什么的？** 安装 K3s，这是 **跑应用的地方**（容器编排平台）。  
> **为什么需要？** 部署阶段 Jenkins 会执行 `kubectl`，让 K3s 拉新镜像、滚动更新 Pod。  
> **不做会怎样？** 镜像构建好了也没地方运行，无法自动上线。

### 8.1 执行脚本

```bash
sudo bash scripts/03-k3s-install.sh
```

**脚本会自动完成：**

- 从 rancher.cn 国内镜像下载 K3s 安装脚本
- 安装 K3s v1.28.5（单节点 Server 模式）
- 禁用 Traefik（节省内存，后续可自建 Ingress）
- 配置 containerd 镜像加速
- 创建 `enterprise-platform` 命名空间
- 配置 Jenkins 部署所需的 RBAC 权限
- 为 `jenkins` 用户写入 kubeconfig

**预计耗时：** 3～8 分钟

### 8.2 验证是否成功

```bash
# 1. 节点状态
sudo k3s kubectl get nodes
# 期望：
# NAME        STATUS   ROLES                  AGE   VERSION
# your-host   Ready    control-plane,master   1m    v1.28.x+k3s1

# 2. 系统 Pod
sudo k3s kubectl get pods -A
# 期望：coredns、local-path-provisioner 等 Pod 为 Running

# 3. 命名空间
sudo k3s kubectl get ns | grep enterprise-platform
# 期望：enterprise-platform   Active

# 4. 设置 kubectl 别名（当前会话）
alias kubectl='sudo k3s kubectl'
kubectl get ns
```

### 8.3 让 kubectl 永久可用（可选）

```bash
echo 'alias kubectl="sudo k3s kubectl"' >> ~/.bashrc
source ~/.bashrc
```

### 8.4 此步骤失败怎么办？

| 错误现象 | 解决方法 |
|----------|----------|
| 安装脚本 curl 超时 | 手动指定镜像：`export K3S_INSTALL_URL=https://get.k3s.io` 重试，或检查网络 |
| 节点 NotReady | `sudo systemctl status k3s` 查看日志；`sudo journalctl -u k3s -f` |
| Pod 镜像拉取失败 | 确认 `/etc/rancher/k3s/registries.yaml` 存在后 `sudo systemctl restart k3s` |
| `jenkins` 用户不存在 | 正常，Jenkins 在下一步安装；装完 Jenkins 后重新执行 kubeconfig 配置段（见 14 章） |

---

## 9. 第五步：安装 Jenkins

> 📖 **新手逐步详解：[STEP_BY_STEP 步骤 7](./STEP_BY_STEP.md#步骤-7安装-jenkins)**

> **本章是做什么的？** 安装 Jenkins，这是 **自动化流水线的大脑**——负责拉代码、编译、打镜像、触发部署。  
> **为什么需要？** 没有 Jenkins，每一步都要人工操作；有了它，`git push` 后全自动。  
> **不做会怎样？** 无法 CI/CD，只能手动编译和部署。

### 9.1 执行脚本

```bash
sudo bash scripts/04-jenkins-install.sh
```

**脚本会自动完成：**

- 安装 OpenJDK 11
- 安装 Jenkins LTS
- 限制 JVM 堆内存为 512MB（适配 4GB 服务器）
- 配置清华镜像源加速插件下载
- 安装 kubectl 到 `/usr/local/bin`
- 为 jenkins 用户配置 kubeconfig

**预计耗时：** 3～10 分钟

### 9.2 验证是否成功

```bash
# 1. 服务状态
sudo systemctl status jenkins
# 期望：Active: active (running)

# 2. 端口监听
sudo ss -tlnp | grep 8080
# 期望：有 java 进程监听 8080

# 3. 获取初始管理员密码
sudo cat /var/lib/jenkins/secrets/initialAdminPassword
# 复制输出的字符串，下一步要用

# 4. Jenkins 用户能否访问 Docker 和 K8s
sudo -u jenkins docker ps
sudo -u jenkins kubectl get ns
# 两条命令均应正常输出，不报错
```

### 9.3 配置服务器 Git 与 GitHub（06 脚本）

> 📖 **完整图文步骤：[GITHUB_SSH_JENKINS.md](./GITHUB_SSH_JENKINS.md)**

Jenkins 安装完成后，为 **`jenkins` 用户** 单独配置 Git 和 SSH（与 Windows 上的密钥无关）：

```bash
cd /opt/enterprise/deploy
sudo bash scripts/06-git-github-setup.sh
```

脚本交互要点：Git 邮箱 → 生成 SSH 选 **`y`** → 复制公钥到 **GitHub Settings → SSH and GPG keys**。

验证：

```bash
sudo -u jenkins ssh -T git@github.com
sudo -u jenkins git ls-remote git@github.com:alexlishujia-dev/enterprise.git
```

### 9.4 此步骤失败怎么办？

| 错误现象 | 解决方法 |
|----------|----------|
| `NO_PUBKEY 7198F4B714ABFC68` | 运行修复脚本：`sudo bash scripts/04-jenkins-install-fix.sh` |
| `Package 'jenkins' has no installation candidate` | 同上；或见下方手动修复命令 |
| `jenkins.service does not exist` | Jenkins 未安装成功，先修复安装再 `systemctl enable` |
| Jenkins 启动失败 | `sudo journalctl -u jenkins -n 50` 查看原因 |
| 8080 无法访问 | `sudo ufw allow 8080`；云服务器检查安全组 |
| `jenkins docker ps` 报 permission denied | `sudo usermod -aG docker jenkins && sudo systemctl restart jenkins` |
| `jenkins kubectl` 报错 | 见 [15.2 节 Jenkins 访问 K8s](#152-jenkins-访问-k8s) |

**一键修复 Jenkins 安装：**

```bash
cd /opt/enterprise/deploy
sudo bash scripts/04-jenkins-install-fix.sh
```

---

## 10. 第六步：初始化 Jenkins 控制台

> 📖 **新手逐步详解：[STEP_BY_STEP 步骤 8](./STEP_BY_STEP.md#步骤-8初始化-jenkins-网页)**

> **本章是做什么的？** 在浏览器里完成 Jenkins 首次设置：装插件、建管理员账号。  
> **为什么需要？** 插件提供 GitHub 集成、Docker 构建、Pipeline 等能力；没有插件 Jenkins 是空壳。  
> **不做会怎样？** 无法创建流水线任务，也接不了 GitHub Webhook。

### 10.1 首次登录

1. 浏览器打开：`http://<服务器IP>:8080`
2. 输入上一步获取的**初始管理员密码**
3. 选择 **「安装推荐的插件」**（等待 5～15 分钟，取决于网络）

### 10.2 额外安装插件

进入 **Manage Jenkins → Plugins → Available plugins**，搜索并安装：

| 插件名 | 用途 |
|--------|------|
| GitHub Integration Plugin | GitHub Webhook 集成 |
| GitHub Branch Source Plugin | 多分支流水线 |
| Pipeline | 流水线核心 |
| Docker Pipeline | 构建 Docker 镜像 |
| Kubernetes CLI | kubectl 命令 |

安装完成后**重启 Jenkins**（页面会提示）。

### 10.3 创建管理员账号

按向导设置：

- 用户名：`admin`（或自定义）
- 密码：请使用强密码并妥善保存

### 10.4 配置 Jenkins URL

保持默认 `http://<服务器IP>:8080/` 即可（有域名后续再改）。

### 10.5 运行健康检查

回到服务器终端：

```bash
cd /opt/EnterprisePlatform/deploy
bash scripts/health-check.sh
```

期望全部显示 `OK` 或正常输出。

---

## 11. 第七步：GitHub 与 Jenkins 集成

> 📖 **新手逐步详解：[STEP_BY_STEP 步骤 9](./STEP_BY_STEP.md#步骤-9git-与-jenkins-连通)**

> **本章是做什么的？** 把 Git 仓库和 Jenkins **连起来**：配置 Token 凭据、添加 Webhook。  
> **为什么需要？** 这样你 `git push` 后，Git 会自动「按门铃」通知 Jenkins 开始构建，不用手动点按钮。  
> **不做会怎样？** Jenkins 能工作，但每次都要手动点「Build Now」才能构建。

> 前提：已完成 [第 4 章 Git 与 GitHub 环境搭建](#4-git-与-github-环境搭建)（仓库已创建、代码已 push）。

### 11.1 创建 Jenkins 专用 Personal Access Token

> 若已在 [4.7 节](#47-使用-https--personal-access-token备选) 创建过 Token 且权限包含 `repo` + `admin:repo_hook`，可直接复用，无需重复创建。

1. 登录 GitHub → 右上角头像 → **Settings**
2. 左侧最底部 **Developer settings**
3. **Personal access tokens → Tokens (classic) → Generate new token (classic)**
4. 填写 Note：`jenkins-ci`
5. 勾选权限：

| 权限 | 说明 |
|------|------|
| `repo` | 读写仓库代码 |
| `admin:repo_hook` | 管理 Webhook |
| `read:user` | 读取用户信息 |

6. 点击 **Generate token**，**立即复制保存**（只显示一次）

### 11.2 在 Jenkins 中添加 GitHub 凭据

1. **Manage Jenkins → Credentials**
2. 点击 **(global)** → **Add Credentials**
3. 填写：

| 字段 | 值 |
|------|-----|
| Kind | Secret text |
| Secret | 粘贴 GitHub Token |
| ID | `github-token` |
| Description | GitHub PAT |

4. 点击 **Create**

### 11.3 在 GitHub 仓库添加 Webhook

1. 打开你的 GitHub 仓库 → **Settings → Webhooks → Add webhook**
2. 填写：

| 字段 | 值 |
|------|-----|
| Payload URL | `http://<服务器IP>:8080/github-webhook/` |
| Content type | `application/json` |
| Secret | 留空或自定义（需与 Jenkins 一致） |
| 事件 | **Just the push event** |
| Active | 勾选 |

3. 点击 **Add webhook**

### 11.4 验证 Webhook 是否可达

> **关键问题：** GitHub 服务器在公网，如果你的 Jenkins 在内网或无公网 IP，Webhook **无法直接送达**。

**判断方法：**

| 场景 | Webhook 是否可用 | 替代方案 |
|------|------------------|----------|
| 云服务器有公网 IP，8080 已放行 | ✅ 可用 | 无 |
| 家庭/公司内网服务器 | ❌ 不可用 | 使用 frp / ngrok 内网穿透；或 Jenkins 定时轮询 GitHub |
| 云服务器但仅内网 IP | ❌ 不可用 | 绑定弹性公网 IP |

**测试命令（在服务器上）：**

```bash
curl -I http://localhost:8080/github-webhook/
# 期望：HTTP/1.1 200 或 403（说明 Jenkins 在响应）
```

添加 Webhook 后，在 GitHub Webhook 页面点击 **Recent Deliveries**，查看是否有绿色 ✓。

也可运行交互说明脚本：

```bash
sudo bash scripts/05-github-webhook-setup.sh
```

---

## 12. 第八步：配置阿里云镜像仓库

> 📖 **新手逐步详解：[STEP_BY_STEP 步骤 10](./STEP_BY_STEP.md#步骤-10配置镜像仓库)**

> **本章是做什么的？** 在阿里云创建 **Docker 镜像仓库**，作为 Jenkins 和 K3s 之间的「中转站」。  
> **为什么需要？** Jenkins 构建完镜像 push 到这里；K3s 部署时从这里 pull。便于版本管理和回滚。  
> **不做会怎样？** 镜像只能留在本机，多节点拉取困难，也不方便保留历史版本。

Jenkins 构建完镜像后需要推送到仓库，K3s 再从仓库拉取。国内环境**强烈建议使用阿里云 ACR**，避免 Docker Hub 限流。

### 12.1 创建命名空间和仓库

1. 登录 [阿里云容器镜像服务 ACR](https://cr.console.aliyun.com/)
2. 选择地域（建议 `华东1 杭州`）
3. **命名空间** → 创建命名空间，如 `enterprise`
4. **镜像仓库** → 创建仓库：
   - 仓库名称：`enterprise-app`
   - 类型：私有
   - 代码源：本地仓库

### 12.2 获取登录凭据

1. 镜像仓库详情页 → **访问凭证**
2. 设置固定密码（记住用户名和密码）

### 12.3 在 Jenkins 中添加镜像仓库凭据

1. **Manage Jenkins → Credentials → Add Credentials**
2. 填写：

| 字段 | 值 |
|------|-----|
| Kind | Username with password |
| Username | 阿里云镜像仓库用户名 |
| Password | 设置的固定密码 |
| ID | `aliyun-registry-cred` |
| Description | Aliyun ACR |

### 12.4 在服务器上测试推送

```bash
# 登录（用户名通常是阿里云账号全邮箱或独立用户名）
docker login registry.cn-hangzhou.aliyuncs.com

# 拉取测试
docker pull registry.cn-hangzhou.aliyuncs.com/enterprise/enterprise-app:latest
# 首次可能不存在，正常
```

### 12.5 创建 K8s 拉取镜像的 Secret（私有仓库必需）

```bash
sudo k3s kubectl create secret docker-registry aliyun-registry \
  --docker-server=registry.cn-hangzhou.aliyuncs.com \
  --docker-username=你的用户名 \
  --docker-password=你的密码 \
  -n enterprise-platform
```

---

## 13. 第九步：创建 CI/CD 流水线

> 📖 **新手逐步详解：[STEP_BY_STEP 步骤 11](./STEP_BY_STEP.md#步骤-11创建-cicd-流水线)**

> **本章是做什么的？** 编写 **Jenkinsfile**（流水线剧本），告诉 Jenkins 按什么顺序：拉代码 → 编译 → 打镜像 → 部署。  
> **为什么需要？** Jenkins 本身不知道你的项目怎么构建；Jenkinsfile 就是「操作说明书」。  
> **不做会怎样？** Jenkins 任务创建了但没有构建步骤，或构建逻辑不对。

### 13.1 在项目中准备 Dockerfile

在项目根目录创建 `Dockerfile`（Spring Boot 示例）：

```dockerfile
FROM eclipse-temurin:11-jre-focal
WORKDIR /app
COPY target/*.jar app.jar
EXPOSE 8080
ENTRYPOINT ["java", "-jar", "app.jar"]
```

### 13.2 在项目中准备 Jenkinsfile

将 `deploy/configs/jenkins/Jenkinsfile.example` 复制到项目根目录并重命名为 `Jenkinsfile`，修改以下变量：

```groovy
REGISTRY_NS  = 'enterprise'           // 你的阿里云命名空间
IMAGE_NAME   = 'enterprise-app'       // 你的镜像仓库名
```

### 13.3 首次手动部署 K8s 资源

在 Jenkins 第一次构建前，需要先有 Deployment 对象：

```bash
# 编辑示例文件，替换镜像地址
sudo vim /opt/EnterprisePlatform/deploy/k8s/app-deployment.example.yaml
# 将 registry.cn-hangzhou.aliyuncs.com/YOUR_NAMESPACE/enterprise-app:latest
# 改为你的实际地址

# 若使用私有仓库，在 deployment 的 spec.template.spec 下添加：
# imagePullSecrets:
#   - name: aliyun-registry

sudo k3s kubectl apply -f /opt/EnterprisePlatform/deploy/k8s/app-deployment.example.yaml
```

### 13.4 在 Jenkins 创建 Pipeline 任务

1. Jenkins 首页 → **New Item**
2. 名称：`enterprise-platform-deploy`
3. 类型：**Pipeline** → OK
4. 配置：

**General 区域：**

- 勾选 **GitHub project**，填入仓库 URL

**Build Triggers 区域：**

- 勾选 **GitHub hook trigger for GITScm polling**

**Pipeline 区域：**

| 字段 | 值 |
|------|-----|
| Definition | Pipeline script from SCM |
| SCM | Git |
| Repository URL | `https://github.com/YOUR_ORG/EnterprisePlatform.git` |
| Credentials | 选择 `github-token` |
| Branch | `*/main` 或 `*/master` |
| Script Path | `Jenkinsfile` |

5. 点击 **Save**

### 13.5 手动触发首次构建

1. 进入任务页 → **Build Now**
2. 点击左侧构建编号 → **Console Output** 查看日志
3. 期望阶段：`Checkout` → `Build` → `Docker Build & Push` → `Deploy to K3s` 全部成功

---

## 14. 第十步：首次部署验证

> 📖 **新手逐步详解：[STEP_BY_STEP 步骤 12](./STEP_BY_STEP.md#步骤-12整体验证)**

> **本章是做什么的？** 完整走一遍流程，确认从 push 到应用运行 **整条链路都通了**。  
> **为什么需要？** 部署环节多，任何一步漏配都可能 silently 失败；验证能及早发现问题。  
> **不做会怎样？** 可能以为部署成功，实际应用并没更新或无法访问。

### 14.1 验证清单

在服务器上逐项执行：

```bash
# ✅ 1. K3s 节点正常
sudo k3s kubectl get nodes

# ✅ 2. 应用 Pod 运行中
sudo k3s kubectl get pods -n enterprise-platform

# ✅ 3. Service 存在
sudo k3s kubectl get svc -n enterprise-platform

# ✅ 4. 镜像已推送到阿里云（在阿里云控制台查看标签列表）

# ✅ 5. Jenkins 构建历史为蓝色（成功）

# ✅ 6. 健康检查脚本
bash /opt/EnterprisePlatform/deploy/scripts/health-check.sh
```

### 14.2 验证 GitHub 自动触发

1. 修改项目任意文件并 push 到 GitHub
2. 等待 10～30 秒
3. Jenkins 应自动出现新的构建任务

### 14.3 访问应用（可选）

若配置了 Ingress 和域名：

```bash
# 查看 Ingress
sudo k3s kubectl get ingress -n enterprise-platform
```

临时端口转发测试（无需 Ingress）：

```bash
sudo k3s kubectl port-forward -n enterprise-platform svc/enterprise-app 8888:80
# 浏览器访问 http://<服务器IP>:8888
```

---

## 15. 权限配置说明

### 14.1 Jenkins 访问 Docker

Jenkins 以 `jenkins` 系统用户运行，必须加入 `docker` 组：

```bash
sudo usermod -aG docker jenkins
sudo systemctl restart jenkins

# 验证
sudo -u jenkins docker ps
```

### 15.2 Jenkins 访问 K8s

```bash
sudo mkdir -p /var/lib/jenkins/.kube
sudo cp /etc/rancher/k3s/k3s.yaml /var/lib/jenkins/.kube/config

# 将 API 地址从 127.0.0.1 改为本机 IP（重要！）
SERVER_IP=$(hostname -I | awk '{print $1}')
sudo sed -i "s/127.0.0.1/${SERVER_IP}/g" /var/lib/jenkins/.kube/config

sudo chown -R jenkins:jenkins /var/lib/jenkins/.kube
sudo chmod 600 /var/lib/jenkins/.kube/config

# 验证
sudo -u jenkins kubectl get ns
sudo -u jenkins kubectl auth can-i create deployments --all-namespaces
# 期望：yes
```

### 14.3 RBAC 权限

Jenkins 部署应用需要 K8s RBAC，安装 K3s 时已自动应用：

```bash
sudo k3s kubectl apply -f /opt/EnterprisePlatform/deploy/k8s/jenkins-rbac.yaml
```

### 14.4 运维用户 deploy

初始化脚本已创建 `deploy` 用户（sudo 免密），可用于日常运维：

```bash
# 切换到 deploy 用户
su - deploy

# deploy 用户执行 docker（需先加入 docker 组，02 脚本已处理）
docker ps
```

更多权限命令见 [PERMISSIONS.md](./PERMISSIONS.md)。

---

## 16. 日常运维命令

```bash
# ---------- 服务管理 ----------
sudo systemctl status jenkins
sudo systemctl restart jenkins
sudo systemctl status k3s
sudo systemctl restart k3s
sudo systemctl restart docker

# ---------- K8s ----------
sudo k3s kubectl get pods -A
sudo k3s kubectl get pods -n enterprise-platform
sudo k3s kubectl logs -f deployment/enterprise-app -n enterprise-platform
sudo k3s kubectl rollout undo deployment/enterprise-app -n enterprise-platform  # 回滚

# ---------- Jenkins ----------
sudo cat /var/lib/jenkins/secrets/initialAdminPassword  # 仅首次
sudo tail -f /var/log/jenkins/jenkins.log

# ---------- 资源监控（4GB 机器必看）----------
free -h
df -h
sudo k3s kubectl top pods -A 2>/dev/null || echo "未安装 metrics-server"

# ---------- 健康检查 ----------
bash /opt/EnterprisePlatform/deploy/scripts/health-check.sh
```

---

## 17. 常见问题 FAQ

### Q1：`docker pull` 或 Pod 镜像拉取超时

```bash
# 检查 Docker 加速
docker info | grep -A5 "Registry Mirrors"

# 检查 K3s 加速
cat /etc/rancher/k3s/registries.yaml
sudo systemctl restart k3s

# 手动测试
docker pull docker.m.daocloud.io/library/nginx:alpine
```

### Q2：Jenkins 构建时 `docker: not found` 或 permission denied

```bash
sudo usermod -aG docker jenkins
sudo systemctl restart jenkins
sudo -u jenkins docker ps
```

### Q3：Jenkins 构建 Deploy 阶段 `Unable to connect to the server`

kubeconfig 中 API 地址仍是 127.0.0.1，改成本机 IP（见 15.2 节）。

### Q4：GitHub Webhook 显示 delivery failed

1. Jenkins 是否公网可达（见 11.4 节）
2. Payload URL 末尾必须有 `/github-webhook/`
3. Jenkins 任务是否勾选 **GitHub hook trigger for GITScm polling**
4. 防火墙/安全组是否放行 8080

### Q5：服务器内存不足 / OOM

```bash
free -h   # 查看 swap 是否生效

# 确认 Jenkins JVM 限制
grep JAVA_ARGS /etc/default/jenkins
# 应为 -Xmx512m

# 确认 Pod 有资源限制
sudo k3s kubectl describe pod -n enterprise-platform
```

### Q6：K3s 安装脚本下载失败

```bash
# 尝试备用安装源
export K3S_INSTALL_URL=https://get.k3s.io
sudo -E bash scripts/03-k3s-install.sh
```

### Q7：`git clone` 或 Jenkins Checkout 失败

```bash
# 1. 测试 GitHub 连通
curl -I https://github.com

# 2. 测试 jenkins 用户 SSH
sudo -u jenkins ssh -T git@github.com

# 3. 测试 HTTPS + Token（在 Jenkins 凭据中配置 github-token）
sudo -u jenkins git ls-remote https://github.com/USER/REPO.git

# 4. 重新运行 Git 配置脚本
sudo bash scripts/06-git-github-setup.sh
```

常见原因：未添加 SSH 公钥、Token 过期、私有仓库未配置凭据、国内网络超时（改用 HTTPS + Token）。

### Q8：push 时 `Permission denied (publickey)`

1. 确认公钥已添加到 GitHub → Settings → SSH and GPG keys
2. Windows 检查 `%USERPROFILE%\.ssh\config` 中 Host github.com 配置
3. 运行 `ssh-add -l` 确认密钥已加载
4. 使用 HTTPS + Token 作为备选（见 [4.7 节](#47-使用-https--personal-access-token备选)）

### Q9：如何完全卸载重装？

```bash
# 卸载 K3s
/usr/local/bin/k3s-uninstall.sh

# 卸载 Jenkins
sudo apt remove --purge jenkins -y
sudo rm -rf /var/lib/jenkins

# 卸载 Docker
sudo apt remove --purge docker-ce docker-ce-cli containerd.io -y
sudo rm -rf /var/lib/docker

# 然后从头执行 01~04 脚本
```

---

## 18. 附录

### 18.1 端口说明

| 端口 | 协议 | 服务 | 说明 |
|------|------|------|------|
| 22 | TCP | SSH | 远程登录 |
| 80 | TCP | HTTP | Web / Ingress |
| 443 | TCP | HTTPS | 加密 Web |
| 8080 | TCP | Jenkins | CI/CD 控制台 |
| 6443 | TCP | K3s API | Kubernetes API |
| 8472 | UDP | Flannel | K3s 网络 |
| 10250 | TCP | Kubelet | 节点代理 |

### 18.2 部署文件目录

```
deploy/
├── docs/
│   ├── GITHUB_REPO_SETUP.md         # 前端+后端 GitHub 仓库创建
│   ├── BEGINNER_GUIDE.md            # 组件概念与整体理解
│   ├── STEP_BY_STEP.md              # ★ 每一步详细说明（新手推荐）
│   ├── DEPLOYMENT_GUIDE.md          # 操作命令与配置细节
│   ├── CHECKLIST.md                 # 部署检查清单
│   └── PERMISSIONS.md               # 权限命令手册
├── configs/
│   ├── apt-sources-cn.list
│   ├── daemon.json
│   ├── registries.yaml
│   └── jenkins/Jenkinsfile.example
├── k8s/
│   ├── namespace.yaml
│   ├── jenkins-rbac.yaml
│   └── app-deployment.example.yaml
└── scripts/
    ├── 01-system-init.sh
    ├── 02-docker-install.sh
    ├── 03-k3s-install.sh
    ├── 04-jenkins-install.sh
    ├── 05-github-webhook-setup.sh
    ├── 06-git-github-setup.sh
    └── health-check.sh
```

### 18.3 部署完成检查清单（可打印）

详见 [CHECKLIST.md](./CHECKLIST.md)。

---

**部署顺利！** 若在某一步卡住，请先查看该步骤的「验证是否成功」和 FAQ 对应条目，或运行 `bash scripts/health-check.sh` 定位问题组件。

# 逐步详解手册（面向新手）

> 本文对部署流程的 **每一步** 做详细说明：做什么、为什么、怎么做、成功是什么样、容易错在哪。  
> 具体操作命令以 [DEPLOYMENT_GUIDE.md](./DEPLOYMENT_GUIDE.md) 为准；本文侧重 **帮你理解**。

**建议阅读顺序：** [BEGINNER_GUIDE.md](./BEGINNER_GUIDE.md) → 本文 → [DEPLOYMENT_GUIDE.md](./DEPLOYMENT_GUIDE.md) 动手操作 → [CHECKLIST.md](./CHECKLIST.md) 勾选

---

## 步骤总览

| 步骤 | 名称 | 在哪里做 | 大概耗时 |
|------|------|----------|----------|
| 0 | 理解整体目标 | 阅读文档 | 15 分钟 |
| 1 | 部署前准备 | 本地 + 服务器 | 10 分钟 |
| 2 | Git + 代码仓库 | 主要是 Windows + 网页 | 20～40 分钟 |
| 3 | 连接服务器、上传脚本 | 本地 SSH | 10 分钟 |
| 4 | 系统初始化 | 服务器 | 10～15 分钟 |
| 5 | 安装 Docker | 服务器 | 5～10 分钟 |
| 6 | 安装 K3s | 服务器 | 5～10 分钟 |
| 7 | 安装 Jenkins | 服务器 | 5～10 分钟 |
| 8 | 初始化 Jenkins 网页 | 浏览器 | 15～30 分钟 |
| 9 | Git 与 Jenkins 连通 | 网页 + 服务器 | 15 分钟 |
| 10 | 配置镜像仓库 | 阿里云网页 | 15 分钟 |
| 11 | 创建流水线 | 项目 + Jenkins | 20～40 分钟 |
| 12 | 整体验证 | 全流程 | 10 分钟 |

---

## 步骤 0：先理解「我要搭什么」

### 做什么

不是装一个软件，而是搭一条 **自动化流水线**：

```
改代码 → push → 自动编译 → 自动打包容器 → 自动部署上线
```

### 为什么需要理解这个

后面有 Git、Docker、Jenkins、K3s 等名词，如果不知道整体目标，容易搞不清「这一步为啥要装」。

### 你要记住的 6 个角色

| 角色 | 干什么 | 你可以把它想成 |
|------|--------|----------------|
| Git 仓库 | 存代码 | 菜谱库 |
| Jenkins | 自动构建、部署 | 机器人厨师 |
| Docker | 打包应用 | 标准餐盒 |
| 镜像仓库 | 存餐盒 | 仓库货架 |
| K3s | 运行应用 | 餐厅 |
| Webhook | 通知 Jenkins | 门铃 |

### 成功标志

你能用自己的话说出：**push 代码之后，机器会自动做哪些事**。

### 下一步

→ [步骤 1：部署前准备](#步骤-1部署前准备)

---

## 步骤 1：部署前准备

### 做什么

确认 **服务器、账号、工具** 都就绪，避免装到一半卡住。

### 为什么需要

比如内存不够 4GB、端口没开、没有 GitHub 账号，后面某一步必然失败。

### 你需要确认的事项

#### 1.1 服务器

| 检查项 | 要求 | 怎么查 |
|--------|------|--------|
| 系统 | Ubuntu 20.04 | 买服务器时选，或 `lsb_release -a` |
| 内存 | ≥ 4GB | 控制台或 `free -h` |
| 磁盘 | ≥ 40GB 可用 | `df -h` |
| 能上网 | 能访问外网 | `ping mirrors.aliyun.com` |

#### 1.2 账号

- **Git 仓库**：GitHub 账号，或公司自建 Gitea（地址形如 `git.xxx.com`）
- **阿里云**：用来存 Docker 镜像（国内速度快）

#### 1.3 本地工具（Windows）

- **SSH 工具**：PowerShell 即可，`ssh root@服务器IP`
- **Git for Windows**：用来 `git push` 代码

#### 1.4 云服务器安全组（如果用阿里云/腾讯云）

放行端口：`22、80、443、8080、6443`

> 22=SSH 登录，8080=Jenkins 网页，6443=K8s API

### 成功标志

- [ ] 能 SSH 登录服务器
- [ ] 有 GitHub（或 Gitea）账号
- [ ] 有阿里云账号
- [ ] 本地已装 Git

### 常见疑问

**Q：我没有公网 IP 可以吗？**  
内网也能搭，但 GitHub.com 的 Webhook 可能通知不到 Jenkins；用自建 Gitea 同机部署更合适。

**Q：一定要阿里云吗？**  
不是必须，但国内 push/pull 镜像更稳定；也可用 Harbor 等自建镜像仓库。

### 下一步

→ [步骤 2：Git 与代码仓库](#步骤-2git-与代码仓库)

---

## 步骤 2：Git 与代码仓库

对应 [DEPLOYMENT_GUIDE.md 第 4 章](./DEPLOYMENT_GUIDE.md#4-git-与-github-环境搭建)。

### 做什么

1. 在 **Windows** 安装 Git  
2. 在 **GitHub/Gitea** 创建空仓库  
3. 把项目代码 **push** 上去  

### 为什么需要

- 代码要有 **统一存放的地方**（远程仓库）
- Jenkins 构建时要从这里 **拉代码**
- push 后通过 Webhook **触发** Jenkins

> **重要：** 这一步主要在 **你的电脑和 Git 网站** 上完成，不是「在服务器上安装 GitHub 网站」。

### 分小节说明

#### 2.1 注册 GitHub / 使用公司 Git（4.1）

- **做什么：** 有一个能登录的 Git 托管账号  
- **为什么：** 没有仓库就没地方 push  
- **成功标志：** 能登录并看到「New repository」按钮  

#### 2.2 Windows 安装 Git（4.2）

- **做什么：** 安装 `git` 命令行工具  
- **为什么：** `git push` 需要本机有 Git  
- **成功标志：** PowerShell 里 `git --version` 有输出  
- **要做的事：**
  ```powershell
  git config --global user.name "你的名字"
  git config --global user.email "你的邮箱"
  ```

#### 2.3 服务器安装 Git（4.3）—— 很多新手会疑惑

- **做什么：** 确保 **Ubuntu 服务器** 上也装了 Git  
- **为什么：** **Jenkins 在服务器上跑**，构建时要执行 `git clone` 拉你的代码；不是给你 SSH 上去写代码用的  
- **谁在用：** 主要是 `jenkins` 这个系统用户  
- **成功标志：** 服务器上 `git --version` 有输出（01 脚本通常已装好）  
- **后续：** 第 7 步装完 Jenkins 后，还要跑 `06-git-github-setup.sh` 给 jenkins 配 SSH/身份  

#### 2.4 创建远程仓库（4.4）

- **做什么：** 在 GitHub 创建 **一个** `EnterprisePlatform` 仓库（Monorepo）  
- **为什么：** 你的项目含 `src/`（API）+ `frontend/` + `mobile/`，放同一仓库便于版本对齐  
- **详细步骤：** [GITHUB_REPO_SETUP.md](./GITHUB_REPO_SETUP.md) ★  
- **成功标志：** GitHub 上能看到 backend + frontend 目录结构  

#### 2.5 配置 SSH 密钥（4.5，推荐）

- **做什么：** 生成密钥对，公钥放到 GitHub，私钥留在本机  
- **为什么：** 以后 push/pull 不用每次输密码  
- **成功标志：** `ssh -T git@github.com` 提示认证成功  

#### 2.6 首次 push 代码（4.6）

- **做什么：** 在 **项目根目录** 一次性 push API + 前端 + mobile + deploy  
- **为什么：** Jenkins 从同一仓库拉代码；前后端接口变更可同一 commit  
- **详细步骤：** [GITHUB_REPO_SETUP.md 步骤 A3～A5](./GITHUB_REPO_SETUP.md#步骤-a3windows-本地初始化-git-并关联远程仓库)  
- **示例：**
  ```powershell
  cd "D:\hj\校务系统\EnterprisePlatform"
  git init && git branch -M main
  git remote add origin git@github.com:你的用户名/EnterprisePlatform.git
  git add .
  git commit -m "Initial commit: API + Web + Mobile + deploy"
  git push -u origin main
  ```
- **成功标志：** GitHub 上有 `src/`、`frontend/`、`mobile/`，且 **没有** `node_modules`  

### 本步骤整体成功标志

- [ ] 远程仓库里有你的代码  
- [ ] 本地 `git remote -v` 指向正确地址  

### 常见错误

| 现象 | 原因 | 处理 |
|------|------|------|
| push 被拒绝 | 远程有 README 冲突 | 先 pull 或删远程 README 重建仓库 |
| Permission denied | SSH 密钥未配置 | 做 4.5 节 |
| 不知道服务器为何要 Git | 误解 | 给 **Jenkins 拉代码** 用，见 2.3 |

### 下一步

→ [步骤 3：连接服务器](#步骤-3连接服务器上传部署脚本)

---

## 步骤 3：连接服务器、上传部署脚本

对应 [DEPLOYMENT_GUIDE.md 第 5 章](./DEPLOYMENT_GUIDE.md#5-第一步连接服务器并上传部署文件)。

### 做什么

1. 用 SSH 登录 Linux 服务器  
2. 把项目里的 `deploy` 目录弄上去  
3. 给脚本加执行权限  

### 为什么需要

后面安装 Docker、K3s、Jenkins 都靠 `deploy/scripts/` 里的自动化脚本；手工一条条装容易漏步骤。

### 具体操作

#### 3.1 SSH 登录

```bash
ssh root@你的服务器IP
```

第一次会问是否信任，输入 `yes`。

#### 3.2 上传 deploy 目录

**方式 A（推荐）：** 服务器上 git clone 整个项目  
**方式 B：** 本机 `scp -r deploy root@IP:/opt/`

#### 3.3 加执行权限

```bash
cd /opt/EnterprisePlatform/deploy
chmod +x scripts/*.sh
```

### 成功标志

```bash
ls scripts/
# 能看到 01～06 和 health-check.sh
```

### 常见疑问

**Q：一定要在 /opt 目录吗？**  
不必须，任何路径都行，后面命令里的路径改成你的即可。

### 下一步

→ [步骤 4：系统初始化](#步骤-4系统初始化)

---

## 步骤 4：系统初始化

对应 [DEPLOYMENT_GUIDE.md 第 6 章](./DEPLOYMENT_GUIDE.md#6-第二步系统初始化)。  
执行：`sudo bash scripts/01-system-init.sh`

### 做什么

给 Linux 系统 **打地基**，还没装 Jenkins/Docker/K3s。

### 为什么需要

| 脚本做的事 | 原因 |
|------------|------|
| 换国内 apt 源 | 国内下载软件快 |
| 安装 git/curl 等 | 后面脚本依赖这些工具 |
| 创建 4GB swap | 4GB 内存不够同时跑 Jenkins+K3s，swap 当临时内存 |
| 配置内核参数 | K8s 网络需要 |
| 配置防火墙 UFW | 放行 8080(Jenkins)、6443(K8s) 等端口 |
| 设置时区 | 日志时间正确 |

### 你会看到什么

脚本跑 5～15 分钟，最后输出「系统初始化完成」和权限命令提示。

### 成功标志

```bash
free -h          # Swap 约 4G
timedatectl      # Asia/Shanghai
sudo ufw status  # 8080、6443 等为 ALLOW
```

### 常见错误

| 现象 | 处理 |
|------|------|
| apt update 失败 | 检查网络、DNS |
| swap 创建失败 | 磁盘空间不足，清理后再跑 |

### 和下一步的关系

系统准备好了 → 才能稳定安装 Docker。

### 下一步

→ [步骤 5：安装 Docker](#步骤-5安装-docker)

---

## 步骤 5：安装 Docker

对应 [DEPLOYMENT_GUIDE.md 第 7 章](./DEPLOYMENT_GUIDE.md#7-第三步安装-docker)。  
执行：`sudo bash scripts/02-docker-install.sh`

### 做什么

安装 **Docker**——用来构建和运行 **容器镜像** 的工具。

### 为什么需要

流水线里 Jenkins 会执行：

```
docker build   → 把你的 jar 打进镜像
docker push    → 把镜像推到阿里云
```

K3s 运行时也会从镜像 **创建容器**。

### 通俗理解

- **镜像** = 安装包（含 Java + 你的 jar）  
- **容器** = 正在运行的程序实例  
- Docker = 做安装包、运行程序的工具  

### 脚本还会做什么

- 配置 **国内镜像加速**（拉 Docker 基础镜像更快）  
- 把 `jenkins` 用户加入 `docker` 组（Jenkins 才能 build）  

### 成功标志

```bash
docker --version
docker run --rm hello-world   # 输出 Hello from Docker!
```

### 常见疑问

**Q：Docker 和 K3s 不是重复吗？**  
不重复。Docker 负责 **构建** 镜像；K3s 负责 **在生产环境运行** 镜像并管理更新、重启。

### 下一步

→ [步骤 6：安装 K3s](#步骤-6安装-k3s)

---

## 步骤 6：安装 K3s

对应 [DEPLOYMENT_GUIDE.md 第 8 章](./DEPLOYMENT_GUIDE.md#8-第四步安装-k3skubernetes)。  
执行：`sudo bash scripts/03-k3s-install.sh`

### 做什么

安装 **K3s**——轻量级 Kubernetes，用来 **运行你的应用**。

### 为什么需要

镜像构建好后，需要有人：

- 启动应用容器  
- 挂了自动重启  
- 发新版时 **滚动更新**（尽量不停机）  

这些由 K3s 完成。Jenkins 部署时执行 `kubectl` 命令告诉 K3s「请用新镜像」。

### 脚本还会做什么

- 创建命名空间 `enterprise-platform`（项目专区）  
- 配置 Jenkins 操作 K8s 的权限（RBAC）  
- 配置 containerd 镜像加速  

### 关键概念（新手只需知道这些）

| 词 | 意思 |
|----|------|
| Pod | 跑你应用的一个实例 |
| Deployment | 描述「用什么镜像、跑几个副本」的配置 |
| kubectl | 操作 K3s 的命令 |

### 成功标志

```bash
sudo k3s kubectl get nodes
# STATUS 为 Ready

sudo k3s kubectl get ns
# 有 enterprise-platform
```

### 常见疑问

**Q：为什么用 K3s 不用完整 K8s？**  
4GB 内存太小，完整 K8s 容易内存爆；K3s 够用且省资源。

### 下一步

→ [步骤 7：安装 Jenkins](#步骤-7安装-jenkins)

---

## 步骤 7：安装 Jenkins

对应 [DEPLOYMENT_GUIDE.md 第 9 章](./DEPLOYMENT_GUIDE.md#9-第五步安装-jenkins)。  
执行：`sudo bash scripts/04-jenkins-install.sh`

### 做什么

安装 **Jenkins**——整条流水线的 **调度中心**。

### 为什么需要

它把下面这些步骤 **串成自动化**：

1. 从 Git 拉代码  
2. `mvn package` 编译  
3. `docker build` 打镜像  
4. `docker push` 推镜像  
5. `kubectl` 部署到 K3s  

没有 Jenkins，每一步都要你 SSH 上去手动敲命令。

### 脚本还会做什么

- 限制 Jenkins 内存 512MB（防止 4GB 机器 OOM）  
- 安装 kubectl  
- 给 jenkins 用户配置访问 K3s 的 kubeconfig  

### 装完后立刻做

```bash
sudo bash scripts/06-git-github-setup.sh
```

给 jenkins 配 Git 身份和 SSH 密钥，让它能 **从 Git 拉代码**。

### 成功标志

```bash
sudo systemctl status jenkins    # active (running)
sudo cat /var/lib/jenkins/secrets/initialAdminPassword   # 记下密码
sudo -u jenkins docker ps        # 不报错
sudo -u jenkins kubectl get ns   # 不报错
```

浏览器访问：`http://服务器IP:8080`

### 常见疑问

**Q：Jenkins 密码忘了？**  
首次密码在 `/var/lib/jenkins/secrets/initialAdminPassword`；设置管理员后这个文件会失效。

### 下一步

→ [步骤 8：初始化 Jenkins 网页](#步骤-8初始化-jenkins-网页)

---

## 步骤 8：初始化 Jenkins 网页

对应 [DEPLOYMENT_GUIDE.md 第 10 章](./DEPLOYMENT_GUIDE.md#10-第六步初始化-jenkins-控制台)。

### 做什么

在 **浏览器** 里完成 Jenkins 首次配置。

### 为什么需要

刚装的 Jenkins 是「空壳」，需要：

- 安装 **插件**（Git、Docker、Pipeline 等）  
- 创建 **管理员账号**  

### 你要做的

1. 打开 `http://服务器IP:8080`  
2. 输入初始密码  
3. 选 **「安装推荐的插件」**（等 5～15 分钟）  
4. 额外安装：GitHub Integration、Pipeline、Docker Pipeline  
5. 创建 admin 账号和密码  
6. 重启 Jenkins（装插件后提示）  

### 成功标志

- 能用 admin 账号登录 Jenkins  
- `bash scripts/health-check.sh` 大部分 OK  

### 常见错误

| 现象 | 处理 |
|------|------|
| 插件下载慢/失败 | 04 脚本已配清华镜像，重试或手动上传插件 |
| 8080 打不开 | 检查防火墙、云安全组 |

### 下一步

→ [步骤 9：Git 与 Jenkins 连通](#步骤-9git-与-jenkins-连通)

---

## 步骤 9：Git 与 Jenkins 连通

对应 [DEPLOYMENT_GUIDE.md 第 11 章](./DEPLOYMENT_GUIDE.md#11-第七步github-与-jenkins-集成)。

### 做什么

让 **Git 仓库** 和 **Jenkins** 能互相「说话」：

1. 创建 **Token**（Jenkins 访问 Git 的密码牌）  
2. 在 Jenkins 里 **保存凭据**  
3. 在 Git 仓库配置 **Webhook**（push 时通知 Jenkins）  

### 为什么需要

| 配置 | 作用 |
|------|------|
| Token + 凭据 | Jenkins **拉私有仓库代码** 时要认证 |
| Webhook | 你 push 后 **自动触发** 构建，不用手点 Build |

### 通俗理解

- **Token** = Jenkins 进 Git 仓库的钥匙  
- **Webhook** = Git 按的门铃，Jenkins 听到就开始构建  

### 你要做的

#### 9.1 创建 GitHub Token

GitHub → Settings → Developer settings → Personal access tokens  
勾选：`repo`、`admin:repo_hook`

#### 9.2 Jenkins 添加凭据

Manage Jenkins → Credentials → Add  
类型 Secret text，ID 填 `github-token`

#### 9.3 GitHub 添加 Webhook

仓库 → Settings → Webhooks  
URL：`http://服务器IP:8080/github-webhook/`

### 成功标志

- Jenkins 凭据列表里有 `github-token`  
- GitHub Webhook 页面 Recent Deliveries 有绿色 ✓（需 Jenkins 公网可达）  

### 常见疑问

**Q：Webhook 一直失败？**  
GitHub.com 在公网，Jenkins 在内网时 GitHub **访问不到** 你。解决：用公网 IP、或自建 Gitea 同机、或 frp 穿透。

**Q：不用 Webhook 行吗？**  
行，但要手动点 Jenkins「Build Now」，或设定时轮询。

### 下一步

→ [步骤 10：配置镜像仓库](#步骤-10配置镜像仓库)

---

## 步骤 10：配置镜像仓库

对应 [DEPLOYMENT_GUIDE.md 第 12 章](./DEPLOYMENT_GUIDE.md#12-第八步配置阿里云镜像仓库)。

### 做什么

在 **阿里云** 创建存放 Docker 镜像的仓库（ACR）。

### 为什么需要

Jenkins 构建流程：

```
docker build  →  本地生成镜像
docker push   →  推到阿里云 ACR
K3s pull      →  从 ACR 拉镜像运行
```

**中转站** 的好处：保留历史版本、多台机器能拉同一镜像、国内速度快。

### 你要做的

1. 阿里云创建 **命名空间**（如 `enterprise`）  
2. 创建 **镜像仓库**（如 `enterprise-app`）  
3. 设置登录密码  
4. Jenkins 添加凭据 ID：`aliyun-registry-cred`  
5. K3s 创建 pull Secret（私有仓库必须）  

### 成功标志

```bash
docker login registry.cn-hangzhou.aliyuncs.com   # 成功
```

### 下一步

→ [步骤 11：创建流水线](#步骤-11创建流水线)

---

## 步骤 11：创建 CI/CD 流水线

对应 [DEPLOYMENT_GUIDE.md 第 13 章](./DEPLOYMENT_GUIDE.md#13-第九步创建-cicd-流水线)。

### 做什么

编写 **Jenkinsfile**（流水线剧本），并在 Jenkins 创建 **Pipeline 任务**。

### 为什么需要

Jenkins 不知道你的项目是 Maven 还是 Node、镜像名叫什么、部署到哪个命名空间——**Jenkinsfile 就是说明书**。

### 流水线典型阶段

| 阶段 | 做什么 |
|------|--------|
| Checkout | Git 拉代码 |
| Build | mvn package 编译 |
| Docker Build & Push | 打镜像并推阿里云 |
| Deploy to K3s | kubectl 更新 Deployment |

### 你要准备的文件

1. **Dockerfile**（项目根目录）—— 怎么打包成镜像  
2. **Jenkinsfile**（项目根目录）—— Jenkins 执行步骤  
3. **k8s Deployment**—— K3s 里应用的初始配置  

### 在 Jenkins 创建任务

- 类型：Pipeline  
- 定义：Pipeline script from SCM  
- SCM：Git，填仓库地址和 `github-token`  
- Script Path：`Jenkinsfile`  
- 勾选：GitHub hook trigger  

### 成功标志

手动点 **Build Now**，Console Output 里各阶段绿色成功。

### 常见错误

| 阶段失败 | 可能原因 |
|----------|----------|
| Checkout | Token/SSH 不对，jenkins 拉不到代码 |
| Build | 项目缺 pom.xml 或编译错误 |
| Docker | jenkins 不在 docker 组 |
| Deploy | kubeconfig 或 RBAC 问题 |

### 下一步

→ [步骤 12：整体验证](#步骤-12整体验证)

---

## 步骤 12：整体验证

对应 [DEPLOYMENT_GUIDE.md 第 14 章](./DEPLOYMENT_GUIDE.md#14-第十步首次部署验证)。

### 做什么

从 **改代码 → push → 自动部署 → 访问应用** 走通全流程。

### 为什么需要

环节多，任何一步漏配都可能「以为成功了其实没有」。

### 验证清单

1. **改一行代码**，push 到 Git  
2. **Jenkins** 自动出现新构建，且成功（蓝色）  
3. **K3s** 里 Pod 在 Running：
   ```bash
   sudo k3s kubectl get pods -n enterprise-platform
   ```
4. **阿里云** 控制台能看到新镜像 tag  
5. **应用能访问**（Ingress 或 port-forward）  

### 成功标志

**你只 push 了代码，几分钟后线上就是新版本**——这就是整套环境的最终目标。

---

## 附录：步骤依赖关系图

```
步骤1 准备
   ↓
步骤2 Git+仓库（代码源头）
   ↓
步骤3 上传脚本
   ↓
步骤4 系统初始化 ──→ 步骤5 Docker ──→ 步骤6 K3s
                              ↓              ↓
                         步骤7 Jenkins ←────┘
                              ↓
                         步骤8 Jenkins网页
                              ↓
步骤2 ──────────────→ 步骤9 Git↔Jenkins
                              ↓
                         步骤10 镜像仓库
                              ↓
                         步骤11 流水线
                              ↓
                         步骤12 验证 ✅
```

---

## 附录：出问题时先看哪里

| 问题发生在 | 先看 |
|------------|------|
| push 代码 | 步骤 2 |
| SSH 连不上服务器 | 步骤 1、3 |
| 脚本执行失败 | 步骤 4～7 对应章节 + FAQ |
| Jenkins 网页打不开 | 步骤 7、8 |
| 构建 Checkout 失败 | 步骤 2.3、9 |
| 构建 Docker 失败 | 步骤 5 |
| 构建 Deploy 失败 | 步骤 6、7 |
| push 不自动构建 | 步骤 9 Webhook |
| 应用起不来 | 步骤 11、12 |

更详细故障排查：[DEPLOYMENT_GUIDE.md FAQ](./DEPLOYMENT_GUIDE.md#17-常见问题-faq)

---

**读完全文后，打开 [CHECKLIST.md](./CHECKLIST.md) 边做边勾选，命令细节查 [DEPLOYMENT_GUIDE.md](./DEPLOYMENT_GUIDE.md)。**

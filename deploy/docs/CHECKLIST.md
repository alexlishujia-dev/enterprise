# 部署检查清单

打印或在部署时逐项勾选，确保不遗漏关键步骤。

---

## 阶段 0：Git 与 GitHub（本地 + 服务器）

- [ ] 已注册 GitHub 账号并完成邮箱验证
- [ ] Windows 已安装 Git（`git --version` 有输出）
- [ ] 已配置 `git config user.name` 和 `user.email`
- [ ] 已在 GitHub 创建 **一个** `EnterprisePlatform` 仓库（Monorepo，见 [GITHUB_REPO_SETUP.md](./GITHUB_REPO_SETUP.md)）
- [ ] push 后 GitHub 上有 `src/`（API）、`frontend/`、`mobile/`，无 `node_modules`
- [ ] 已生成 SSH 密钥并添加到 GitHub（或已准备 HTTPS Token）
- [ ] `ssh -T git@github.com` 认证成功（SSH 方式）
- [ ] 本地项目已 `git push` 到 GitHub，浏览器能看到代码
- [ ] （服务器）已执行 `sudo bash scripts/06-git-github-setup.sh`
- [ ] （服务器）`sudo -u jenkins git ls-remote` 能访问仓库

---

## 部署前

- [ ] 服务器为 Ubuntu 20.04，内存 ≥ 4GB，磁盘 ≥ 40GB
- [ ] 可通过 SSH 登录服务器
- [ ] 服务器能访问外网（`ping mirrors.aliyun.com` 成功）
- [ ] 云服务器安全组已放行：22、80、443、8080、6443、8472/udp、10250
- [ ] 已注册阿里云账号
- [ ] 已记录服务器 IP、GitHub 仓库地址

---

## 阶段 1：上传文件

- [ ] `deploy/` 目录已上传到服务器（如 `/opt/EnterprisePlatform/deploy`）
- [ ] `chmod +x scripts/*.sh` 已执行
- [ ] `ls scripts/` 能看到 01～06 脚本

---

## 阶段 2：系统初始化（01-system-init.sh）

- [ ] 脚本执行无报错退出
- [ ] `timedatectl` 时区为 Asia/Shanghai
- [ ] `free -h` 显示 Swap 约 4GB
- [ ] `sudo ufw status` 显示 8080、6443 等已 ALLOW
- [ ] `sysctl net.ipv4.ip_forward` 返回 1

---

## 阶段 3：Docker（02-docker-install.sh）

- [ ] `docker --version` 有输出
- [ ] `docker info | grep "Registry Mirrors"` 有国内镜像地址
- [ ] `docker run --rm hello-world` 成功
- [ ] （推荐）已配置阿里云个人镜像加速并 restart docker

---

## 阶段 4：K3s（03-k3s-install.sh）

- [ ] `sudo k3s kubectl get nodes` 状态为 Ready
- [ ] `sudo k3s kubectl get pods -A` 系统 Pod 为 Running
- [ ] `enterprise-platform` 命名空间已创建
- [ ] jenkins-rbac 已 apply

---

## 阶段 5：Jenkins（04-jenkins-install.sh）

- [ ] `systemctl status jenkins` 为 active (running)
- [ ] 浏览器能打开 `http://<IP>:8080`
- [ ] 已记录初始管理员密码
- [ ] `sudo -u jenkins docker ps` 成功
- [ ] `sudo -u jenkins kubectl get ns` 成功

---

## 阶段 6：Git 服务器配置（06-git-github-setup.sh）

- [ ] jenkins 用户 Git 身份已配置
- [ ] （若用 SSH）jenkins 公钥已添加到 GitHub
- [ ] `sudo -u jenkins ssh -T git@github.com` 成功

---

## 阶段 7：Jenkins 控制台配置

- [ ] 已完成首次向导（推荐插件已安装）
- [ ] 已安装：GitHub Integration、Pipeline、Docker Pipeline
- [ ] 已创建管理员账号
- [ ] `bash scripts/health-check.sh` 全部 OK

---

## 阶段 8：GitHub 与 Jenkins 集成

- [ ] 已创建 Jenkins 专用 Personal Access Token（repo + admin:repo_hook）
- [ ] Jenkins 凭据 `github-token` 已添加
- [ ] GitHub 仓库 Webhook 已添加（Payload URL 正确）
- [ ] Webhook Recent Deliveries 有绿色成功记录（或确认有公网 IP）

---

## 阶段 9：阿里云镜像仓库

- [ ] 已创建命名空间和镜像仓库
- [ ] Jenkins 凭据 `aliyun-registry-cred` 已添加
- [ ] `docker login registry.cn-hangzhou.aliyuncs.com` 成功
- [ ] K8s Secret `aliyun-registry` 已在 enterprise-platform 命名空间创建

---

## 阶段 10：流水线

- [ ] 项目根目录有 `Dockerfile`
- [ ] 项目根目录有 `Jenkinsfile`（变量已修改）
- [ ] `app-deployment.example.yaml` 镜像地址已修改并 apply
- [ ] Jenkins Pipeline 任务已创建并 Save
- [ ] 手动 Build Now 构建成功（蓝色）

---

## 阶段 11：端到端验证

- [ ] push 代码后 Jenkins 自动触发构建
- [ ] `kubectl get pods -n enterprise-platform` Pod 为 Running
- [ ] 阿里云控制台能看到新推送的镜像 tag
- [ ] 应用可访问（Ingress 或 port-forward）

---

## 部署完成 ✅

全部勾选后，CI/CD 环境搭建完成。

**文档索引：**

- 组件概念：[BEGINNER_GUIDE.md](./BEGINNER_GUIDE.md)
- **逐步详解：[STEP_BY_STEP.md](./STEP_BY_STEP.md)** ★
- 操作命令：[DEPLOYMENT_GUIDE.md](./DEPLOYMENT_GUIDE.md)
- 权限命令：[PERMISSIONS.md](./PERMISSIONS.md)

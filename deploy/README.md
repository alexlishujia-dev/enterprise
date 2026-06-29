# GitHub + Jenkins + K3s 部署包

面向 **Ubuntu 20.04 / 4GB 内存 / 国内网络** 的 CI/CD 环境一键部署方案。

---

## 从这里开始

| 文档 | 说明 |
|------|------|
| **[docs/GITHUB_SSH_JENKINS.md](docs/GITHUB_SSH_JENKINS.md)** | **Jenkins 服务器 SSH 密钥（详细步骤）** |
| **[docs/GITHUB_SSH_WINDOWS.md](docs/GITHUB_SSH_WINDOWS.md)** | Windows 生成 SSH 密钥 |
| **[docs/GITHUB_REPO_SETUP.md](docs/GITHUB_REPO_SETUP.md)** | **前端+后端 API 如何创建 GitHub 仓库** |
| **[docs/BEGINNER_GUIDE.md](docs/BEGINNER_GUIDE.md)** | 各组件有什么用、整体流程（概念入门） |
| **[docs/STEP_BY_STEP.md](docs/STEP_BY_STEP.md)** | **每一步详细说明：做什么、为什么、成功标志（新手强烈推荐）** |
| **[docs/DEPLOYMENT_GUIDE.md](docs/DEPLOYMENT_GUIDE.md)** | 详细部署手册（具体操作命令） |
| [docs/CHECKLIST.md](docs/CHECKLIST.md) | 部署检查清单（逐项勾选） |
| [docs/PERMISSIONS.md](docs/PERMISSIONS.md) | 权限与运维命令手册 |

---

## 5 分钟快速开始

```bash
# 0. 先在本地完成 GitHub 仓库创建并 push 代码（见 DEPLOYMENT_GUIDE.md 第 4 章）

# 1. 上传 deploy 目录到服务器后
cd /opt/EnterprisePlatform/deploy
chmod +x scripts/*.sh

# 2. 按顺序执行（不可跳步）
sudo bash scripts/01-system-init.sh
sudo bash scripts/02-docker-install.sh
sudo bash scripts/03-k3s-install.sh
sudo bash scripts/04-jenkins-install.sh
sudo bash scripts/06-git-github-setup.sh

# 3. 浏览器打开 Jenkins，按 DEPLOYMENT_GUIDE.md 第 10～13 章继续配置
# 4. 健康检查
bash scripts/health-check.sh
```

---

## 架构

```
GitHub ──Webhook──▶ Jenkins ──docker build/push──▶ 阿里云 ACR
                      │
                      └──kubectl──▶ K3s (enterprise-platform 命名空间)
```

---

## 目录结构

```
deploy/
├── README.md                          # 本文件（索引）
├── docs/
│   ├── DEPLOYMENT_GUIDE.md            # ★ 详细部署手册
│   ├── CHECKLIST.md                   # 部署检查清单
│   └── PERMISSIONS.md                 # 权限命令
├── configs/
│   ├── apt-sources-cn.list            # 阿里云 apt 源
│   ├── daemon.json                    # Docker 镜像加速
│   ├── registries.yaml                # K3s containerd 镜像加速
│   └── jenkins/Jenkinsfile.example    # Pipeline 示例
├── k8s/
│   ├── namespace.yaml
│   ├── jenkins-rbac.yaml
│   └── app-deployment.example.yaml
└── scripts/
    ├── 01-system-init.sh              # 系统初始化
    ├── 02-docker-install.sh           # Docker 安装
    ├── 03-k3s-install.sh              # K3s 安装
    ├── 04-jenkins-install.sh          # Jenkins 安装
    ├── 05-github-webhook-setup.sh     # GitHub Webhook 说明
    ├── 06-git-github-setup.sh         # Git 安装与 GitHub SSH 配置
    └── health-check.sh                # 环境健康检查
```

---

## 硬件要求

| 项目 | 最低 |
|------|------|
| 系统 | Ubuntu 20.04 LTS |
| CPU | 2 核 |
| 内存 | 4 GB（脚本自动配置 4GB swap） |
| 磁盘 | 40 GB+ |

---

## 常见问题速查

| 现象 | 查看 |
|------|------|
| Git / GitHub 如何安装配置 | [DEPLOYMENT_GUIDE.md 第 4 章](docs/DEPLOYMENT_GUIDE.md#4-git-与-github-环境搭建) |
| 不知道从哪开始 | [DEPLOYMENT_GUIDE.md 第 3 节](docs/DEPLOYMENT_GUIDE.md#3-整体流程一览) |
| 镜像拉取超时 | [DEPLOYMENT_GUIDE.md FAQ Q1](docs/DEPLOYMENT_GUIDE.md#q1docker-pull-或-pod-镜像拉取超时) |
| git clone / push 失败 | [DEPLOYMENT_GUIDE.md FAQ Q7/Q8](docs/DEPLOYMENT_GUIDE.md#q7git-clone-或-jenkins-checkout-失败) |
| Jenkins 无法访问 Docker/K8s | [PERMISSIONS.md](docs/PERMISSIONS.md) |
| GitHub Webhook 不触发 | [DEPLOYMENT_GUIDE.md 第 11.4 节](docs/DEPLOYMENT_GUIDE.md#114-验证-webhook-是否可达) |

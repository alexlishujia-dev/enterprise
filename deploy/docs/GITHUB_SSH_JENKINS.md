# Jenkins 服务器 SSH 密钥配置 —— 详细步骤

> 对应 [DEPLOYMENT_GUIDE.md 4.5.4 / 9.3 节](./DEPLOYMENT_GUIDE.md#454-jenkins-服务器-ssh-密钥)  
> **何时做：** 安装完 Jenkins（`04-jenkins-install.sh`）之后  
> **在哪做：** Ubuntu **服务器**上（不是 Windows）

---

## 一、为什么 Windows 配了，服务器还要再配？

| 环境 | 用户 | 用途 |
|------|------|------|
| **你的 Windows 电脑** | `92320` | 你开发时 `git push` 代码 |
| **Ubuntu 服务器** | `jenkins` | Jenkins 构建时 `git clone` 拉代码 |

这是 **两把不同的钥匙**：

- Windows 的密钥 → 存在 `C:\Users\92320\.ssh\`
- Jenkins 的密钥 → 存在 `/var/lib/jenkins/.ssh/`

**Jenkins 不会用你 Windows 上的密钥**，必须在服务器上单独为 `jenkins` 用户生成并加到 GitHub。

---

## 二、整体流程（5 步）

```
① 服务器运行 06 脚本
② 为 jenkins 生成 SSH 密钥对
③ 复制公钥到 GitHub（个人 SSH keys）
④ 配置 jenkins 的 SSH config
⑤ 测试 sudo -u jenkins ssh -T git@github.com
```

---

## 三、前提条件

- [ ] **已执行 `04-jenkins-install.sh`**（必须！否则没有 `jenkins` 用户）
- [ ] 验证：`id jenkins` 有输出
- [ ] Jenkins 在运行：`systemctl status jenkins`
- [ ] 服务器能访问 github.com
- [ ] 代码仓库已存在（如 `alexlishujia-dev/enterprise`）

检查 Jenkins 用户是否存在：

```bash
id jenkins
# 期望：uid=xxx(jenkins) gid=xxx(jenkins) ...

# 若报「no such user」，先安装 Jenkins：
cd /opt/enterprise/deploy
sudo bash scripts/04-jenkins-install.sh
```

---

## 四、方式 A：用脚本自动配置（推荐）

### 步骤 1：进入 deploy 目录

```bash
cd /opt/enterprise/deploy
```

### 步骤 2：运行脚本

```bash
sudo bash scripts/06-git-github-setup.sh
```

### 步骤 3：按提示输入

脚本会依次询问：

| 提示 | 填什么 | 示例 |
|------|--------|------|
| Git 用户名 (user.name) | 随意，用于 commit 记录 | `jenkins` 或 `alexlishujia-dev` |
| Git 邮箱 (user.email) | 你的邮箱 | `alexlishujia@gmail.com` |
| 是否为 jenkins 生成 SSH 密钥? | 输入 **`y`** | `y` |
| GitHub 用户名 | 你的 GitHub 用户名 | `alexlishujia-dev` |

### 步骤 4：查看并复制公钥

**方式 1：脚本运行后屏幕会直接打印公钥**，复制整行即可。

**方式 2：用命令查看（脚本跑完后或手动生成后）**

在 **Ubuntu 服务器**上执行：

```bash
# 查看 jenkins 用户的公钥（推荐）
sudo cat /var/lib/jenkins/.ssh/id_ed25519_github.pub
```

或：

```bash
# 同上，另一种写法
sudo -u jenkins cat ~/.ssh/id_ed25519_github.pub
```

**方式 3：确认公钥文件是否存在**

```bash
sudo ls -la /var/lib/jenkins/.ssh/
```

应能看到 `id_ed25519_github.pub` 文件。

**复制说明：**

- 输出是 **一行**，以 `ssh-ed25519` 开头，以邮箱或注释结尾  
- **整行复制**，不要换行、不要漏字符  
- 复制的是 **`.pub` 公钥**，不是无后缀的私钥  

示例输出：

```
ssh-ed25519 AAAAC3NzaC1lZDI1NTE5AAAAIxxxxxxxxxxxxxxxx alexlishujia-dev@jenkins-ci
```

### 步骤 5：添加到 GitHub

1. 打开 **https://github.com/settings/keys**（注意：是 **SSH and GPG keys**，不是仓库的 Deploy keys）
2. **New SSH key**
3. 填写：

| 字段 | 值 |
|------|-----|
| Title | `jenkins-server` 或 `jenkins-alexlee` |
| Key type | Authentication Key |
| Key | 粘贴公钥整行 |

4. **Add SSH key**

### 步骤 6：验证

```bash
sudo -u jenkins ssh -T git@github.com
```

第一次可能问 `Are you sure you want to continue connecting (yes/no)?` → 输入 **`yes`**

**成功：**

```
Hi alexlishujia-dev! You've successfully authenticated, but GitHub does not provide shell access.
```

### 步骤 7：验证能否访问你的仓库

```bash
sudo -u jenkins git ls-remote git@github.com:alexlishujia-dev/enterprise.git
```

有输出 commit hash 列表即成功。

---

## 五、方式 B：完全手动配置（脚本失败时用）

### 5.1 创建 .ssh 目录

```bash
sudo mkdir -p /var/lib/jenkins/.ssh
sudo chmod 700 /var/lib/jenkins/.ssh
sudo chown jenkins:jenkins /var/lib/jenkins/.ssh
```

### 5.2 生成密钥（以 jenkins 用户身份）

```bash
sudo -u jenkins ssh-keygen -t ed25519 \
  -C "alexlishujia-dev@jenkins-ci" \
  -f /var/lib/jenkins/.ssh/id_ed25519_github \
  -N ""
```

- `-N ""` 表示不设 passphrase（Jenkins 无人值守，不能每次输密码）

### 5.3 查看并复制公钥

```bash
# 查看公钥（复制整行输出）
sudo cat /var/lib/jenkins/.ssh/id_ed25519_github.pub
```

```bash
# 或
sudo -u jenkins cat /var/lib/jenkins/.ssh/id_ed25519_github.pub
```

复制 **整行**（`ssh-ed25519` 开头）→ 加到 GitHub **Settings → SSH and GPG keys**。

### 5.4 创建 SSH config

```bash
sudo tee /var/lib/jenkins/.ssh/config << 'EOF'
Host github.com
  HostName github.com
  User git
  IdentityFile /var/lib/jenkins/.ssh/id_ed25519_github
  StrictHostKeyChecking accept-new
EOF

sudo chown jenkins:jenkins /var/lib/jenkins/.ssh/config
sudo chmod 600 /var/lib/jenkins/.ssh/config
```

### 5.5 设置私钥权限

```bash
sudo chmod 600 /var/lib/jenkins/.ssh/id_ed25519_github
sudo chown jenkins:jenkins /var/lib/jenkins/.ssh/id_ed25519_github
sudo chown jenkins:jenkins /var/lib/jenkins/.ssh/id_ed25519_github.pub
```

### 5.6 测试

```bash
sudo -u jenkins ssh -T git@github.com
sudo -u jenkins git ls-remote git@github.com:alexlishujia-dev/enterprise.git
```

---

## 六、文件位置说明

```
/var/lib/jenkins/.ssh/
├── id_ed25519_github       ← 私钥（只有 jenkins 能读，权限 600）
├── id_ed25519_github.pub   ← 公钥（已复制到 GitHub）
└── config                  ← 告诉 SSH 连接 GitHub 用哪把钥匙
```

查看文件：

```bash
sudo ls -la /var/lib/jenkins/.ssh/
```

期望权限：

```
drwx------  jenkins jenkins  .ssh/
-rw-------  jenkins jenkins  id_ed25519_github
-rw-r--r--  jenkins jenkins  id_ed25519_github.pub
-rw-------  jenkins jenkins  config
```

---

## 七、和 Jenkins Pipeline 的关系

配置完成后，Jenkins 任务里 Git 仓库地址要用 **SSH 格式**：

```
git@github.com:alexlishujia-dev/enterprise.git
```

**不是：**

```
https://github.com/alexlishujia-dev/enterprise.git
```

若 Pipeline 用 HTTPS，则需要 **GitHub Token 凭据**，不需要 SSH 密钥（二选一即可）。

| Jenkins 仓库地址格式 | 需要的凭据 |
|---------------------|------------|
| `git@github.com:...` | SSH 密钥（本文） |
| `https://github.com/...` | GitHub Token（`github-token`） |

---

## 八、常见问题

### Q1：`jenkins 用户不存在`

先装 Jenkins：

```bash
sudo bash scripts/04-jenkins-install.sh
```

### Q2：`Permission denied (publickey)`

1. 公钥是否加到 **Settings → SSH and GPG keys**（不是 Deploy keys）
2. `config` 文件路径是否正确
3. 权限是否正确（见第六节）
4. 重新测试：

```bash
sudo -u jenkins ssh -vT git@github.com
```

### Q3：Windows 密钥和 Jenkins 密钥能共用吗？

**不能直接用。** 私钥不能复制给 jenkins 用户（权限和安全问题）。  
应在服务器上 **单独生成** 一把给 jenkins。

### Q4：Deploy keys 和个人 SSH keys 区别？

| | 个人 SSH keys | Deploy keys |
|--|---------------|-------------|
| 位置 | 账号 Settings | 某个仓库 Settings |
| 访问范围 | 账号下所有有权限的仓库 | 仅该仓库 |
| 适合 | Jenkins 拉多个项目 | 仅一个仓库、只读/部署 |

**Jenkins 推荐：** 加到 **个人 SSH keys**（或仓库 Deploy key 并勾选 Allow write access）。

### Q5：GitHub 访问慢或超时

改用 HTTPS + Token，见 [DEPLOYMENT_GUIDE 第 11 章](./DEPLOYMENT_GUIDE.md#11-第七步github-与-jenkins-集成)：

```bash
git config --global http.version HTTP/1.1
```

Jenkins 凭据 ID：`github-token`

---

## 九、检查清单

- [ ] `id jenkins` 用户存在
- [ ] `/var/lib/jenkins/.ssh/id_ed25519_github.pub` 已生成
- [ ] 公钥已加到 GitHub **SSH and GPG keys**
- [ ] `/var/lib/jenkins/.ssh/config` 已创建
- [ ] `sudo -u jenkins ssh -T git@github.com` 显示 `Hi alexlishujia-dev!`
- [ ] `sudo -u jenkins git ls-remote git@github.com:.../enterprise.git` 有输出

全部打勾后，Jenkins 就能从 GitHub 拉代码构建了。

---

## 附录：查看公钥命令速查

| 场景 | 命令 |
|------|------|
| **Jenkins 服务器（Linux）** | `sudo cat /var/lib/jenkins/.ssh/id_ed25519_github.pub` |
| Jenkins 服务器（另一种） | `sudo -u jenkins cat ~/.ssh/id_ed25519_github.pub` |
| 列出 jenkins 的 .ssh 目录 | `sudo ls -la /var/lib/jenkins/.ssh/` |
| **Windows 开发机** | `type %USERPROFILE%\.ssh\id_ed25519_github.pub` |
| Windows PowerShell | `Get-Content $env:USERPROFILE\.ssh\id_ed25519_github.pub` |

---

1. [DEPLOYMENT_GUIDE 第 11 章](./DEPLOYMENT_GUIDE.md#11-第七步github-与-jenkins-集成) — Webhook + Jenkins 凭据  
2. [DEPLOYMENT_GUIDE 第 13 章](./DEPLOYMENT_GUIDE.md#13-第九步创建-cicd-流水线) — 创建 Pipeline  

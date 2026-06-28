# Windows 生成 SSH 密钥 —— 新手完全指南

> 配合 [DEPLOYMENT_GUIDE.md 第 4.5 节](./DEPLOYMENT_GUIDE.md#45-配置-ssh-密钥推荐免密-pushpull) 使用。  
> 若觉得 SSH 太复杂，可跳过本文，改用 [HTTPS + Token](./DEPLOYMENT_GUIDE.md#47-使用-https--personal-access-token备选)（见文末对比）。

---

## 一、先搞懂：SSH 密钥到底是什么？

### 用「锁和钥匙」来理解

| 概念 | 是什么 | 放在哪 | 给谁 |
|------|--------|--------|------|
| **私钥** | 你的钥匙 | 只在 **自己电脑** 上 | 绝对不能给别人 |
| **公钥** | 对应的锁 | 复制到 **GitHub 网站** | 可以公开 |

**工作流程：**

```
你电脑上的 Git 要 push 代码到 GitHub
    → GitHub 说：「证明你是本人」
    → 你的电脑用「私钥」自动解锁
    → GitHub 用你之前存的「公钥」验证
    → 通过，允许 push
```

**好处：** 配置一次后，`git push` **不用每次输密码**（比 HTTPS 省事）。

---

## 二、你需要准备什么？

- Windows 10 / 11
- 已安装 [Git for Windows](https://git-scm.com/download/win)（自带 OpenSSH 或 Git Bash）
- 已登录 GitHub（Google 登录也可以）
- 约 10 分钟

---

## 三、完整操作步骤（一步一步来）

### 步骤 1：打开 PowerShell

1. 按 `Win` 键，输入 `PowerShell`
2. 点击 **Windows PowerShell**（普通打开即可，不必管理员）

---

### 步骤 2：生成密钥对

**复制下面整行**，粘贴到 PowerShell，按 **Enter**：

```powershell
ssh-keygen -t ed25519 -C "你的GitHub邮箱" -f "$env:USERPROFILE\.ssh\id_ed25519_github"
```

> 把 `你的GitHub邮箱` 换成你在 GitHub 上绑定的邮箱（Settings → Emails 里能看到）。  
> 例如：`-C "abc@gmail.com"`

#### 接下来会出现 3 次提问（很重要）

**第 1 次：**

```
Enter passphrase (empty for no passphrase):
```

- **意思：** 要不要给这把钥匙再加一层密码？
- **新手建议：** 直接按 **Enter**（不设置，最简单）
- 若设了密码，每次 push 可能要输这个密码

**第 2 次：**

```
Enter same passphrase again:
```

- 再按 **Enter**（若上一步没设密码）

**第 3 次：成功提示类似：**

```
Your identification has been saved in C:\Users\你的用户名\.ssh\id_ed25519_github
Your public key has been saved in C:\Users\你的用户名\.ssh\id_ed25519_github.pub
```

✅ 到这里，电脑上已经有一对密钥了。

---

### 步骤 3：查看生成了哪些文件

在 PowerShell 执行：

```powershell
dir $env:USERPROFILE\.ssh
```

应能看到（名字可能略有不同）：

| 文件名 | 是什么 | 能给别人吗 |
|--------|--------|------------|
| `id_ed25519_github` | **私钥**（钥匙） | ❌ 绝对不能 |
| `id_ed25519_github.pub` | **公钥**（锁） | ✅ 要复制到 GitHub |

---

### 步骤 4：复制公钥内容

在 PowerShell 执行：

```powershell
Get-Content $env:USERPROFILE\.ssh\id_ed25519_github.pub
```

**屏幕会输出一行文字**，类似：

```
ssh-ed25519 AAAAC3NzaC1lZDI1NTE5AAAAIxxxxxxxxxxxxxxxxxxxxxxx abc@gmail.com
```

**操作：**

1. 用鼠标 **选中整行**（从 `ssh-ed25519` 开头到邮箱结尾）
2. **Ctrl + C** 复制
3. **不要** 有多余换行或空格

> 💡 也可以执行 `notepad $env:USERPROFILE\.ssh\id_ed25519_github.pub` 用记事本打开再复制。

---

### 步骤 5：把公钥粘贴到 GitHub

1. 浏览器打开 https://github.com 并登录  
2. 右上角 **头像** → **Settings**  
3. 左侧菜单最下方附近：**SSH and GPG keys**  
4. 点绿色按钮 **New SSH key**  
5. 填写：

| 字段 | 填什么 |
|------|--------|
| Title | `我的Windows电脑`（随便起，方便辨认） |
| Key type | Authentication Key |
| Key | **Ctrl+V 粘贴** 刚才复制的公钥整行 |

6. 点 **Add SSH key**  
7. GitHub 可能要求再输入一次密码（Google 登录的确认 GitHub 身份）

✅ 公钥已上传到 GitHub。

---

### 步骤 6：告诉电脑「连接 GitHub 时用这把钥匙」

Windows 需要一个小配置文件，否则可能找不到密钥。

#### 6.1 创建/编辑 config 文件

在 PowerShell 执行：

```powershell
notepad $env:USERPROFILE\.ssh\config
```

若提示「文件不存在，是否创建？」→ 点 **是**。

#### 6.2 在记事本里粘贴以下内容并保存

```
Host github.com
  HostName github.com
  User git
  IdentityFile ~/.ssh/id_ed25519_github
  StrictHostKeyChecking accept-new
```

保存后关闭记事本。

> **作用：** 以后连接 `github.com` 时，自动使用 `id_ed25519_github` 这把钥匙。

---

### 步骤 7：启动 ssh-agent 并加载密钥（可选但推荐）

在 PowerShell **逐行** 执行：

```powershell
Get-Service ssh-agent | Set-Service -StartupType Manual
Start-Service ssh-agent
ssh-add $env:USERPROFILE\.ssh\id_ed25519_github
```

若 `ssh-add` 成功，会显示：

```
Identity added: C:\Users\xxx\.ssh\id_ed25519_github
```

> 若 `Start-Service ssh-agent` 报错，可跳过这步，很多情况下步骤 6 的 config 已够用。

---

### 步骤 8：测试是否成功

在 PowerShell 执行：

```powershell
ssh -T git@github.com
```

**第一次** 可能问：

```
Are you sure you want to continue connecting (yes/no)?
```

输入 **`yes`** 回车。

**成功** 会看到类似：

```
Hi 你的GitHub用户名! You've successfully authenticated, but GitHub does not provide shell access.
```

看到 **`Hi 你的用户名!`** 就说明 SSH 配置成功 ✅

**失败** 常见提示：

| 提示 | 原因 |
|------|------|
| `Permission denied (publickey)` | 公钥没加到 GitHub，或 config 路径不对 |
| `Could not resolve hostname` | 网络/DNS 问题 |

---

### 步骤 9：用 SSH 地址 push 代码

远程仓库地址要用 **SSH 格式**，不是 HTTPS：

```powershell
# ✅ SSH 格式（配好密钥后用这个）
git remote add origin git@github.com:你的GitHub用户名/EnterprisePlatform.git

# ❌ HTTPS 格式（不需要 SSH 密钥，但需要 Token）
# git remote add origin https://github.com/你的用户名/EnterprisePlatform.git
```

若之前已经加了 HTTPS 地址，可以改成 SSH：

```powershell
git remote set-url origin git@github.com:你的GitHub用户名/EnterprisePlatform.git
git remote -v
```

然后正常 push：

```powershell
git push -u origin main
```

**不应再要求输入 GitHub 密码**（若设了 passphrase 可能问密钥密码）。

---

## 四、全流程一张图

```
步骤2  ssh-keygen          →  电脑生成 私钥 + 公钥
步骤4  复制 .pub 文件       →  公钥内容
步骤5  GitHub 网页粘贴      →  GitHub 记住你的锁
步骤6  编辑 .ssh/config     →  电脑知道用哪把钥匙
步骤8  ssh -T git@github.com →  测试能不能开锁
步骤9  git push             →  以后 push 自动认证
```

---

## 五、常见问题

### Q1：`ssh-keygen` 不是内部或外部命令？

**原因：** 没装 Git，或 OpenSSH 未启用。

**解决：**

1. 安装 [Git for Windows](https://git-scm.com/download/win)  
2. 或用 **Git Bash**（开始菜单里找）代替 PowerShell 执行同样命令  
3. 或在 Windows：**设置 → 应用 → 可选功能 → 添加 OpenSSH 客户端**

---

### Q2：GitHub 用 Google 登录，和 SSH 有关系吗？

**没有。** Google 只用于 **网页登录** GitHub。  
SSH 密钥是 **Git 命令行 push/pull** 用的，两套独立。

---

### Q3：私钥可以发给别人吗？

**绝对不行。** 私钥 = 你的身份。泄露后别人能以你的名义 push 代码。  
只有 **`.pub` 公钥** 可以放到 GitHub。

---

### Q4：换电脑了怎么办？

新电脑重新做一遍本文步骤，在 GitHub 再 **New SSH key** 加一个新公钥（Title 写「新电脑」）。  
旧电脑的密钥可在 GitHub 上 Delete 掉。

---

### Q5：SSH 太麻烦，有更简单的吗？

有。改用 **HTTPS + Personal Access Token**：

1. GitHub 创建 Token（Settings → Developer settings → Tokens）  
2. 远程地址用：`https://github.com/用户名/仓库.git`  
3. push 时密码处 **粘贴 Token**（不是 Google 密码）

详见 [DEPLOYMENT_GUIDE 4.7 节](./DEPLOYMENT_GUIDE.md#47-使用-https--personal-access-token备选)。

| 方式 | 优点 | 缺点 |
|------|------|------|
| **SSH 密钥** | 配好后 push 免密 | 初次配置步骤多 |
| **HTTPS + Token** | 步骤少 | Token 过期要重新生成 |

**新手二选一即可**，不必两个都配。

---

## 六、检查清单

- [ ] PowerShell 执行 `ssh-keygen` 成功，有 `.pub` 文件  
- [ ] 公钥已粘贴到 GitHub → Settings → SSH and GPG keys  
- [ ] 已创建 `%USERPROFILE%\.ssh\config` 并保存  
- [ ] `ssh -T git@github.com` 显示 `Hi 用户名!`  
- [ ] `git remote -v` 地址是 `git@github.com:...` 格式  
- [ ] `git push` 能成功  

---

**配置成功后，继续 [GITHUB_REPO_SETUP.md](./GITHUB_REPO_SETUP.md) 把代码 push 到 GitHub。**

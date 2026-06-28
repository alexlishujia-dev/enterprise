#!/bin/bash
# =============================================================================
# 06-git-github-setup.sh - 服务器 Git 安装与 GitHub 连接配置
# 用法: sudo bash 06-git-github-setup.sh
# 可选环境变量: GIT_USER_NAME, GIT_USER_EMAIL, GITHUB_USER
# =============================================================================
set -euo pipefail

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'
log()  { echo -e "${GREEN}[INFO]${NC} $*"; }
warn() { echo -e "${YELLOW}[WARN]${NC} $*"; }

[[ $EUID -eq 0 ]] || { echo "请使用 root 运行: sudo bash $0"; exit 1; }

# -----------------------------------------------------------------------------
# 1. 安装 Git
# -----------------------------------------------------------------------------
if command -v git &>/dev/null; then
  log "Git 已安装: $(git --version)"
else
  log "安装 Git..."
  apt-get update -y
  apt-get install -y git
fi

# -----------------------------------------------------------------------------
# 2. 测试 GitHub 网络连通性
# -----------------------------------------------------------------------------
log "测试 GitHub 连通性..."
if curl -sf --connect-timeout 10 -o /dev/null https://github.com; then
  log "GitHub 访问正常"
else
  warn "无法直接访问 github.com，国内网络可能需配置代理或 hosts"
  warn "Jenkins 拉代码可使用 HTTPS + Personal Access Token（见部署文档第 11 章）"
fi

# -----------------------------------------------------------------------------
# 3. 为 jenkins 用户配置 Git（CI 拉代码用）
# -----------------------------------------------------------------------------
configure_git_user() {
  local user="$1"
  local name="${GIT_USER_NAME:-}"
  local email="${GIT_USER_EMAIL:-}"

  if [[ -z "$name" ]]; then
    read -rp "请输入 Git 用户名 (user.name) [${user}]: " name
    name="${name:-$user}"
  fi
  if [[ -z "$email" ]]; then
    read -rp "请输入 Git 邮箱 (user.email): " email
  fi

  sudo -u "$user" git config --global user.name "$name"
  sudo -u "$user" git config --global user.email "$email"
  sudo -u "$user" git config --global init.defaultBranch main
  sudo -u "$user" git config --global pull.rebase false
  # 大仓库加速
  sudo -u "$user" git config --global http.postBuffer 524288000
  sudo -u "$user" git config --global core.autocrlf input

  log "用户 ${user} Git 配置:"
  sudo -u "$user" git config --global --list
}

if id jenkins &>/dev/null; then
  log "配置 jenkins 用户 Git 身份..."
  configure_git_user jenkins
else
  warn "jenkins 用户不存在，跳过 jenkins Git 配置（请先执行 04-jenkins-install.sh）"
fi

# 为 deploy 用户配置（若存在）
if id deploy &>/dev/null; then
  read -rp "是否同时为 deploy 用户配置 Git? [y/N] " ans
  if [[ "${ans,,}" == "y" ]]; then
    configure_git_user deploy
  fi
fi

# -----------------------------------------------------------------------------
# 4. 为 jenkins 生成 SSH 密钥（可选，使用 SSH 克隆时需要）
# -----------------------------------------------------------------------------
JENKINS_SSH_DIR="/var/lib/jenkins/.ssh"
read -rp "是否为 jenkins 生成 GitHub SSH 密钥? [y/N] " gen_ssh
if [[ "${gen_ssh,,}" == "y" ]] && id jenkins &>/dev/null; then
  mkdir -p "$JENKINS_SSH_DIR"
  chown jenkins:jenkins "$JENKINS_SSH_DIR"
  chmod 700 "$JENKINS_SSH_DIR"

  KEY_FILE="${JENKINS_SSH_DIR}/id_ed25519_github"
  if [[ ! -f "$KEY_FILE" ]]; then
    GITHUB_USER="${GITHUB_USER:-}"
    [[ -z "$GITHUB_USER" ]] && read -rp "GitHub 用户名: " GITHUB_USER
    sudo -u jenkins ssh-keygen -t ed25519 -C "${GITHUB_USER}@jenkins-ci" -f "$KEY_FILE" -N ""
    log "SSH 公钥已生成，请添加到 GitHub:"
    echo ""
    cat "${KEY_FILE}.pub"
    echo ""
    echo "添加路径: GitHub → Settings → SSH and GPG keys → New SSH key"
    echo "Title: jenkins-$(hostname)"
  else
    log "密钥已存在: ${KEY_FILE}.pub"
    cat "${KEY_FILE}.pub"
  fi

  # SSH config
  if [[ ! -f "${JENKINS_SSH_DIR}/config" ]]; then
    cat > "${JENKINS_SSH_DIR}/config" << EOF
Host github.com
  HostName github.com
  User git
  IdentityFile ${KEY_FILE}
  StrictHostKeyChecking accept-new
EOF
    chown jenkins:jenkins "${JENKINS_SSH_DIR}/config"
    chmod 600 "${JENKINS_SSH_DIR}/config"
  fi

  log "测试 SSH 连接（需先在 GitHub 添加公钥）..."
  sudo -u jenkins ssh -T git@github.com 2>&1 || true
fi

log "Git / GitHub 服务器端配置完成"
echo ""
echo "下一步:"
echo "  1. 在 GitHub 创建仓库并 push 代码（见 docs/DEPLOYMENT_GUIDE.md 第 4 章）"
echo "  2. 创建 Personal Access Token 并在 Jenkins 添加凭据"
echo "  3. sudo bash scripts/05-github-webhook-setup.sh"

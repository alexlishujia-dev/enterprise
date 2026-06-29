#!/bin/bash
# =============================================================================
# 04-jenkins-install-fix.sh - 修复 Jenkins apt 源 / GPG / 安装失败
# 用法: sudo bash 04-jenkins-install-fix.sh
# 适用错误:
#   NO_PUBKEY 7198F4B714ABFC68
#   Package 'jenkins' has no installation candidate
#   Unit file jenkins.service does not exist
# =============================================================================
set -euo pipefail

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'
log()  { echo -e "${GREEN}[INFO]${NC} $*"; }
warn() { echo -e "${YELLOW}[WARN]${NC} $*"; }
err()  { echo -e "${RED}[ERROR]${NC} $*"; exit 1; }

[[ $EUID -eq 0 ]] || err "请使用 root 运行: sudo bash $0"

log "1/6 安装依赖..."
apt-get install -y gnupg ca-certificates curl wget openjdk-11-jdk

JENKINS_KEYRING="/usr/share/keyrings/jenkins-keyring.gpg"
log "2/6 清理旧 Jenkins 源..."
rm -f /etc/apt/sources.list.d/jenkins.list "${JENKINS_KEYRING}" 2>/dev/null || true

log "3/6 导入 Jenkins GPG 密钥..."
import_ok=false
for key_url in \
  "https://pkg.jenkins.io/debian-stable/jenkins.io-2023.key" \
  "https://pkg.jenkins.io/debian/jenkins.io-2025.key" \
  "https://pkg.jenkins.io/debian-stable/jenkins.io-2025.key"
do
  if curl -fsSL --connect-timeout 30 "${key_url}" | gpg --dearmor -o "${JENKINS_KEYRING}" 2>/dev/null; then
    [[ -s "${JENKINS_KEYRING}" ]] && { log "密钥导入成功: ${key_url}"; import_ok=true; break; }
  fi
  warn "密钥下载失败，尝试下一个: ${key_url}"
done

if [[ "${import_ok}" != "true" ]]; then
  warn "在线下载密钥均失败，尝试 keyserver..."
  gpg --keyserver keyserver.ubuntu.com --recv-keys 7198F4B714ABFC68
  gpg --export 7198F4B714ABFC68 | gpg --dearmor -o "${JENKINS_KEYRING}"
fi

[[ -s "${JENKINS_KEYRING}" ]] || err "GPG 密钥仍为空，请检查服务器能否访问 pkg.jenkins.io 或 keyserver.ubuntu.com"

try_apt_install() {
  local repo_url="$1"
  log "4/6 尝试 apt 源: ${repo_url}"
  echo deb "[signed-by=${JENKINS_KEYRING}] ${repo_url} binary/" > /etc/apt/sources.list.d/jenkins.list
  cat /etc/apt/sources.list.d/jenkins.list
  apt-get update -y
  if apt-cache show jenkins &>/dev/null; then
    apt-get install -y jenkins
    return 0
  fi
  return 1
}

log "5/6 安装 Jenkins..."
if try_apt_install "https://pkg.jenkins.io/debian-stable" || \
   try_apt_install "https://pkg.jenkins.io/debian"; then
  log "apt 安装 Jenkins 成功"
else
  warn "apt 源均无法安装，尝试直接下载 .deb 包..."
  DEB_URL="https://pkg.jenkins.io/debian-stable/binary/jenkins_2.479.3_all.deb"
  TMP_DEB="/tmp/jenkins.deb"
  wget -O "${TMP_DEB}" "${DEB_URL}" || wget -O "${TMP_DEB}" "https://pkg.jenkins.io/debian/binary/jenkins_2.479.3_all.deb" || \
    err "无法下载 jenkins.deb，请检查网络"
  dpkg -i "${TMP_DEB}" || apt-get install -f -y
  rm -f "${TMP_DEB}"
fi

log "6/6 启动 Jenkins..."
systemctl daemon-reload
systemctl enable jenkins
systemctl start jenkins
sleep 3
systemctl status jenkins --no-pager || true

id jenkins || err "jenkins 用户不存在，安装可能失败"

echo ""
echo "========== Jenkins 安装结果 =========="
echo "状态: $(systemctl is-active jenkins 2>/dev/null || echo unknown)"
echo "访问: http://$(hostname -I | awk '{print $1}'):8080"
[[ -f /var/lib/jenkins/secrets/initialAdminPassword ]] && \
  echo "初始密码: $(cat /var/lib/jenkins/secrets/initialAdminPassword)"
echo ""
echo "下一步:"
echo "  sudo bash scripts/06-git-github-setup.sh"
echo "======================================"

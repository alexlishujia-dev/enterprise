#!/bin/bash
# =============================================================================
# 04-jenkins-install-fix.sh - 修复 Jenkins apt 源 / GPG / 安装失败
# 用法: sudo bash 04-jenkins-install-fix.sh
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

JENKINS_KEYRING="/usr/share/keyrings/jenkins-keyring.gpg"

jenkins_installed() {
  dpkg -l jenkins 2>/dev/null | grep -q '^ii[[:space:]]*jenkins' && \
    [[ -f /lib/systemd/system/jenkins.service || -f /usr/lib/systemd/system/jenkins.service ]]
}

check_apt_sources() {
  local codename
  codename="$(lsb_release -sc 2>/dev/null || echo unknown)"
  if grep -qE '\bfocal\b' /etc/apt/sources.list 2>/dev/null && [[ "${codename}" == "jammy" ]]; then
    err "检测到 Ubuntu 22.04 但 apt 源仍为 20.04 (focal)。请先改回 jammy 源并执行 apt --fix-broken install，再重试。"
  fi
}

install_java_runtime() {
  if command -v java &>/dev/null; then
    log "Java 已安装: $(java -version 2>&1 | head -1)"
    return 0
  fi
  log "安装 Java 运行时..."
  for pkg in openjdk-17-jre-headless openjdk-11-jre-headless; do
    if apt-get install -y "${pkg}"; then
      log "已安装 ${pkg}"
      return 0
    fi
    warn "${pkg} 安装失败，尝试下一个..."
  done
  warn "Java 未安装，将在安装 jenkins.deb 时自动拉取依赖"
}

fetch_latest_jenkins_deb_url() {
  local ver base

  # 清华镜像：deb 直接在 debian-stable/ 目录下（无 binary/ 子目录）
  ver="$(curl -fsSL --connect-timeout 20 \
    'https://mirrors.tuna.tsinghua.edu.cn/jenkins/debian-stable/' 2>/dev/null \
    | grep -oE 'jenkins_[0-9]+\.[0-9]+\.[0-9]+_all\.deb' \
    | sed 's/jenkins_//;s/_all\.deb//' \
    | sort -t. -k1,1n -k2,2n -k3,3n | tail -1 || true)"
  if [[ -n "${ver}" ]]; then
    echo "https://mirrors.tuna.tsinghua.edu.cn/jenkins/debian-stable/jenkins_${ver}_all.deb"
    return 0
  fi

  for base in \
    "https://pkg.jenkins.io/debian-stable/binary" \
    "https://pkg.jenkins.io/debian/binary"
  do
    ver="$(curl -fsSL --connect-timeout 20 "${base}/Packages" 2>/dev/null \
      | awk '/^Package: jenkins$/{f=1} f&&/^Version:/{print $2; exit}' || true)"
    if [[ -n "${ver}" ]]; then
      echo "${base}/jenkins_${ver}_all.deb"
      return 0
    fi
  done
  return 1
}

install_jenkins_deb() {
  local deb_url tmp_deb
  tmp_deb="/tmp/jenkins.deb"
  rm -f "${tmp_deb}"

  if deb_url="$(fetch_latest_jenkins_deb_url)"; then
    log "下载 jenkins.deb: ${deb_url}"
    if wget -q --show-progress -O "${tmp_deb}" "${deb_url}"; then
      dpkg -i "${tmp_deb}" || apt-get install -f -y
      rm -f "${tmp_deb}"
      jenkins_installed && return 0
      warn "deb 包已安装但 jenkins 服务文件缺失"
    fi
  fi

  warn "自动探测版本失败，尝试固定版本地址..."
  for deb_url in \
    "https://mirrors.tuna.tsinghua.edu.cn/jenkins/debian-stable/jenkins_2.555.3_all.deb" \
    "https://mirrors.tuna.tsinghua.edu.cn/jenkins/debian-stable/jenkins_2.541.3_all.deb" \
    "https://pkg.jenkins.io/debian-stable/binary/jenkins_2.479.3_all.deb"
  do
    log "下载 jenkins.deb: ${deb_url}"
    if wget -q --show-progress -O "${tmp_deb}" "${deb_url}"; then
      dpkg -i "${tmp_deb}" || apt-get install -f -y
      rm -f "${tmp_deb}"
      jenkins_installed && return 0
    fi
    warn "下载或安装失败: ${deb_url}"
  done
  return 1
}

import_jenkins_gpg() {
  local key_url import_ok=false
  rm -f "${JENKINS_KEYRING}"

  for key_url in \
    "https://pkg.jenkins.io/debian-stable/jenkins.io-2023.key" \
    "https://pkg.jenkins.io/debian/jenkins.io-2025.key" \
    "https://pkg.jenkins.io/debian-stable/jenkins.io-2025.key"
  do
    log "尝试下载 GPG 密钥: ${key_url}"
    rm -f "${JENKINS_KEYRING}"
    if curl -fsSL --connect-timeout 30 "${key_url}" \
      | gpg --batch --yes --dearmor -o "${JENKINS_KEYRING}" 2>/dev/null; then
      if [[ -s "${JENKINS_KEYRING}" ]]; then
        log "密钥导入成功: ${key_url}"
        import_ok=true
        break
      fi
    fi
    warn "密钥下载失败: ${key_url}"
  done

  if [[ "${import_ok}" != "true" ]]; then
    warn "在线密钥均失败，尝试 keyserver..."
    rm -f "${JENKINS_KEYRING}"
    gpg --batch --yes --keyserver keyserver.ubuntu.com --recv-keys 7198F4B714ABFC68
    gpg --batch --yes --export 7198F4B714ABFC68 | gpg --batch --yes --dearmor -o "${JENKINS_KEYRING}"
  fi

  [[ -s "${JENKINS_KEYRING}" ]]
}

try_apt_install() {
  local repo_url="$1"
  log "尝试 apt 源: ${repo_url}"
  echo deb "[signed-by=${JENKINS_KEYRING}] ${repo_url} binary/" > /etc/apt/sources.list.d/jenkins.list
  apt-get update -y
  if ! apt-cache show jenkins &>/dev/null; then
    warn "apt 源中找不到 jenkins 包: ${repo_url}"
    return 1
  fi
  if apt-get install -y jenkins && jenkins_installed; then
    return 0
  fi
  warn "apt-get install jenkins 失败"
  return 1
}

# ---------- 主流程 ----------
if jenkins_installed; then
  log "Jenkins 已安装，跳过安装步骤"
else
  check_apt_sources

  log "1/6 安装基础依赖..."
  apt-get install -y gnupg ca-certificates curl wget lsb-release
  install_java_runtime

  log "2/6 清理旧 Jenkins apt 源..."
  rm -f /etc/apt/sources.list.d/jenkins.list 2>/dev/null || true

  log "3/6 安装 Jenkins（.deb 直装，无需 GPG）..."
  if install_jenkins_deb; then
    log ".deb 安装 Jenkins 成功"
  else
    warn ".deb 安装失败，尝试 apt 源（需 GPG 密钥）..."
    log "4/6 导入 Jenkins GPG 密钥..."
    import_jenkins_gpg || err "GPG 密钥导入失败，请检查能否访问 pkg.jenkins.io 或 keyserver.ubuntu.com"
    log "5/6 通过 apt 安装 Jenkins..."
    if try_apt_install "https://pkg.jenkins.io/debian-stable" || \
       try_apt_install "https://pkg.jenkins.io/debian"; then
      log "apt 安装 Jenkins 成功"
    else
      err "Jenkins 安装失败。可手动执行:
  wget -O /tmp/jenkins.deb https://mirrors.tuna.tsinghua.edu.cn/jenkins/debian-stable/jenkins_2.555.3_all.deb
  sudo dpkg -i /tmp/jenkins.deb || sudo apt-get install -f -y"
    fi
  fi

  jenkins_installed || err "Jenkins 仍未安装成功"
fi

log "配置 JVM 内存（4GB 机器）..."
JENKINS_JAVA=/etc/default/jenkins
if [[ -f "${JENKINS_JAVA}" ]]; then
  if grep -q '^JAVA_ARGS=' "${JENKINS_JAVA}"; then
    sed -i 's/^JAVA_ARGS=.*/JAVA_ARGS="-Djava.awt.headless=true -Xmx512m -Xms256m"/' "${JENKINS_JAVA}"
  else
    echo 'JAVA_ARGS="-Djava.awt.headless=true -Xmx512m -Xms256m"' >> "${JENKINS_JAVA}"
  fi
fi

log "6/6 启动 Jenkins..."
systemctl daemon-reload
systemctl enable jenkins
systemctl start jenkins
sleep 3
systemctl status jenkins --no-pager || true

id jenkins || err "jenkins 用户不存在"

echo ""
echo "========== Jenkins 安装结果 =========="
echo "状态: $(systemctl is-active jenkins 2>/dev/null || echo unknown)"
echo "访问: http://$(hostname -I | awk '{print $1}'):8080"
[[ -f /var/lib/jenkins/secrets/initialAdminPassword ]] && \
  echo "初始密码: $(cat /var/lib/jenkins/secrets/initialAdminPassword)"
echo ""
echo "下一步: sudo bash scripts/06-git-github-setup.sh"
echo "======================================"

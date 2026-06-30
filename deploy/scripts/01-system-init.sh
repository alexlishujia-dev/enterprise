#!/bin/bash
# =============================================================================
# 01-system-init.sh - Ubuntu 20.04/22.04 系统初始化（国内源、swap、防火墙、用户权限）
# 用法: sudo bash 01-system-init.sh
# =============================================================================
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DEPLOY_DIR="$(dirname "$SCRIPT_DIR")"
CONFIG_DIR="${DEPLOY_DIR}/configs"

RED='\033[0;31m'
GREEN='\033[0;32m'
NC='\033[0m'
log()  { echo -e "${GREEN}[INFO]${NC} $*"; }
warn() { echo -e "${RED}[WARN]${NC} $*"; }

[[ $EUID -eq 0 ]] || { echo "请使用 root 运行: sudo bash $0"; exit 1; }

# -----------------------------------------------------------------------------
# 1. 更换 apt 为国内源（按系统版本自动选择 focal / jammy）
# -----------------------------------------------------------------------------
UBUNTU_CODENAME="$(lsb_release -sc 2>/dev/null || echo unknown)"
case "${UBUNTU_CODENAME}" in
  focal)  APT_SOURCES="${CONFIG_DIR}/apt-sources-cn.list" ;;
  jammy)  APT_SOURCES="${CONFIG_DIR}/apt-sources-cn-jammy.list" ;;
  *)
    warn "未识别的 Ubuntu 版本: ${UBUNTU_CODENAME}，跳过替换 apt 源"
    APT_SOURCES=""
    ;;
esac

if [[ -n "${APT_SOURCES}" ]]; then
  [[ -f "${APT_SOURCES}" ]] || { warn "缺少 ${APT_SOURCES}，跳过替换 apt 源"; APT_SOURCES=""; }
fi

if [[ -n "${APT_SOURCES}" ]]; then
  log "配置阿里云 apt 源 (${UBUNTU_CODENAME})..."
  cp /etc/apt/sources.list /etc/apt/sources.list.bak.$(date +%Y%m%d) 2>/dev/null || true
  cp "${APT_SOURCES}" /etc/apt/sources.list
  apt-get update -y
  apt-get upgrade -y
else
  warn "请手动确认 /etc/apt/sources.list 与当前 Ubuntu 版本一致"
fi

# -----------------------------------------------------------------------------
# 2. 基础工具与时区
# -----------------------------------------------------------------------------
log "安装基础依赖..."
DEBIAN_FRONTEND=noninteractive apt-get install -y \
  curl wget git vim htop net-tools \
  apt-transport-https ca-certificates gnupg lsb-release \
  software-properties-common jq unzip

timedatectl set-timezone Asia/Shanghai

# -----------------------------------------------------------------------------
# 3. Swap（4GB 内存强烈建议 4GB swap）
# -----------------------------------------------------------------------------
SWAP_SIZE="4G"
if ! swapon --show | grep -q '/swapfile'; then
  log "创建 ${SWAP_SIZE} swap..."
  fallocate -l "${SWAP_SIZE}" /swapfile || dd if=/dev/zero of=/swapfile bs=1M count=4096
  chmod 600 /swapfile
  mkswap /swapfile
  swapon /swapfile
  grep -q '/swapfile' /etc/fstab || echo '/swapfile none swap sw 0 0' >> /etc/fstab
  # 降低 swappiness，减少 SSD 磨损
  sysctl vm.swappiness=10
  grep -q 'vm.swappiness' /etc/sysctl.conf || echo 'vm.swappiness=10' >> /etc/sysctl.conf
else
  log "swap 已存在，跳过"
fi

# -----------------------------------------------------------------------------
# 4. 内核参数（K8s 必需）
# -----------------------------------------------------------------------------
log "配置内核参数..."
cat > /etc/sysctl.d/99-kubernetes.conf << 'EOF'
net.bridge.bridge-nf-call-iptables  = 1
net.bridge.bridge-nf-call-ip6tables = 1
net.ipv4.ip_forward                 = 1
fs.inotify.max_user_instances       = 8192
fs.inotify.max_user_watches         = 524288
EOF
modprobe br_netfilter 2>/dev/null || true
modprobe overlay 2>/dev/null || true
sysctl --system

# -----------------------------------------------------------------------------
# 5. 禁用 swap（K8s 官方建议；K3s 可容忍，此处注释掉，保留 swap 防 OOM）
# 若使用完整 K8s 请取消下行注释:
# swapoff -a && sed -i '/swapfile/d' /etc/fstab
# -----------------------------------------------------------------------------

# -----------------------------------------------------------------------------
# 6. 防火墙（UFW）
# -----------------------------------------------------------------------------
if command -v ufw &>/dev/null; then
  log "配置 UFW 防火墙..."
  ufw --force reset
  ufw default deny incoming
  ufw default allow outgoing
  ufw allow 22/tcp comment 'SSH'
  ufw allow 80/tcp comment 'HTTP'
  ufw allow 443/tcp comment 'HTTPS'
  ufw allow 8080/tcp comment 'Jenkins'
  ufw allow 6443/tcp comment 'K8s API'
  # K3s 所需
  ufw allow 8472/udp comment 'K3s Flannel VXLAN'
  ufw allow 10250/tcp comment 'Kubelet'
  ufw --force enable
fi

# -----------------------------------------------------------------------------
# 7. 创建部署用户（可选，推荐不用 root 跑 CI）
# -----------------------------------------------------------------------------
DEPLOY_USER="${DEPLOY_USER:-deploy}"
if ! id "${DEPLOY_USER}" &>/dev/null; then
  log "创建用户 ${DEPLOY_USER}..."
  useradd -m -s /bin/bash "${DEPLOY_USER}"
  usermod -aG sudo "${DEPLOY_USER}"
  # 免密 sudo（生产环境建议改为 NOPASSWD 限定命令）
  echo "${DEPLOY_USER} ALL=(ALL) NOPASSWD:ALL" > "/etc/sudoers.d/${DEPLOY_USER}"
  chmod 440 "/etc/sudoers.d/${DEPLOY_USER}"
fi

# -----------------------------------------------------------------------------
# 8. 目录权限
# -----------------------------------------------------------------------------
mkdir -p /opt/apps /var/log/cicd
chmod 755 /opt/apps

log "系统初始化完成"
echo ""
echo "========== 权限相关命令速查 =========="
echo "# 将用户加入 docker 组（安装 Docker 后执行）"
echo "sudo usermod -aG docker jenkins"
echo "sudo usermod -aG docker ${DEPLOY_USER}"
echo ""
echo "# 查看用户组"
echo "groups jenkins"
echo "id jenkins"
echo ""
echo "# Jenkins 使用 kubectl（K3s 安装后）"
echo "sudo mkdir -p /var/lib/jenkins/.kube"
echo "sudo cp /etc/rancher/k3s/k3s.yaml /var/lib/jenkins/.kube/config"
echo "sudo chown -R jenkins:jenkins /var/lib/jenkins/.kube"
echo "sudo chmod 600 /var/lib/jenkins/.kube/config"
echo ""
echo "# 文件/目录权限修复"
echo "sudo chown -R jenkins:jenkins /var/lib/jenkins"
echo "sudo chmod 755 /var/lib/jenkins"
echo ""
echo "# sudoers 语法检查"
echo "sudo visudo -c"
echo "======================================"

#!/bin/bash
# =============================================================================
# 02-docker-install.sh - Docker CE 安装（国内镜像源 + daemon 加速）
# 用法: sudo bash 02-docker-install.sh
# =============================================================================
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DEPLOY_DIR="$(dirname "$SCRIPT_DIR")"
CONFIG_DIR="${DEPLOY_DIR}/configs"

GREEN='\033[0;32m'
NC='\033[0m'
log() { echo -e "${GREEN}[INFO]${NC} $*"; }

[[ $EUID -eq 0 ]] || { echo "请使用 root 运行"; exit 1; }

if command -v docker &>/dev/null; then
  log "Docker 已安装: $(docker --version)"
else
  log "添加 Docker 国内 apt 源..."
  install -m 0755 -d /etc/apt/keyrings
  curl -fsSL https://mirrors.aliyun.com/docker-ce/linux/ubuntu/gpg | gpg --dearmor -o /etc/apt/keyrings/docker.gpg
  chmod a+r /etc/apt/keyrings/docker.gpg

  echo \
    "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] \
    https://mirrors.aliyun.com/docker-ce/linux/ubuntu \
    $(. /etc/os-release && echo "$VERSION_CODENAME") stable" \
    > /etc/apt/sources.list.d/docker.list

  apt-get update -y
  apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
fi

# 镜像加速
log "配置 Docker daemon 镜像加速..."
mkdir -p /etc/docker
cp "${CONFIG_DIR}/daemon.json" /etc/docker/daemon.json

systemctl enable docker
systemctl restart docker

# 权限：jenkins 和 deploy 用户加入 docker 组
for u in jenkins deploy; do
  if id "$u" &>/dev/null; then
    usermod -aG docker "$u" || true
    log "用户 $u 已加入 docker 组"
  fi
done

# 验证
docker run --rm hello-world
docker info | grep -A6 "Registry Mirrors" || true

log "Docker 安装完成"
echo ""
echo "提示: 若需阿里云个人加速，编辑 /etc/docker/daemon.json 添加:"
echo '  "https://YOUR_ID.mirror.aliyuncs.com"'
echo "然后: sudo systemctl restart docker"

#!/bin/bash
# =============================================================================
# 03-k3s-install.sh - K3s 轻量 K8s（国内镜像 + containerd 加速）
# 用法: sudo bash 03-k3s-install.sh
# 4GB 内存推荐: 单节点 Server，禁用 Traefik（可选，由 Ingress 替代）
# =============================================================================
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DEPLOY_DIR="$(dirname "$SCRIPT_DIR")"
CONFIG_DIR="${DEPLOY_DIR}/configs"

GREEN='\033[0;32m'
NC='\033[0m'
log() { echo -e "${GREEN}[INFO]${NC} $*"; }

[[ $EUID -eq 0 ]] || { echo "请使用 root 运行"; exit 1; }

# 国内 K3s 安装脚本镜像（任选其一，失败则换）
K3S_INSTALL_URL="${K3S_INSTALL_URL:-https://rancher-mirror.rancher.cn/k3s/k3s-install.sh}"

# 使用阿里云等国内镜像拉取 K3s 二进制与 pause 等
export INSTALL_K3S_MIRROR=cn
# 指定版本（稳定版）
export INSTALL_K3S_VERSION="${INSTALL_K3S_VERSION:-v1.28.5+k3s1}"

# 4GB 优化：限制 etcd 与 kube-apiserver 内存（K3s 内置 sqlite 更省资源）
INSTALL_K3S_EXEC="${INSTALL_K3S_EXEC:---write-kubeconfig-mode 644 \
  --disable traefik \
  --kube-apiserver-arg=default-not-ready-toleration-seconds=30 \
  --kube-apiserver-arg=default-unreachable-toleration-seconds=30}"

if command -v k3s &>/dev/null; then
  log "K3s 已安装: $(k3s --version)"
else
  log "安装 K3s (${INSTALL_K3S_VERSION})..."
  curl -sfL "${K3S_INSTALL_URL}" | INSTALL_K3S_MIRROR=cn INSTALL_K3S_EXEC="${INSTALL_K3S_EXEC}" sh -
fi

# containerd 镜像加速
log "配置 K3s registries.yaml..."
mkdir -p /etc/rancher/k3s
cp "${CONFIG_DIR}/registries.yaml" /etc/rancher/k3s/registries.yaml
systemctl restart k3s

# kubectl 别名（root）
if ! grep -q 'kubectl' /root/.bashrc 2>/dev/null; then
  echo 'alias kubectl="k3s kubectl"' >> /root/.bashrc
fi

# 等待节点 Ready
log "等待节点就绪..."
for i in $(seq 1 30); do
  if k3s kubectl get nodes 2>/dev/null | grep -q Ready; then
    break
  fi
  sleep 5
done

k3s kubectl get nodes -o wide
k3s kubectl get pods -A

# 为 jenkins 配置 kubeconfig
if id jenkins &>/dev/null; then
  log "配置 Jenkins kubeconfig..."
  mkdir -p /var/lib/jenkins/.kube
  cp /etc/rancher/k3s/k3s.yaml /var/lib/jenkins/.kube/config
  # 将 127.0.0.1 改为本机 IP（Jenkins 容器/进程需能访问 API）
  SERVER_IP=$(hostname -I | awk '{print $1}')
  sed -i "s/127.0.0.1/${SERVER_IP}/g" /var/lib/jenkins/.kube/config
  chown -R jenkins:jenkins /var/lib/jenkins/.kube
  chmod 600 /var/lib/jenkins/.kube/config
fi

# 应用 RBAC（Jenkins 部署权限）
K8S_DIR="${DEPLOY_DIR}/k8s"
if [[ -f "${K8S_DIR}/jenkins-rbac.yaml" ]]; then
  k3s kubectl apply -f "${K8S_DIR}/jenkins-rbac.yaml"
fi
if [[ -f "${K8S_DIR}/namespace.yaml" ]]; then
  k3s kubectl apply -f "${K8S_DIR}/namespace.yaml"
fi

log "K3s 安装完成"
echo ""
echo "常用命令:"
echo "  sudo k3s kubectl get pods -A"
echo "  sudo systemctl status k3s"
echo "  sudo -u jenkins kubectl get ns"

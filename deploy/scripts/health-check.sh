#!/bin/bash
# 部署环境健康检查
set -euo pipefail

echo "=== Docker ==="
if docker info >/dev/null 2>&1; then echo "OK"; else echo "FAIL"; fi

echo "=== K3s Node ==="
k3s kubectl get nodes 2>/dev/null || echo "K3s not ready"

echo "=== Jenkins ==="
systemctl is-active jenkins 2>/dev/null || echo "inactive"

echo "=== Jenkins -> Docker ==="
if sudo -u jenkins docker ps >/dev/null 2>&1; then echo "OK"; else echo "FAIL (usermod -aG docker jenkins ?)"; fi

echo "=== Jenkins -> K8s ==="
if sudo -u jenkins kubectl get ns >/dev/null 2>&1; then echo "OK"; else echo "FAIL (kubeconfig ?)"; fi

echo "=== Registry Mirrors ==="
docker info 2>/dev/null | grep -A3 "Registry Mirrors" || true

echo "=== Memory / Disk ==="
free -h | head -2
df -h / | tail -1

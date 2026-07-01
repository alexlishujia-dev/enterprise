#!/bin/bash
# =============================================================================
# 04-jenkins-plugin-mirror.sh - 配置 Jenkins 插件国内镜像（解决离线/404）
# 用法: sudo bash 04-jenkins-plugin-mirror.sh
#
# 说明: 清华 mirrors.tuna.../jenkins/updates/update-center.json 已 404，
#       本脚本自动探测当前可用的国内更新中心地址。
# =============================================================================
set -euo pipefail

[[ $EUID -eq 0 ]] || { echo "请使用 root: sudo bash $0"; exit 1; }

# 按优先级排列（2026 年实测/社区维护）
MIRROR_CANDIDATES=(
  "${JENKINS_UPDATE_MIRROR:-}"
  "https://updates.jenkins-zh.cn/update-center.json"
  "https://mirrors.huaweicloud.com/jenkins/update-center.json"
  "https://cdn.jsdelivr.net/gh/lework/jenkins-update-center/updates/huawei/update-center.json"
  "https://cdn.jsdelivr.net/gh/lework/jenkins-update-center/updates/tsinghua/update-center.json"
)

JENKINS_JAVA=/etc/default/jenkins

pick_mirror_url() {
  local url
  for url in "${MIRROR_CANDIDATES[@]}"; do
    [[ -z "${url}" ]] && continue
    if curl -fsSL --connect-timeout 15 -r 0-1023 "${url}" >/dev/null 2>&1; then
      echo "${url}"
      return 0
    fi
    echo "[WARN] 不可用: ${url}" >&2
  done
  return 1
}

MIRROR_URL="$(pick_mirror_url)" || {
  echo "[ERROR] 所有镜像均不可达。请检查服务器外网，或手动指定:"
  echo "  sudo JENKINS_UPDATE_MIRROR=https://updates.jenkins-zh.cn/update-center.json bash $0"
  exit 1
}

echo "[INFO] 使用插件更新中心: ${MIRROR_URL}"

mkdir -p /var/lib/jenkins/updates
cat > /var/lib/jenkins/hudson.model.UpdateCenter.xml << EOF
<?xml version='1.1' encoding='UTF-8'?>
<sites>
  <site>
    <id>default</id>
    <url>${MIRROR_URL}</url>
  </site>
</sites>
EOF
chown jenkins:jenkins /var/lib/jenkins/hudson.model.UpdateCenter.xml

# 清除旧缓存，避免仍指向 404 地址
rm -rf /var/lib/jenkins/updates/default.json /var/lib/jenkins/updates/*.json 2>/dev/null || true

JAVA_ARGS='-Djava.awt.headless=true -Xmx512m -Xms256m -Dhudson.model.DownloadService.noSignatureCheck=true'
if [[ -f "${JENKINS_JAVA}" ]]; then
  if grep -q '^JAVA_ARGS=' "${JENKINS_JAVA}"; then
    sed -i "s|^JAVA_ARGS=.*|JAVA_ARGS=\"${JAVA_ARGS}\"|" "${JENKINS_JAVA}"
  else
    echo "JAVA_ARGS=\"${JAVA_ARGS}\"" >> "${JENKINS_JAVA}"
  fi
fi

systemctl restart jenkins
sleep 5

if systemctl is-active --quiet jenkins; then
  echo "[INFO] Jenkins 已重启"
  echo "[INFO] 请回到浏览器：跳过插件安装 → 进入主页 → Manage Jenkins → Plugins → Check now"
else
  echo "[ERROR] Jenkins 未运行，请执行: sudo journalctl -u jenkins -n 20"
  exit 1
fi

echo ""
echo "手动验证镜像:"
echo "  curl -I \"${MIRROR_URL}\""

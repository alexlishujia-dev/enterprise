#!/bin/bash
# =============================================================================
# 04-jenkins-plugin-mirror.sh - 配置 Jenkins 插件国内镜像（解决插件安装失败）
# 用法: sudo bash 04-jenkins-plugin-mirror.sh
# =============================================================================
set -euo pipefail

[[ $EUID -eq 0 ]] || { echo "请使用 root: sudo bash $0"; exit 1; }

MIRROR_URL="${JENKINS_UPDATE_MIRROR:-https://mirrors.tuna.tsinghua.edu.cn/jenkins/updates/update-center.json}"
JENKINS_JAVA=/etc/default/jenkins

echo "[INFO] 配置插件更新中心: ${MIRROR_URL}"

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
systemctl is-active jenkins && echo "[INFO] Jenkins 已重启，请回到浏览器点击「重试」安装插件"

echo ""
echo "若清华镜像仍失败，可换华为镜像重试:"
echo "  sudo JENKINS_UPDATE_MIRROR=https://mirrors.huaweicloud.com/jenkins/updates/update-center.json bash $0"

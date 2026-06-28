#!/bin/bash
# =============================================================================
# 04-jenkins-install.sh - Jenkins LTS（国内插件源 + 4GB 内存 JVM 限制）
# 用法: sudo bash 04-jenkins-install.sh
# =============================================================================
set -euo pipefail

GREEN='\033[0;32m'
NC='\033[0m'
log() { echo -e "${GREEN}[INFO]${NC} $*"; }

[[ $EUID -eq 0 ]] || { echo "请使用 root 运行"; exit 1; }

if systemctl is-active --quiet jenkins 2>/dev/null; then
  log "Jenkins 已在运行"
else
  log "添加 Jenkins apt 源..."
  curl -fsSL https://pkg.jenkins.io/debian-stable/jenkins.io-2023.key | gpg --dearmor -o /usr/share/keyrings/jenkins-keyring.gpg
  echo deb [signed-by=/usr/share/keyrings/jenkins-keyring.gpg] \
    https://pkg.jenkins.io/debian-stable binary/ > /etc/apt/sources.list.d/jenkins.list

  apt-get update -y
  apt-get install -y openjdk-11-jdk jenkins

  # 4GB 机器限制 Jenkins 堆内存
  JENKINS_JAVA=/etc/default/jenkins
  if [[ -f "$JENKINS_JAVA" ]]; then
    if grep -q '^JAVA_ARGS=' "$JENKINS_JAVA"; then
      sed -i 's/^JAVA_ARGS=.*/JAVA_ARGS="-Djava.awt.headless=true -Xmx512m -Xms256m"/' "$JENKINS_JAVA"
    else
      echo 'JAVA_ARGS="-Djava.awt.headless=true -Xmx512m -Xms256m"' >> "$JENKINS_JAVA"
    fi
  fi

  # 国内插件更新中心（清华镜像）
  mkdir -p /var/lib/jenkins/updates
  cat > /var/lib/jenkins/hudson.model.UpdateCenter.xml << 'EOF'
<?xml version='1.1' encoding='UTF-8'?>
<sites>
  <site>
    <id>default</id>
    <url>https://mirrors.tuna.tsinghua.edu.cn/jenkins/updates/update-center.json</url>
  </site>
</sites>
EOF
  chown jenkins:jenkins /var/lib/jenkins/hudson.model.UpdateCenter.xml

  systemctl enable jenkins
  systemctl start jenkins
fi

# jenkins 用户权限
usermod -aG docker jenkins 2>/dev/null || true

# 安装 kubectl 供 Jenkins Pipeline 使用
if ! command -v kubectl &>/dev/null; then
  log "安装 kubectl..."
  curl -LO "https://dl.k8s.io/release/$(curl -L -s https://dl.k8s.io/release/stable.txt)/bin/linux/amd64/kubectl"
  install -o root -g root -m 0755 kubectl /usr/local/bin/kubectl
  rm -f kubectl
fi

# 确保 kubeconfig 存在（依赖 03 脚本）
if [[ -f /etc/rancher/k3s/k3s.yaml ]] && [[ ! -f /var/lib/jenkins/.kube/config ]]; then
  mkdir -p /var/lib/jenkins/.kube
  cp /etc/rancher/k3s/k3s.yaml /var/lib/jenkins/.kube/config
  SERVER_IP=$(hostname -I | awk '{print $1}')
  sed -i "s/127.0.0.1/${SERVER_IP}/g" /var/lib/jenkins/.kube/config
  chown -R jenkins:jenkins /var/lib/jenkins/.kube
  chmod 600 /var/lib/jenkins/.kube/config
fi

log "Jenkins 安装完成"
echo ""
echo "========== Jenkins 初始信息 =========="
echo "访问: http://$(hostname -I | awk '{print $1}'):8080"
if [[ -f /var/lib/jenkins/secrets/initialAdminPassword ]]; then
  echo "初始密码: $(cat /var/lib/jenkins/secrets/initialAdminPassword)"
fi
echo ""
echo "推荐安装插件: Git, GitHub, Pipeline, Docker Pipeline, Kubernetes CLI"
echo ""
echo "权限命令:"
echo "  sudo systemctl restart jenkins"
echo "  sudo -u jenkins docker ps"
echo "  sudo -u jenkins kubectl get ns"
echo "======================================"

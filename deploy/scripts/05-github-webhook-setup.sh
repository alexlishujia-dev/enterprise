#!/bin/bash
# =============================================================================
# 05-github-webhook-setup.sh - GitHub Webhook 与 Jenkins 凭据说明（交互式）
# 用法: sudo bash 05-github-webhook-setup.sh
# =============================================================================
set -euo pipefail

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'
log()  { echo -e "${GREEN}[INFO]${NC} $*"; }
warn() { echo -e "${YELLOW}[WARN]${NC} $*"; }

SERVER_IP=$(hostname -I | awk '{print $1}')
JENKINS_URL="http://${SERVER_IP}:8080"

cat << EOF

╔══════════════════════════════════════════════════════════════════╗
║           GitHub + Jenkins 集成配置指南                            ║
╚══════════════════════════════════════════════════════════════════╝

1. GitHub Personal Access Token (classic)
   权限: repo, admin:repo_hook, read:user
   创建: https://github.com/settings/tokens

2. Jenkins 中添加凭据
   路径: Manage Jenkins → Credentials → System → Global
   类型: Secret text
   ID 建议: github-token
   Secret: 粘贴 GitHub Token

3. Jenkins 安装插件
   - GitHub Integration Plugin
   - GitHub Branch Source Plugin
   - Pipeline: GitHub Groovy Libraries

4. GitHub 仓库 Webhook
   Payload URL: ${JENKINS_URL}/github-webhook/
   Content type: application/json
   Secret: (可选，与 Jenkins 中配置一致)
   事件: Just the push event

5. Jenkins 任务配置
   - 勾选 "GitHub hook trigger for GITScm polling"
   - Pipeline script from SCM → Git
   - Repository URL: https://github.com/YOUR_ORG/YOUR_REPO.git
   - Credentials: 选择 github-token

6. 防火墙（若 Webhook 失败）
   sudo ufw allow 8080/tcp
   # 若 GitHub 无法访问内网 IP，需:
   #   - 使用 frp/ngrok 暴露 Jenkins
   #   - 或部署在公网云服务器

EOF

read -rp "是否测试 Jenkins 是否可访问? [y/N] " ans
if [[ "${ans,,}" == "y" ]]; then
  if curl -sf -o /dev/null -w "%{http_code}" "${JENKINS_URL}/login" | grep -qE '200|403'; then
    log "Jenkins 可访问: ${JENKINS_URL}"
  else
    warn "无法访问 ${JENKINS_URL}，请检查 systemctl status jenkins"
  fi
fi

log "配置说明已输出，详细流水线见 configs/jenkins/Jenkinsfile.example"

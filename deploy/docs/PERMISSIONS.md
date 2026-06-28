# 系统权限与运维命令手册

本文档汇总 GitHub + Jenkins + K3s 部署后的常用权限配置与故障处理命令。

---

## 一、用户与组

| 用户 | 组 | 说明 |
|------|-----|------|
| root | root | 安装脚本执行用户 |
| jenkins | jenkins, docker | CI 进程，需访问 Docker 与 K8s |
| deploy | sudo, docker | 可选运维用户 |

### 创建与授权

```bash
# 创建运维用户
sudo useradd -m -s /bin/bash deploy
sudo usermod -aG sudo deploy
sudo usermod -aG docker deploy

# Jenkins 访问 Docker（必须）
sudo usermod -aG docker jenkins
sudo systemctl restart jenkins

# 验证（需重新登录或 newgrp 后生效）
groups jenkins
sudo -u jenkins docker ps
```

---

## 二、文件与目录权限

```bash
# Jenkins 主目录
sudo chown -R jenkins:jenkins /var/lib/jenkins
sudo chmod 755 /var/lib/jenkins

# kubeconfig（仅 jenkins 可读）
sudo mkdir -p /var/lib/jenkins/.kube
sudo cp /etc/rancher/k3s/k3s.yaml /var/lib/jenkins/.kube/config
sudo sed -i "s/127.0.0.1/$(hostname -I | awk '{print $1}')/g" /var/lib/jenkins/.kube/config
sudo chown -R jenkins:jenkins /var/lib/jenkins/.kube
sudo chmod 600 /var/lib/jenkins/.kube/config

# Docker 套接字（默认 root:docker 660）
ls -l /var/run/docker.sock
# 若权限异常:
sudo chmod 660 /var/run/docker.sock
sudo chown root:docker /var/run/docker.sock
```

---

## 三、sudoers 配置

```bash
# 语法检查（修改前必做）
sudo visudo -c

# 允许 deploy 免密 sudo（开发环境）
echo 'deploy ALL=(ALL) NOPASSWD:ALL' | sudo tee /etc/sudoers.d/deploy
sudo chmod 440 /etc/sudoers.d/deploy

# 生产环境：仅允许 Jenkins 重启指定服务
sudo tee /etc/sudoers.d/jenkins-limited << 'EOF'
jenkins ALL=(ALL) NOPASSWD: /bin/systemctl restart k3s, /usr/local/bin/kubectl
EOF
sudo chmod 440 /etc/sudoers.d/jenkins-limited
```

---

## 四、防火墙 (UFW)

```bash
sudo ufw status numbered
sudo ufw allow 22/tcp
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw allow 8080/tcp    # Jenkins
sudo ufw allow 6443/tcp    # K8s API
sudo ufw allow 8472/udp    # K3s Flannel
sudo ufw enable
sudo ufw reload
```

---

## 五、国内镜像相关命令

```bash
# apt 换源后
sudo apt update && sudo apt upgrade -y

# Docker 镜像加速
sudo cp deploy/configs/daemon.json /etc/docker/daemon.json
sudo systemctl restart docker
docker info | grep -A5 "Registry Mirrors"

# 手动拉取测试（国内）
docker pull docker.m.daocloud.io/library/nginx:alpine

# K3s containerd 镜像加速
sudo cp deploy/configs/registries.yaml /etc/rancher/k3s/registries.yaml
sudo systemctl restart k3s

# 预拉取常用镜像（避免首次部署超时）
sudo k3s crictl pull docker.io/library/nginx:alpine
```

### 阿里云个人 Docker 加速

1. 登录 https://cr.console.aliyun.com/cn-hangzhou/instances/mirrors
2. 获取专属地址如 `https://abc123.mirror.aliyuncs.com`
3. 写入 `/etc/docker/daemon.json` 的 `registry-mirrors` 数组首位
4. `sudo systemctl restart docker`

---

## 六、服务管理

```bash
# Jenkins
sudo systemctl start|stop|restart|status jenkins
sudo journalctl -u jenkins -f

# K3s
sudo systemctl start|stop|restart|status k3s
sudo k3s kubectl get nodes
sudo k3s kubectl get pods -A

# Docker
sudo systemctl restart docker
```

---

## 七、K8s 权限排查

```bash
# 以 jenkins 用户测试
sudo -u jenkins kubectl auth can-i create deployments --all-namespaces
sudo -u jenkins kubectl get pods -n enterprise-platform

# 若 forbidden，重新应用 RBAC
sudo k3s kubectl apply -f deploy/k8s/jenkins-rbac.yaml

# 查看当前上下文
sudo -u jenkins kubectl config view
```

---

## 八、4GB 内存优化

```bash
# 查看内存
free -h
sudo k3s kubectl top nodes 2>/dev/null || echo "安装 metrics-server 后可看"

# Jenkins JVM（/etc/default/jenkins）
# JAVA_ARGS="-Xmx512m -Xms256m"

# 限制单个 Pod 资源（见 app-deployment.example.yaml resources 段）

# swap 状态
swapon --show
```

---

## 九、GitHub Webhook 排查

```bash
# Jenkins 是否监听 8080
sudo ss -tlnp | grep 8080

# 从外网测试（需公网 IP 或内网穿透）
curl -I http://YOUR_IP:8080/github-webhook/

# Jenkins 日志中的 webhook 记录
sudo grep -i webhook /var/log/jenkins/jenkins.log | tail -20
```

---

## 十、一键健康检查

```bash
#!/bin/bash
echo "=== Docker ===" && docker info >/dev/null && echo OK || echo FAIL
echo "=== K3s ===" && k3s kubectl get nodes
echo "=== Jenkins ===" && systemctl is-active jenkins
echo "=== Jenkins→Docker ===" && sudo -u jenkins docker ps >/dev/null && echo OK || echo FAIL
echo "=== Jenkins→K8s ===" && sudo -u jenkins kubectl get ns >/dev/null && echo OK || echo FAIL
echo "=== Memory ===" && free -h | head -2
```

保存为 `deploy/scripts/health-check.sh` 后执行: `bash deploy/scripts/health-check.sh`

#!/usr/bin/env bash
set -euo pipefail

export DEBIAN_FRONTEND=noninteractive
source /vagrant_userdata/common-swap.sh
ensure_swap 2G

sudo install -d -m 0755 /etc/apt/keyrings
sudo rm -f /etc/apt/sources.list.d/jenkins.list
sudo apt-get update -y
sudo apt-get install -y \
  ansible \
  ca-certificates \
  curl \
  fontconfig \
  git \
  gnupg \
  openjdk-21-jre \
  sshpass \
  wget

sudo wget -O /etc/apt/keyrings/jenkins-keyring.asc \
  https://pkg.jenkins.io/debian-stable/jenkins.io-2026.key

echo "deb [signed-by=/etc/apt/keyrings/jenkins-keyring.asc] https://pkg.jenkins.io/debian-stable binary/" |
  sudo tee /etc/apt/sources.list.d/jenkins.list >/dev/null

sudo apt-get update -y
sudo apt-get install -y jenkins docker.io

sudo usermod -aG docker jenkins
sudo systemctl enable docker
sudo systemctl start docker
sudo systemctl enable jenkins
sudo systemctl start jenkins

echo "Jenkins setup complete. Open http://192.168.56.103:8080"

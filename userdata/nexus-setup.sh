#!/usr/bin/env bash
set -euo pipefail

export DEBIAN_FRONTEND=noninteractive

NEXUS_VERSION="${NEXUS_VERSION:-3.78.0-14}"
NEXUS_URL="https://download.sonatype.com/nexus/3/nexus-unix-x86-64-${NEXUS_VERSION}.tar.gz"
NEXUS_DIR="nexus-${NEXUS_VERSION}"

sudo apt-get update -y
sudo apt-get install -y \
  ca-certificates \
  curl \
  gnupg \
  java-common \
  software-properties-common \
  tar \
  wget

if [ ! -f /usr/share/keyrings/corretto-archive-keyring.gpg ]; then
  wget -qO- https://apt.corretto.aws/corretto.key |
    sudo gpg --dearmor -o /usr/share/keyrings/corretto-archive-keyring.gpg
fi

echo "deb [signed-by=/usr/share/keyrings/corretto-archive-keyring.gpg] https://apt.corretto.aws stable main" |
  sudo tee /etc/apt/sources.list.d/corretto.list >/dev/null

sudo apt-get update -y
sudo apt-get install -y java-17-amazon-corretto-jdk

sudo mkdir -p /opt/nexus /tmp/nexus

if [ ! -d "/opt/nexus/$NEXUS_DIR" ]; then
  rm -rf /tmp/nexus/*
  wget "$NEXUS_URL" -O /tmp/nexus/nexus.tar.gz
  tar xzf /tmp/nexus/nexus.tar.gz -C /tmp/nexus
  sudo cp -r /tmp/nexus/"$NEXUS_DIR" /opt/nexus/
  rm -rf /tmp/nexus/*
fi

sudo useradd -m -s /bin/bash nexus || true
sudo chown -R nexus:nexus /opt/nexus
echo 'run_as_user="nexus"' | sudo tee "/opt/nexus/$NEXUS_DIR/bin/nexus.rc" >/dev/null

sudo tee /etc/systemd/system/nexus.service >/dev/null <<EOT
[Unit]
Description=Nexus Repository Manager
After=network.target

[Service]
Type=forking
LimitNOFILE=65536
ExecStart=/opt/nexus/$NEXUS_DIR/bin/nexus start
ExecStop=/opt/nexus/$NEXUS_DIR/bin/nexus stop
User=nexus
Restart=on-abort

[Install]
WantedBy=multi-user.target
EOT

sudo systemctl daemon-reload
sudo systemctl enable nexus
sudo systemctl restart nexus

echo "Nexus setup complete. Open http://192.168.56.101:8081"

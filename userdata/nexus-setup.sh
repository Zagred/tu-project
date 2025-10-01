#!/bin/bash

# Exit on errors
set -e

# Update package list and install dependencies
sudo apt-get update -y
sudo apt-get install -y wget tar gnupg software-properties-common curl

# Install Amazon Corretto 17 (Java 17)
sudo apt-get install -y java-common
wget -qO- https://apt.corretto.aws/corretto.key | sudo gpg --dearmor -o /usr/share/keyrings/corretto-archive-keyring.gpg
echo "deb [signed-by=/usr/share/keyrings/corretto-archive-keyring.gpg] https://apt.corretto.aws stable main" | sudo tee /etc/apt/sources.list.d/corretto.list
sudo apt-get update -y
sudo apt-get install -y java-17-amazon-corretto-jdk

# Verify Java installation
java -version

# Prepare Nexus directories
sudo mkdir -p /opt/nexus
sudo mkdir -p /tmp/nexus
cd /tmp/nexus

# Download and extract Nexus
NEXUSURL="https://download.sonatype.com/nexus/3/nexus-unix-x86-64-3.78.0-14.tar.gz"
wget $NEXUSURL -O nexus.tar.gz
EXTOUT=$(tar xzvf nexus.tar.gz)
NEXUSDIR=$(echo $EXTOUT | head -n1 | cut -d'/' -f1)
rm -f nexus.tar.gz
sudo cp -r /tmp/nexus/* /opt/nexus/

# Create nexus user and set permissions
sudo useradd -m -s /bin/bash nexus || true
sudo chown -R nexus:nexus /opt/nexus

# Create systemd service
sudo tee /etc/systemd/system/nexus.service > /dev/null <<EOT
[Unit]
Description=Nexus Repository Manager
After=network.target

[Service]
Type=forking
LimitNOFILE=65536
ExecStart=/opt/nexus/$NEXUSDIR/bin/nexus start
ExecStop=/opt/nexus/$NEXUSDIR/bin/nexus stop
User=nexus
Restart=on-abort

[Install]
WantedBy=multi-user.target
EOT

# Configure nexus to run as nexus user
echo 'run_as_user="nexus"' | sudo tee /opt/nexus/$NEXUSDIR/bin/nexus.rc

# Enable and start service
sudo systemctl daemon-reload
sudo systemctl enable nexus
sudo systemctl start nexus

echo "Nexus installation complete. Access it on port 8081 (default)."

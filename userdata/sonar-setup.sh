#!/usr/bin/env bash
set -euo pipefail

export DEBIAN_FRONTEND=noninteractive
source /vagrant_userdata/common-swap.sh
ensure_swap 4G

SONAR_VERSION="${SONAR_VERSION:-9.9.8.100196}"
SONAR_ZIP="sonarqube-${SONAR_VERSION}.zip"
SONAR_URL="https://binaries.sonarsource.com/Distribution/sonarqube/${SONAR_ZIP}"

sudo tee /etc/sysctl.d/99-sonarqube.conf >/dev/null <<EOT
vm.max_map_count=262144
fs.file-max=65536
EOT
sudo sysctl --system

sudo tee /etc/security/limits.d/99-sonarqube.conf >/dev/null <<EOT
sonar   -   nofile   65536
sonar   -   nproc    4096
EOT

sudo apt-get update -y
sudo apt-get install -y \
  ca-certificates \
  curl \
  nginx \
  openjdk-17-jdk \
  postgresql \
  postgresql-contrib \
  unzip

sudo systemctl enable postgresql
sudo systemctl start postgresql

sudo -u postgres psql <<'SQL'
DO
$$
BEGIN
  IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = 'sonar') THEN
    CREATE ROLE sonar LOGIN PASSWORD 'admin123';
  ELSE
    ALTER ROLE sonar WITH LOGIN PASSWORD 'admin123';
  END IF;
END
$$;
SQL

sudo -u postgres psql -tc "SELECT 1 FROM pg_database WHERE datname = 'sonarqube'" | grep -q 1 ||
  sudo -u postgres createdb -O sonar sonarqube
sudo -u postgres psql -c "GRANT ALL PRIVILEGES ON DATABASE sonarqube TO sonar;"

sudo mkdir -p /sonarqube
if [ ! -d /opt/sonarqube ]; then
  curl -fsSL "$SONAR_URL" -o "/sonarqube/$SONAR_ZIP"
  sudo unzip -q "/sonarqube/$SONAR_ZIP" -d /opt/
  sudo mv "/opt/sonarqube-$SONAR_VERSION" /opt/sonarqube
fi

sudo groupadd sonar || true
sudo useradd -c "SonarQube - User" -d /opt/sonarqube -g sonar sonar || true
sudo chown -R sonar:sonar /opt/sonarqube

sudo tee /opt/sonarqube/conf/sonar.properties >/dev/null <<EOT
sonar.jdbc.username=sonar
sonar.jdbc.password=admin123
sonar.jdbc.url=jdbc:postgresql://localhost/sonarqube
sonar.web.host=0.0.0.0
sonar.web.port=9000
sonar.web.javaAdditionalOpts=-server
sonar.web.javaOpts=-Xmx768m -Xms256m
sonar.ce.javaOpts=-Xmx512m -Xms256m
sonar.search.javaOpts=-Xmx512m -Xms512m -XX:+HeapDumpOnOutOfMemoryError
sonar.log.level=INFO
sonar.path.logs=logs
EOT

sudo tee /etc/systemd/system/sonarqube.service >/dev/null <<EOT
[Unit]
Description=SonarQube service
After=syslog.target network.target

[Service]
Type=forking

ExecStart=/opt/sonarqube/bin/linux-x86-64/sonar.sh start
ExecStop=/opt/sonarqube/bin/linux-x86-64/sonar.sh stop

User=sonar
Group=sonar
Restart=always

LimitNOFILE=65536
LimitNPROC=4096

[Install]
WantedBy=multi-user.target
EOT

sudo rm -f /etc/nginx/sites-enabled/default /etc/nginx/sites-available/default
sudo tee /etc/nginx/sites-available/sonarqube >/dev/null <<EOT
server{
    listen      80;
    server_name _;

    access_log  /var/log/nginx/sonar.access.log;
    error_log   /var/log/nginx/sonar.error.log;

    proxy_buffers 16 64k;
    proxy_buffer_size 128k;

    location / {
        proxy_pass  http://127.0.0.1:9000;
        proxy_next_upstream error timeout invalid_header http_500 http_502 http_503 http_504;
        proxy_redirect off;
              
        proxy_set_header    Host            \$host;
        proxy_set_header    X-Real-IP       \$remote_addr;
        proxy_set_header    X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header    X-Forwarded-Proto http;
    }
}
EOT

sudo ln -sf /etc/nginx/sites-available/sonarqube /etc/nginx/sites-enabled/sonarqube
sudo systemctl daemon-reload
sudo systemctl enable sonarqube nginx
sudo systemctl restart sonarqube
sudo systemctl restart nginx
sudo ufw allow 80,9000,9001/tcp || true

echo "SonarQube setup complete. Open http://192.168.56.102:9000"
